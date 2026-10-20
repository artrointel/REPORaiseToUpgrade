using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System.Collections;

namespace RepoHell;

[BepInPlugin(PluginGuid, PluginName, PluginVersion)]
public sealed class Plugin : BaseUnityPlugin
{
    public const string PluginGuid = "TODO_AUTHOR.RepoHell";
    public const string PluginName = "RepoHell";
    public const string PluginVersion = "0.1.0";

    internal static ManualLogSource ModLogger { get; private set; } = null!;
    internal static Plugin Instance { get; private set; } = null!;

    private Harmony? _harmony;

    private void Awake()
    {
        Instance = this;
        ModLogger = Logger;
        _harmony = new Harmony(PluginGuid);
        _harmony.PatchAll();

        ModLogger.LogInfo($"{PluginName} v{PluginVersion} loaded.");
    }

    internal static void RunCoroutine(IEnumerator routine)
    {
        Instance.StartCoroutine(routine);
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

