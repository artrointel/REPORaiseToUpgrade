# Changelog

## 0.1.2

- Fix a null write buffer in the game's PhotonStream that prevented multiplayer upgrade claims from completing.
- Add a regression check that executes the installed PhotonStream implementation, including SendNext, ToArray, null payloads, and independent write buffers.

## 0.1.1

- Update compatibility for R.E.P.O. 0.5.0 (The Sunstone Update).
- Fix vanilla-client feedback positioning after the game's SetPositionRPC became host-only. Send the carrier's first native transform snapshot reliably before activating the upgrade.
- Keep temporary feedback items out of persistent item registration and spawned-item tracking as well as checkout.
- Cancel gesture progress when the source item is destroyed or the player is dead or disabled.
- Build against installed game assemblies when available and add automated game API and native network contract checks.
- Restrict packaging to Release, avoid automatic profile deployment during packaging, and normalize ZIP entry paths.

## 0.1.0

- Renamed the mod to RaiseToUpgrade to match its focused shop-upgrade ritual.
- Require players to hold an upgrade and look at least 60 degrees above the horizon for two seconds.
- Play the vanilla upgrade feedback for hosts and unmodded clients.
- Preserve the original shop item, purchased-item inventory, and future upgrade prices.
- Automatically release the held item after a successful claim.

