using System.Collections.Generic;
using UnityEngine;

namespace RepoHell;

internal sealed class UpgradeHoldTracker : MonoBehaviour
{
    internal const float RequiredHoldSeconds = 2f;
    internal const float MinimumLookUpDot = 0.8660254f;

    private readonly Dictionary<string, float> _holdSeconds = new();
    private readonly HashSet<string> _currentlyGrabbing = new();
    private readonly List<string> _stalePlayers = new();
    private readonly List<PhysGrabber> _pendingReleases = new();

    private PhysGrabObject _physGrabObject = null!;
    private ItemUpgrade _itemUpgrade = null!;
    private string _upgradeName = string.Empty;

    internal void Initialize(
        ItemUpgrade itemUpgrade,
        PhysGrabObject physGrabObject,
        string upgradeName)
    {
        _itemUpgrade = itemUpgrade;
        _physGrabObject = physGrabObject;
        _upgradeName = upgradeName;
    }

    private void Update()
    {
        if (_physGrabObject == null || !ShopUpgradeService.IsHostInShop)
        {
            _holdSeconds.Clear();
            return;
        }

        _currentlyGrabbing.Clear();
        _pendingReleases.Clear();

        foreach (PhysGrabber grabber in _physGrabObject.playerGrabbing)
        {
            if (grabber == null || grabber.playerAvatar == null)
            {
                continue;
            }

            PlayerAvatar playerAvatar = grabber.playerAvatar;
            string steamId = SemiFunc.PlayerGetSteamID(playerAvatar);
            if (string.IsNullOrWhiteSpace(steamId) || ShopUpgradeService.HasClaimed(steamId))
            {
                continue;
            }

            if (!_currentlyGrabbing.Add(steamId))
            {
                continue;
            }

            if (!IsLookingUp(playerAvatar))
            {
                _holdSeconds.Remove(steamId);
                continue;
            }

            _holdSeconds.TryGetValue(steamId, out float elapsedSeconds);
            elapsedSeconds += Time.unscaledDeltaTime;
            _holdSeconds[steamId] = elapsedSeconds;

            if (elapsedSeconds >= RequiredHoldSeconds &&
                ShopUpgradeService.TryClaim(_itemUpgrade, playerAvatar, _upgradeName))
            {
                _holdSeconds.Remove(steamId);
                _pendingReleases.Add(grabber);
            }
        }

        RemoveInterruptedHolds();
        ReleaseSuccessfulPlayers();
    }

    private static bool IsLookingUp(PlayerAvatar playerAvatar)
    {
        PlayerLocalCamera playerCamera = playerAvatar.localCamera;
        if (playerCamera == null)
        {
            return false;
        }

        // PlayerLocalCamera is driven directly by the local camera for the host
        // and by its vanilla Photon rotation stream for remote vanilla players.
        // A dot value of sin(60 degrees) means at least 60 degrees above
        // the horizon.
        return Vector3.Dot(playerCamera.transform.forward, Vector3.up) >=
            MinimumLookUpDot;
    }

    private void RemoveInterruptedHolds()
    {
        _stalePlayers.Clear();

        foreach (string steamId in _holdSeconds.Keys)
        {
            if (!_currentlyGrabbing.Contains(steamId))
            {
                _stalePlayers.Add(steamId);
            }
        }

        foreach (string steamId in _stalePlayers)
        {
            _holdSeconds.Remove(steamId);
        }
    }

    private void ReleaseSuccessfulPlayers()
    {
        int objectViewId = _physGrabObject.photonView != null
            ? _physGrabObject.photonView.ViewID
            : -1;

        foreach (PhysGrabber grabber in _pendingReleases)
        {
            if (grabber == null)
            {
                continue;
            }

            try
            {
                // The vanilla RPC releases the grab on the owning client and then
                // propagates GrabEnded normally, so unmodded players stay in sync.
                grabber.OverrideGrabRelease(objectViewId, 0.25f);
            }
            catch (System.Exception exception)
            {
                Plugin.ModLogger.LogWarning(
                    $"Upgrade was granted, but the held item could not be released: {exception}");
            }
        }

        _pendingReleases.Clear();
    }
}
