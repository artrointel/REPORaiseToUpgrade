using HarmonyLib;
using UnityEngine;

namespace RaiseToUpgrade;

/// <summary>
/// Marks the temporary vanilla item used to broadcast stock upgrade feedback.
/// This component exists only on the host and has no rendering or networking.
/// </summary>
internal sealed class UpgradeEffectCarrier : MonoBehaviour
{
}

[HarmonyPatch(typeof(ShopManager), nameof(ShopManager.ShoppingListItemAdd))]
internal static class UpgradeEffectCarrierShopPatch
{
    [HarmonyPrefix]
    private static bool Prefix(ItemAttributes item)
    {
        // Upgrade prices use itemsUpgradesPurchased, which is incremented only
        // when an item in this list is bought. Never let the effect-only carrier
        // enter that purchase path.
        return item == null || item.GetComponent<UpgradeEffectCarrier>() == null;
    }
}

[HarmonyPatch(typeof(StatsManager), nameof(StatsManager.ItemFetchName))]
internal static class UpgradeEffectCarrierStatsPatch
{
    [HarmonyPrefix]
    private static bool Prefix(ItemAttributes itemAttributes)
    {
        // Temporary feedback must not allocate persistent item/save entries.
        return itemAttributes == null || itemAttributes.GetComponent<UpgradeEffectCarrier>() == null;
    }
}

[HarmonyPatch(typeof(ItemManager), nameof(ItemManager.AddSpawnedItem))]
internal static class UpgradeEffectCarrierSpawnedItemPatch
{
    [HarmonyPrefix]
    private static bool Prefix(ItemAttributes item)
    {
        return item == null || item.GetComponent<UpgradeEffectCarrier>() == null;
    }
}
