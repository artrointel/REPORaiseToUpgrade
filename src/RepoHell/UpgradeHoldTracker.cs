using System.Collections.Generic;
using UnityEngine;

namespace RepoHell;

internal sealed class UpgradeHoldTracker : MonoBehaviour
{
    internal const float RequiredHoldSeconds = 2f;

    private readonly Dictionary<string, float> _holdSeconds = new();
    private readonly HashSet<string> _currentlyGrabbing = new();
    private readonly List<string> _stalePlayers = new();

    private PhysGrabObject _physGrabObject = null!;
    private string _upgradeName = string.Empty;

    internal void Initialize(PhysGrabObject physGrabObject, string upgradeName)
    {
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

            _holdSeconds.TryGetValue(steamId, out float elapsedSeconds);
            elapsedSeconds += Time.unscaledDeltaTime;
            _holdSeconds[steamId] = elapsedSeconds;

            if (elapsedSeconds >= RequiredHoldSeconds &&
                ShopUpgradeService.TryClaim(playerAvatar, _upgradeName))
            {
                _holdSeconds.Remove(steamId);
            }
        }

        RemoveInterruptedHolds();
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
}
