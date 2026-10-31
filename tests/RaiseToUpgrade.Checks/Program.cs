using Mono.Cecil;
using Mono.Cecil.Cil;
using RaiseToUpgrade;

int checks = 0;
void Check(bool condition, string message)
{
    checks++;
    if (!condition) throw new InvalidOperationException(message);
}

// Execute the actual installed PhotonStream, rather than a stub or packet-only model.
// The old `new PhotonStream(true, null)` fails here on the first SendNext.
var writer = VanillaPhotonStream.CreateWriter();
Check(writer.IsWriting && writer.Count == 0, "Native write buffer was not initialized.");
byte[] serializedComponent = { 1, 2, 3, 255 };
writer.SendNext(serializedComponent);
writer.SendNext(null);
writer.SendNext(1001);
object[] written = writer.ToArray();
Check(written.Length == 3, "Native PhotonStream write failed.");
Check(ReferenceEquals(written[0], serializedComponent) && written[1] == null && (int)written[2] == 1001,
    "Native PhotonStream changed payload order.");
var otherWriter = VanillaPhotonStream.CreateWriter();
otherWriter.SendNext("independent");
Check(writer.Count == 3 && otherWriter.Count == 1, "Native writers share mutable buffers.");

// Decode the native PUN event envelope as the vanilla OnEvent/OnSerializeRead paths do.
foreach (int prefix in new[] { 0, 1, 127, 255 })
foreach (int length in new[] { 1, 2, 8, 64 })
{
    object[] payload = Enumerable.Range(0, length)
        .Select(i => i % 3 == 0 ? null! : (object)new byte[] { (byte)i, 42 }).ToArray();
    object[] packet = NativeSnapshotPacket.Build(123456, prefix, 1001, payload);
    Check(packet.Length == 3 && (int)packet[0] == 123456, "Invalid native event header.");
    short decodedPrefix = packet[1] == null ? (short)0 : (byte)packet[1];
    Check(decodedPrefix == prefix, "Invalid native scene prefix.");
    object[] view = (object[])packet[2];
    Check((int)view[0] == 1001 && !(bool)view[1] && view[2] == null,
        "First snapshot must be a full, uncompressed native view update.");
    Check(view.Length == payload.Length + 3, "Native component stream length changed.");
    for (int i = 0; i < length; i++)
        Check(ReferenceEquals(view[i + 3], payload[i]), "Native observed-component order or data changed.");
    payload[0] = "changed";
    Check(view[3] == null, "Envelope aliases the caller's payload array.");
}
foreach (var invalid in new[] { (-1, 1001, 1), (256, 1001, 1), (0, 0, 1), (0, -1, 1), (0, 1001, 0) })
{
    bool rejected = false;
    try { NativeSnapshotPacket.Build(0, invalid.Item1, invalid.Item2, new object[invalid.Item3]); }
    catch (ArgumentException) { rejected = true; }
    Check(rejected, "Invalid snapshot accepted.");
}

string root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../"));
string managed = args.Length > 0 ? Path.GetFullPath(args[0]) :
    @"C:\Program Files (x86)\Steam\steamapps\common\REPO\REPO_Data\Managed";
using var resolver = new DefaultAssemblyResolver();
resolver.AddSearchDirectory(managed);
var parameters = new ReaderParameters { AssemblyResolver = resolver };
using var game = AssemblyDefinition.ReadAssembly(Path.Combine(managed, "Assembly-CSharp.dll"), parameters);
using var photon = AssemblyDefinition.ReadAssembly(Path.Combine(managed, "PhotonUnityNetworking.dll"), parameters);
TypeDefinition GameType(string name) => game.MainModule.Types.Single(t => t.Name == name);
MethodDefinition Method(TypeDefinition type, string name, int count) =>
    type.Methods.Single(m => m.Name == name && m.Parameters.Count == count);
bool Calls(MethodDefinition method, string name) => method.Body.Instructions.Any(i =>
    i.Operand is MethodReference call && call.Name == name);

foreach (string type in new[] {
    "ItemUpgradeDeathHeadBattery", "ItemUpgradeMapPlayerCount", "ItemUpgradePlayerCrouchRest",
    "ItemUpgradePlayerEnergy", "ItemUpgradePlayerExtraJump", "ItemUpgradePlayerGrabRange",
    "ItemUpgradePlayerGrabStrength", "ItemUpgradePlayerGrabThrow", "ItemUpgradePlayerHealth",
    "ItemUpgradePlayerSprintSpeed", "ItemUpgradePlayerTumbleClimb", "ItemUpgradePlayerTumbleLaunch",
    "ItemUpgradePlayerTumbleWings" })
    Check(GameType(type).Methods.Any(m => m.Name == "Upgrade"), $"Unsupported upgrade API: {type}");

foreach (var target in new[] {
    ("ItemUpgrade", "Start", 0), ("ShopManager", "ShoppingListItemAdd", 1),
    ("StatsManager", "ItemFetchName", 3), ("ItemManager", "AddSpawnedItem", 1) })
    Check(Method(GameType(target.Item1), target.Item2, target.Item3) != null, "Harmony target missing.");
Check(Calls(Method(GameType("ItemUpgrade"), "PlayerUpgrade", 0), "Invoke"), "Native upgrade event missing.");
Check(Calls(Method(GameType("ItemToggle"), "ToggleItemLogic", 2), "Invoke"), "Native use event missing.");
Check(Method(GameType("ItemToggle"), "ToggleItemRPC", 3).CustomAttributes.Any(a => a.AttributeType.Name == "PunRPC"),
    "Native use RPC missing.");
var nativeView = photon.MainModule.Types.Single(t => t.Name == "PhotonView");
Check(Method(nativeView, "SerializeView", 2).IsPublic, "Native view serialization API changed.");
var nativeTransform = photon.MainModule.Types.Single(t => t.Name == "PhotonTransformView");
var receive = Method(nativeTransform, "OnPhotonSerializeView", 2);
Check(receive.Body.Instructions.Any(i => i.Operand is FieldReference field && field.Name == "m_firstTake"),
    "First-snapshot initialization contract changed.");
Check(Calls(receive, "set_position") && Calls(receive, "set_rotation"),
    "First-snapshot immediate position/rotation application is missing.");
var network = photon.MainModule.Types.Single(t => t.Name == "PhotonNetwork");
Check(Method(network, "SendSerializeViewBatch", 1).Body.Instructions.Any(i =>
    i.OpCode.Code == Code.Ldc_I4 && (int)i.Operand == 206), "Native reliable serialization event changed.");

foreach (string configuration in new[] { "Debug", "Release" })
{
    string path = Path.Combine(root, "src/RaiseToUpgrade/bin", configuration, "netstandard2.1/RaiseToUpgrade.dll");
    using var plugin = AssemblyDefinition.ReadAssembly(path, parameters);
    Check(plugin.Name.Version == new Version(0, 1, 2, 0), "Plugin version mismatch.");
    foreach (MemberReference member in plugin.MainModule.GetMemberReferences())
    {
        if (member.DeclaringType.Scope.Name is not ("Assembly-CSharp" or "PhotonUnityNetworking" or "PhotonRealtime" or "Photon3Unity3D"))
            continue;
        Check(member switch {
            MethodReference method => method.Resolve() != null,
            FieldReference field => field.Resolve() != null,
            _ => true
        }, $"Installed-game API missing: {member.FullName}");
    }
    var service = plugin.MainModule.Types.Single(t => t.Name == "ShopUpgradeService");
    var use = Method(service, "TriggerVanillaUpgrade", 3);
    var calls = use.Body.Instructions.Where(i => i.Operand is MethodReference)
        .Select(i => ((MethodReference)i.Operand).Name).ToList();
    Check(calls.IndexOf("SendFirstSnapshot") < calls.IndexOf("RPC"), "Use was queued before the native snapshot.");
    Check(!use.Body.Instructions.Any(i => i.Operand is string value && value == "SetPositionRPC"),
        "Host-only position RPC is still used for remote feedback.");
}
Console.WriteLine($"Passed {checks} checks: native snapshot framing, payload ordering, installed-game APIs, upgrade types, Harmony targets, and feedback ordering.");
