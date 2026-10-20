using BepInEx;
using BepInEx.Logging;
using HarmonyLib;

namespace RaiseToUpgrade;

[BepInPlugin(PluginGuid, PluginName, PluginVersion)]
public sealed class Plugin : BaseUnityPlugin
{
    public const string PluginGuid = "artrointel.RaiseToUpgrade";
    public const string PluginName = "RaiseToUpgrade";
    public const string PluginVersion = "0.1.0";

    internal static ManualLogSource ModLogger { get; private set; } = null!;

    private Harmony? _harmony;

    private void Awake()
    {
        ModLogger = Logger;
        _harmony = new Harmony(PluginGuid);
        _harmony.PatchAll();

        ModLogger.LogInfo($"{PluginName} v{PluginVersion} loaded.");
    }

    private void Update()
    {
        ShopUpgradeService.TickShopState();
    }

    private void OnDestroy()
    {
        _harmony?.UnpatchSelf();
    }
}

