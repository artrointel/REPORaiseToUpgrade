using HarmonyLib;
using UnityEngine;

namespace RepoHell;

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
