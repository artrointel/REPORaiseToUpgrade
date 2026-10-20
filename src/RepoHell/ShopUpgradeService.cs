using System;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

namespace RepoHell;

internal static class ShopUpgradeService
{
    private const string UpgradeRpcName = "TesterUpgradeCommandRPC";

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

    internal static bool TryClaim(PlayerAvatar playerAvatar, string upgradeName)
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
            ApplyUpgrade(steamId, upgradeName);
            Plugin.ModLogger.LogInfo(
                $"Granted {upgradeName} +1 to {SemiFunc.PlayerGetName(playerAvatar)} ({steamId}).");
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

    private static void ApplyUpgrade(string steamId, string upgradeName)
    {
        PunManager punManager = PunManager.instance;

        if (SemiFunc.IsMultiplayer())
        {
            PhotonView photonView = punManager.GetComponent<PhotonView>();
            photonView.RPC(
                UpgradeRpcName,
                RpcTarget.All,
                steamId,
                upgradeName,
                1);
            return;
        }

        ApplySingleplayerUpgrade(punManager, steamId, upgradeName);
    }

    private static void ApplySingleplayerUpgrade(
        PunManager punManager,
        string steamId,
        string upgradeName)
    {
        switch (upgradeName)
        {
            case "DeathHeadBattery":
                punManager.UpgradeDeathHeadBattery(steamId, 1);
                break;
            case "MapPlayerCount":
                punManager.UpgradeMapPlayerCount(steamId, 1);
                break;
            case "CrouchRest":
                punManager.UpgradePlayerCrouchRest(steamId, 1);
                break;
            case "Stamina":
                punManager.UpgradePlayerEnergy(steamId, 1);
                break;
            case "ExtraJump":
                punManager.UpgradePlayerExtraJump(steamId, 1);
                break;
            case "Range":
                punManager.UpgradePlayerGrabRange(steamId, 1);
                break;
            case "Strength":
                punManager.UpgradePlayerGrabStrength(steamId, 1);
                break;
            case "Throw":
                punManager.UpgradePlayerThrowStrength(steamId, 1);
                break;
            case "Health":
                punManager.UpgradePlayerHealth(steamId, 1);
                break;
            case "Speed":
                punManager.UpgradePlayerSprintSpeed(steamId, 1);
                break;
            case "TumbleClimb":
                punManager.UpgradePlayerTumbleClimb(steamId, 1);
                break;
            case "Launch":
                punManager.UpgradePlayerTumbleLaunch(steamId, 1);
                break;
            case "TumbleWings":
                punManager.UpgradePlayerTumbleWings(steamId, 1);
                break;
            default:
                throw new ArgumentOutOfRangeException(
                    nameof(upgradeName),
                    upgradeName,
                    "Unsupported shop upgrade.");
        }
    }
}
