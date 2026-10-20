using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace RaiseToUpgrade;

[HarmonyPatch(typeof(ItemUpgrade), "Start")]
internal static class ItemUpgradePatch
{
    private static readonly IReadOnlyDictionary<string, string> UpgradeNames =
        new Dictionary<string, string>
        {
            ["ItemUpgradeDeathHeadBattery"] = "DeathHeadBattery",
            ["ItemUpgradeMapPlayerCount"] = "MapPlayerCount",
            ["ItemUpgradePlayerCrouchRest"] = "CrouchRest",
            ["ItemUpgradePlayerEnergy"] = "Stamina",
            ["ItemUpgradePlayerExtraJump"] = "ExtraJump",
            ["ItemUpgradePlayerGrabRange"] = "Range",
            ["ItemUpgradePlayerGrabStrength"] = "Strength",
            ["ItemUpgradePlayerGrabThrow"] = "Throw",
            ["ItemUpgradePlayerHealth"] = "Health",
            ["ItemUpgradePlayerSprintSpeed"] = "Speed",
            ["ItemUpgradePlayerTumbleClimb"] = "TumbleClimb",
            ["ItemUpgradePlayerTumbleLaunch"] = "Launch",
            ["ItemUpgradePlayerTumbleWings"] = "TumbleWings",
        };

    [HarmonyPostfix]
    private static void Postfix(ItemUpgrade __instance)
    {
        if (__instance.GetComponent<UpgradeEffectCarrier>() != null)
        {
            return;
        }

        if (__instance.GetComponent<UpgradeHoldTracker>() != null)
        {
            return;
        }

        PhysGrabObject physGrabObject = __instance.GetComponent<PhysGrabObject>();
        if (physGrabObject == null)
        {
            return;
        }

        foreach (MonoBehaviour component in __instance.GetComponents<MonoBehaviour>())
        {
            if (component != null &&
                UpgradeNames.TryGetValue(component.GetType().Name, out string upgradeName))
            {
                UpgradeHoldTracker tracker =
                    __instance.gameObject.AddComponent<UpgradeHoldTracker>();
                tracker.Initialize(__instance, physGrabObject, upgradeName);
                return;
            }
        }
    }
}
