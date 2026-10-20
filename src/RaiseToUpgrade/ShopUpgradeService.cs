using System;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

namespace RaiseToUpgrade;

internal static class ShopUpgradeService
{
    private const string SetPositionRpcName = "SetPositionRPC";
    private const string ToggleItemRpcName = "ToggleItemRPC";
    private static readonly Vector3 CarrierStagingOffset =
        new(4096f, 4096f, 4096f);

    private static readonly HashSet<string> ClaimedPlayers = new(StringComparer.Ordinal);
    private static readonly Dictionary<string, PreparedUpgrade> PreparedByPlayer =
        new(StringComparer.Ordinal);
    private static bool _wasInShop;

    internal static bool IsHostInShop =>
        RunManager.instance != null &&
        GameManager.instance != null &&
        SemiFunc.RunIsShop() &&
        SemiFunc.IsMasterClientOrSingleplayer();

    internal static void TickShopState()
    {
        if (RunManager.instance == null)
        {
            ClearPreparedUpgrades();
            _wasInShop = false;
            return;
        }

        bool isInShop = SemiFunc.RunIsShop();

        if (isInShop && !_wasInShop)
        {
            ClearPreparedUpgrades();
            ClaimedPlayers.Clear();
            Plugin.ModLogger.LogInfo("Entered the shop. Every player may claim one held upgrade.");
        }
        else if (!isInShop && _wasInShop)
        {
            ClearPreparedUpgrades();
        }

        _wasInShop = isInShop;
    }

    internal static bool HasClaimed(string steamId)
    {
        return ClaimedPlayers.Contains(steamId);
    }

    /// <summary>
    /// Prepares one hidden vanilla item for the player's current grab. Returning
    /// false means a new carrier was created this frame, so the gesture timer must
    /// begin at zero. Looking away keeps the same prepared carrier; switching or
    /// releasing the item disposes it.
    /// </summary>
    internal static bool EnsurePrepared(
        ItemUpgrade sourceUpgrade,
        PlayerAvatar playerAvatar,
        string upgradeName)
    {
        if (!IsHostInShop)
        {
            return false;
        }

        string steamId = SemiFunc.PlayerGetSteamID(playerAvatar);
        if (string.IsNullOrWhiteSpace(steamId) || HasClaimed(steamId))
        {
            return false;
        }

        int sourceInstanceId = sourceUpgrade.GetInstanceID();
        if (PreparedByPlayer.TryGetValue(steamId, out PreparedUpgrade? existing))
        {
            if (existing.SourceInstanceId == sourceInstanceId &&
                existing.UpgradeName == upgradeName &&
                existing.Carrier != null)
            {
                return true;
            }

            PreparedByPlayer.Remove(steamId);
            DestroyCarrier(existing.Carrier);
        }

        GameObject carrier;
        try
        {
            carrier = CreateCarrier(sourceUpgrade, upgradeName);
        }
        catch (Exception exception)
        {
            Plugin.ModLogger.LogError(
                $"Could not prepare {upgradeName} feedback for {steamId}: {exception}");
            return false;
        }

        PreparedByPlayer[steamId] = new PreparedUpgrade(
            sourceInstanceId,
            upgradeName,
            carrier);
        return false;
    }

    internal static void CancelPreparation(string steamId, ItemUpgrade sourceUpgrade)
    {
        if (string.IsNullOrWhiteSpace(steamId) ||
            !PreparedByPlayer.TryGetValue(steamId, out PreparedUpgrade? prepared) ||
            prepared.SourceInstanceId != sourceUpgrade.GetInstanceID())
        {
            return;
        }

        PreparedByPlayer.Remove(steamId);
        DestroyCarrier(prepared.Carrier);
    }

    internal static void CancelPreparationsForSource(ItemUpgrade sourceUpgrade)
    {
        int sourceInstanceId = sourceUpgrade.GetInstanceID();
        List<string> stalePlayers = new();

        foreach (KeyValuePair<string, PreparedUpgrade> pair in PreparedByPlayer)
        {
            if (pair.Value.SourceInstanceId == sourceInstanceId)
            {
                stalePlayers.Add(pair.Key);
            }
        }

        foreach (string steamId in stalePlayers)
        {
            PreparedUpgrade prepared = PreparedByPlayer[steamId];
            PreparedByPlayer.Remove(steamId);
            DestroyCarrier(prepared.Carrier);
        }
    }

    internal static bool TryClaim(
        ItemUpgrade sourceUpgrade,
        PlayerAvatar playerAvatar,
        string upgradeName)
    {
        if (!IsHostInShop)
        {
            return false;
        }

        string steamId = SemiFunc.PlayerGetSteamID(playerAvatar);
        if (string.IsNullOrWhiteSpace(steamId) || HasClaimed(steamId))
        {
            return false;
        }

        if (!PreparedByPlayer.TryGetValue(steamId, out PreparedUpgrade? prepared) ||
            prepared.SourceInstanceId != sourceUpgrade.GetInstanceID() ||
            prepared.UpgradeName != upgradeName ||
            prepared.Carrier == null)
        {
            return false;
        }

        ClaimedPlayers.Add(steamId);
        PreparedByPlayer.Remove(steamId);

        try
        {
            TriggerVanillaUpgrade(
                prepared.Carrier,
                sourceUpgrade,
                playerAvatar);

            Plugin.ModLogger.LogInfo(
                $"Played vanilla {upgradeName} +1 use for " +
                $"{SemiFunc.PlayerGetName(playerAvatar)} ({steamId}).");
            return true;
        }
        catch (Exception exception)
        {
            ClaimedPlayers.Remove(steamId);
            DestroyCarrier(prepared.Carrier);
            Plugin.ModLogger.LogError(
                $"Failed to grant {upgradeName} +1 to player {steamId}: {exception}");
            return false;
        }
    }

    private static GameObject CreateCarrier(ItemUpgrade sourceUpgrade, string upgradeName)
    {
        Item item = sourceUpgrade.itemAttributes.item;
        Vector3 stagingPosition = sourceUpgrade.transform.position + CarrierStagingOffset;
        Quaternion rotation = sourceUpgrade.transform.rotation;
        GameObject carrier;

        if (SemiFunc.IsMultiplayer())
        {
            carrier = PhotonNetwork.InstantiateRoomObject(
                item.prefab.ResourcePath,
                stagingPosition,
                rotation,
                0,
                null);
        }
        else
        {
            carrier = UnityEngine.Object.Instantiate(
                item.prefab.Prefab,
                stagingPosition,
                rotation);
        }

        if (carrier == null)
        {
            throw new InvalidOperationException(
                $"Could not prepare the vanilla effect carrier for {upgradeName}.");
        }

        // This host-only marker excludes the carrier from checkout and price
        // progression. It adds no rendering and is not required on vanilla peers.
        carrier.AddComponent<UpgradeEffectCarrier>();

        // The host owns the room object, so disabling host-side physics keeps the
        // staged carrier from falling, colliding, or entering a shop trigger.
        Rigidbody? rigidbody = carrier.GetComponent<Rigidbody>();
        if (rigidbody != null)
        {
            rigidbody.velocity = Vector3.zero;
            rigidbody.angularVelocity = Vector3.zero;
            rigidbody.detectCollisions = false;
            rigidbody.isKinematic = true;
        }

        foreach (Collider collider in carrier.GetComponentsInChildren<Collider>())
        {
            collider.enabled = false;
        }

        return carrier;
    }

    private static void TriggerVanillaUpgrade(
        GameObject carrier,
        ItemUpgrade sourceUpgrade,
        PlayerAvatar playerAvatar)
    {
        ItemToggle itemToggle = carrier.GetComponent<ItemToggle>();
        PhysGrabObject carrierPhysGrabObject = carrier.GetComponent<PhysGrabObject>();
        if (itemToggle == null || carrierPhysGrabObject == null)
        {
            throw new MissingComponentException(
                "The prepared vanilla effect carrier is missing required components.");
        }

        string purchasedItemName = sourceUpgrade.itemAttributes.item.name;
        bool hadPurchasedEntry = StatsManager.instance.itemsPurchased.TryGetValue(
            purchasedItemName,
            out int purchasedItemCount);

        try
        {
            Vector3 effectPosition = sourceUpgrade.transform.position;
            Quaternion effectRotation = sourceUpgrade.transform.rotation;
            int playerPhotonViewId = playerAvatar.photonView.ViewID;

            if (SemiFunc.IsMultiplayer())
            {
                PhotonView photonView = carrier.GetComponent<PhotonView>();
                if (photonView == null || photonView.ViewID == 0)
                {
                    throw new MissingComponentException(
                        "The prepared effect carrier has no networked PhotonView.");
                }

                // Reliable RPCs sent by the same PhotonView arrive in order. Each
                // client teleports first, then the stock consume path hides the
                // mesh and detaches its particles at the original item's position.
                photonView.RPC(
                    SetPositionRpcName,
                    RpcTarget.All,
                    effectPosition,
                    effectRotation);
                photonView.RPC(
                    ToggleItemRpcName,
                    RpcTarget.All,
                    true,
                    playerPhotonViewId);
            }
            else
            {
                carrierPhysGrabObject.SetPositionLogic(effectPosition, effectRotation);
                itemToggle.ToggleItemLogic(true, playerPhotonViewId);
            }
        }
        finally
        {
            RestorePurchasedItemCount(
                purchasedItemName,
                hadPurchasedEntry,
                purchasedItemCount);
        }
    }

    private static void RestorePurchasedItemCount(
        string itemName,
        bool hadPurchasedEntry,
        int originalCount)
    {
        // ItemUpgrade.PlayerUpgrade normally consumes one entry from this table.
        // Our temporary carrier must not consume a real item the players bought.
        if (hadPurchasedEntry)
        {
            StatsManager.instance.itemsPurchased[itemName] = originalCount;
        }
        else
        {
            StatsManager.instance.itemsPurchased.Remove(itemName);
        }
    }

    private static void ClearPreparedUpgrades()
    {
        if (PreparedByPlayer.Count == 0)
        {
            return;
        }

        foreach (PreparedUpgrade prepared in PreparedByPlayer.Values)
        {
            DestroyCarrier(prepared.Carrier);
        }

        PreparedByPlayer.Clear();
    }

    private static void DestroyCarrier(GameObject? carrier)
    {
        if (carrier == null)
        {
            return;
        }

        try
        {
            if (SemiFunc.IsMultiplayer())
            {
                PhotonNetwork.Destroy(carrier);
            }
            else
            {
                UnityEngine.Object.Destroy(carrier);
            }
        }
        catch (Exception exception)
        {
            Plugin.ModLogger.LogWarning(
                $"Could not clean up a prepared upgrade effect carrier: {exception}");
        }
    }

    private sealed class PreparedUpgrade
    {
        internal PreparedUpgrade(
            int sourceInstanceId,
            string upgradeName,
            GameObject carrier)
        {
            SourceInstanceId = sourceInstanceId;
            UpgradeName = upgradeName;
            Carrier = carrier;
        }

        internal int SourceInstanceId { get; }
        internal string UpgradeName { get; }
        internal GameObject Carrier { get; }
    }
}
