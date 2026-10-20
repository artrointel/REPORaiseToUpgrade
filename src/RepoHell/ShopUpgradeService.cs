using System;
using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

namespace RepoHell;

internal static class ShopUpgradeService
{
    private const string ToggleItemRpcName = "ToggleItemRPC";
    private const float EffectCarrierInitializationSeconds = 0.35f;

    private static readonly HashSet<string> ClaimedPlayers = new(StringComparer.Ordinal);
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
            _wasInShop = false;
            return;
        }

        bool isInShop = SemiFunc.RunIsShop();

        if (isInShop && !_wasInShop)
        {
            ClaimedPlayers.Clear();
            Plugin.ModLogger.LogInfo("Entered the shop. Every player may claim one held upgrade.");
        }

        _wasInShop = isInShop;
    }

    internal static bool HasClaimed(string steamId)
    {
        return ClaimedPlayers.Contains(steamId);
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
        if (string.IsNullOrWhiteSpace(steamId) || !ClaimedPlayers.Add(steamId))
        {
            return false;
        }

        try
        {
            QueueVanillaUpgrade(sourceUpgrade, playerAvatar, steamId, upgradeName);

            Plugin.ModLogger.LogInfo(
                $"Queued vanilla {upgradeName} +1 use for " +
                $"{SemiFunc.PlayerGetName(playerAvatar)} ({steamId}).");
            return true;
        }
        catch (Exception exception)
        {
            ClaimedPlayers.Remove(steamId);
            Plugin.ModLogger.LogError(
                $"Failed to grant {upgradeName} +1 to player {steamId}: {exception}");
            return false;
        }
    }

    private static void QueueVanillaUpgrade(
        ItemUpgrade sourceUpgrade,
        PlayerAvatar playerAvatar,
        string steamId,
        string upgradeName)
    {
        Item item = sourceUpgrade.itemAttributes.item;
        Vector3 position = sourceUpgrade.transform.position;
        Quaternion rotation = sourceUpgrade.transform.rotation;
        GameObject effectCarrier;

        if (SemiFunc.IsMultiplayer())
        {
            effectCarrier = PhotonNetwork.InstantiateRoomObject(
                item.prefab.ResourcePath,
                position,
                rotation,
                0,
                null);
        }
        else
        {
            effectCarrier = UnityEngine.Object.Instantiate(
                item.prefab.Prefab,
                position,
                rotation);
        }

        if (effectCarrier == null)
        {
            throw new InvalidOperationException(
                $"Could not create the vanilla effect carrier for {upgradeName}.");
        }

        // The carrier exists only to run the stock use effect. Mark it before the
        // next physics tick so the host can never treat it as a shop purchase.
        effectCarrier.AddComponent<UpgradeEffectCarrier>();

        int playerPhotonViewId = playerAvatar.photonView.ViewID;
        string purchasedItemName = item.name;

        Plugin.RunCoroutine(TriggerVanillaUpgradeNextFrame(
            effectCarrier,
            playerPhotonViewId,
            steamId,
            upgradeName,
            purchasedItemName));
    }

    private static IEnumerator TriggerVanillaUpgradeNextFrame(
        GameObject effectCarrier,
        int playerPhotonViewId,
        string steamId,
        string upgradeName,
        string purchasedItemName)
    {
        // Leave enough time for remote vanilla clients to instantiate the room
        // object and run ItemUpgrade.Start before its use RPC arrives. A single
        // host frame is not sufficient when the remote client's RTT is higher.
        yield return new WaitForSecondsRealtime(EffectCarrierInitializationSeconds);

        bool purchaseSnapshotTaken = false;
        bool hadPurchasedEntry = false;
        int purchasedItemCount = 0;

        try
        {
            if (effectCarrier == null)
            {
                throw new InvalidOperationException(
                    "The vanilla effect carrier disappeared before it could be used.");
            }

            ItemToggle itemToggle = effectCarrier.GetComponent<ItemToggle>();
            if (itemToggle == null)
            {
                throw new MissingComponentException(
                    "The vanilla effect carrier has no ItemToggle component.");
            }

            // Snapshot immediately before the synchronous local RPC. This also
            // preserves a real purchase made during the carrier's init delay.
            hadPurchasedEntry = StatsManager.instance.itemsPurchased.TryGetValue(
                purchasedItemName,
                out purchasedItemCount);
            purchaseSnapshotTaken = true;

            // ItemUpgrade.Start disables ItemToggle while in the shop, so its own
            // Start method never caches a PhotonView on this temporary object.
            // Send the stock RPC through the actual PhotonView instead. Vanilla
            // clients still execute ItemToggleRPC -> ToggleItemLogic themselves.
            if (SemiFunc.IsMultiplayer())
            {
                PhotonView photonView = effectCarrier.GetComponent<PhotonView>();
                if (photonView == null || photonView.ViewID == 0)
                {
                    throw new MissingComponentException(
                        "The vanilla effect carrier has no networked PhotonView.");
                }

                photonView.RPC(
                    ToggleItemRpcName,
                    RpcTarget.All,
                    true,
                    playerPhotonViewId);
            }
            else
            {
                itemToggle.ToggleItemLogic(true, playerPhotonViewId);
            }

        }
        catch (Exception exception)
        {
            // No upgrade was delivered, so allow the player to try again.
            ClaimedPlayers.Remove(steamId);
            Plugin.ModLogger.LogError(
                $"Failed to play/apply vanilla {upgradeName} use for {steamId}: {exception}");

            if (effectCarrier != null)
            {
                if (SemiFunc.IsMultiplayer())
                {
                    PhotonNetwork.Destroy(effectCarrier);
                }
                else
                {
                    UnityEngine.Object.Destroy(effectCarrier);
                }
            }
        }
        finally
        {
            if (purchaseSnapshotTaken)
            {
                RestorePurchasedItemCount(
                    purchasedItemName,
                    hadPurchasedEntry,
                    purchasedItemCount);
            }
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
}
