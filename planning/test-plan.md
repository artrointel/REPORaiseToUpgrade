# Manual Test Plan

## Single-player checks

1. Enter a shop and confirm that grabbing an upgrade for less than two seconds grants nothing.
2. Keep grabbing an upgrade while looking forward or downward and confirm that no reward occurs.
3. Look at least 60 degrees upward for two continuous seconds and confirm that the corresponding stat increases by exactly one.
4. Look away before completion, look up again, and confirm that a fresh two-second hold is required.
5. Switch to another upgrade before completion and confirm that the previous attempt is cancelled and the new timer starts from zero.
6. Confirm that success plays the same sound and visual effect as normal use of that vanilla upgrade.
7. Confirm that neither the prepared temporary item nor a teleporting mesh becomes visible.
8. Confirm that the original item remains in the shop and is released automatically after success.
9. Try another upgrade in the same shop and confirm that no second reward is granted.
10. Compare the item's displayed price before and after the free claim and confirm that it does not increase.
11. Enter the next shop and confirm that the player can claim once again.
12. Buy and use the original item after the free claim and confirm that normal checkout and consumption still work.

## Mixed-lobby checks

Use at least two players. Install RaiseToUpgrade only for the host; the joining player must use an unmodded profile.

1. Have the unmodded player complete the gesture and confirm that only their stat increases by one.
2. Confirm that the increase is visible and usable immediately while still inside the shop.
3. Confirm that the host and unmodded player observe the same vanilla use sound and visual effect.
4. Have both players grab the same upgrade and confirm that their timers and one-claim limits remain independent.
5. If both succeed on the same shared item, confirm that each successful player is released without affecting the other's grab incorrectly.
6. Have the host claim first and confirm that the unmodded player may still claim once, and vice versa.
7. Confirm that nobody can claim twice during the same shop visit.
8. Move to the next shop and confirm that both allowances reset.
9. Disconnect and reconnect a player, then confirm that no exception occurs and no reward is assigned to the wrong player.

## Regression checks

- Normal shop checkout and vanilla upgrade use still work.
- The original shop item never disappears because of a free claim.
- A genuinely purchased item still increases its future price according to vanilla rules.
- No free reward is granted outside the shop.
- A modded non-host joining an unmodded lobby cannot activate the feature.
- Rapidly grabbing, releasing, or switching items does not leave hidden temporary objects or accumulate errors.
- The temporary feedback item never becomes visible, collidable, or eligible for checkout.
- Leaving the shop, unloading an item, or ending the run cleans up all prepared temporary items.

## Evidence to capture before release

- BepInEx logs from the host for one successful single-player and mixed-lobby claim.
- A short host recording showing the gesture, vanilla feedback, automatic release, and unchanged source item.
- A short unmodded-client recording showing the same feedback and immediate stat increase.
- Before-and-after price checks for both a free claim and a normal purchase.

## R.E.P.O. 0.5.0 regression checks

- Test host and vanilla-client gestures away from the shopkeeper first, including gamepad input and different game languages. Upgrade detection uses component identities, not localized display names.
- In a mixed lobby, compare the actual particle and sound positions to the original shop item. No feedback may play at the hidden staging location. Repeat under network latency and packet loss.
- Verify no second visible or collidable upgrade appears during the gesture or claim. The first native transform snapshot must precede the use RPC.
- Present a real upgrade to the shopkeeper and confirm normal discount/rejection/destruction behavior remains available. A destroyed item must cancel any ongoing free-claim gesture without granting a reward afterward.
- Reroll the upgrade stand while an item is being held; removed items must not leave pending carriers or grant rewards.
- Check persistent item/save dictionaries before and after repeated aborted gestures; temporary carrier entries must not accumulate.
- Die, become disabled, disconnect, or leave the shop during a gesture; no stale timer or hidden carrier may grant an unintended upgrade.
- Verify the one-free-claim limit, next-shop reset, normal purchase price progression, and original-item consumption after a real purchase.

Automated checks inspect both build configurations and the installed game/Photon DLLs. Passing them does not establish in-game audio/particle behavior or mixed-lobby compatibility without the manual checks above.
