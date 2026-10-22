# RaiseToUpgrade Specification

## Product statement

In every shop, each player may hold up one upgrade item and look skyward for two seconds to claim a free personal `+1`.

## Gameplay rules

1. The feature is active only while the run is in the shop.
2. Each player may claim one upgrade per shop visit.
3. The player must keep grabbing the same supported upgrade item.
4. The player must look at least 60 degrees above the horizon.
5. Both conditions must remain true continuously for two seconds.
6. Releasing or switching the item resets progress. Looking below the threshold resets the timer without discarding the prepared feedback item.
7. Success grants `+1` only to the player performing the gesture.
8. The original shop item is neither consumed nor purchased.
9. A free claim must not increment purchase counts or change future prices.
10. Multiple players grabbing the same item are tracked independently.
11. A player who has already claimed in the current shop receives no additional reward.
12. Success plays the stock upgrade sound and visual effect for the host and unmodded clients.
13. Success releases that player's grab and prevents immediate re-grabbing for 0.25 seconds.

## Host-only compatibility contract

- The host is the sole authority for gesture validation and rewards.
- When a valid grab begins, the host creates a temporary networked copy of the same vanilla upgrade outside the playable area. The two-second gesture gives every client time to initialize it.
- On success, the host sends the stock position and use RPCs in order through the same network object.
- The temporary item runs the vanilla stat, sound, and particle path, then is consumed. The original shop item remains untouched.
- Releasing or switching the source item before success destroys the unused temporary copy.
- Only the host needs RaiseToUpgrade or BepInEx. Joining players may use an entirely unmodded game.
- If a modded player joins an unmodded host, RaiseToUpgrade does not alter the game state.
- Core behavior must not depend on custom client code, UI, assets, or network messages.

## Supported upgrades

- Health
- Stamina
- Sprint speed
- Extra jump
- Tumble climb, launch, and wings
- Grab range, strength, and throw
- Map player count
- Death Head battery

## Scope

The current release intentionally excludes progress UI, configuration menus, new items, and custom visual effects. This keeps the interaction small and preserves full compatibility for unmodded clients.

## Acceptance criteria

- The gesture requires a continuous two-second grab while looking at least 60 degrees upward.
- Exactly one supported stat increases by one for the initiating player.
- The source item remains in the shop and can still be purchased normally.
- The free claim does not change purchase inventory or the item's next price.
- A second claim in the same shop is rejected.
- Entering a later shop resets the per-player allowance.
- An unmodded client receives the stat increase and observes the same stock sound and visual effect as the host.
- Only the successful player is forced to release the item.
- Abandoned temporary items are cleaned up without visible meshes, collisions, or accumulated network objects.

Use [test-plan.md](test-plan.md) for manual release verification.
