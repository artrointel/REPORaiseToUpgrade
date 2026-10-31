# R.E.P.O. 0.5.0 compatibility review — 2026-10-31

Target: RaiseToUpgrade 0.1.2, installed Steam build 26294889, R.E.P.O. 0.5.0 (The Sunstone Update).
Official notes: https://steamcommunity.com/app/3241660/announcements

## Findings and changes

- All 13 supported upgrade component classes and their Upgrade methods remain present. Native ItemToggle use events, ItemUpgrade.PlayerUpgrade, purchase accounting, and upgrade feedback still exist. Component-based matching is independent of the new localization feature.
- PhysGrabObject.SetPositionRPC now returns immediately on non-host peers. The previous position-RPC/use-RPC sequence could therefore detach client effects at the hidden carrier position. Replace it with the first native PhotonTransformView snapshot, sent using PUN's reliable serialization event before the use RPC. Keep host carrier serialization Off during preparation so clients apply that first snapshot immediately, rather than waiting for interpolation. Serialize observed components through the native API; add no custom client RPC, prefab, or event type.
- Exclude feedback carriers from StatsManager.ItemFetchName and ItemManager.AddSpawnedItem, in addition to the existing checkout guard. This prevents temporary identities from entering persistent item tracking.
- Stop gesture processing for dead source items and dead/disabled players. This also covers the new shopkeeper destruction path while the original object is awaiting removal.
- Preserve native shopkeeper discounts and destruction for real items. Hidden carriers are not player-held, so they do not qualify for the new presentation scan. No shopkeeper patches are added.
- Build against the installed game and Photon assemblies when available. The public NuGet game-reference package still stops at 0.4.4; it remains a fallback rather than the latest compatibility target. Pin build dependency versions and regenerate stale restore paths after the project's earlier rename.
- Package only Release, skip deployment during packaging, validate cleanup paths, and use forward-slash ZIP entries.

## Validation and limits

Debug and Release builds against the installed game pass without warnings or errors. The automated check runner passes 509 checks for packet framing, prefix/ID boundaries, full snapshot headers, observed payload ordering, installed-game member references, supported upgrade methods, Harmony targets, native first-snapshot handling, and snapshot-before-use ordering.

This review did not launch a game or validate a host/unmodded-client session. Confirm audio/particle positioning, visibility, network delays, gamepad gestures, shopkeeper interactions, rerolls, and price/save behavior using test-plan.md before publishing. The ZIP is prepared locally; it has not been uploaded to Thunderstore.

Assembly-CSharp.dll SHA256: AA6B1CE0DEEF8D2B7104A3722864C4BA94C107257726471F8E44B45C99E36888

## First gameplay test follow-up

The first 0.1.1 gameplay attempt loaded correctly and entered the shop, but every claim failed in PhotonStream.SendNext. The installed PUN constructor leaves writeData null; the initial automated checks validated API signatures and packet envelopes without executing this native stream path.

Version 0.1.2 explicitly initializes a fresh native write buffer before serializing observed components. The regression suite now executes the actual installed PhotonStream and verifies writes, null payloads, output order, and independent buffers. A successful in-game claim and remote vanilla-client feedback still require gameplay confirmation after restart.
