# Copper Fishing Net Mail Timing Design

## Goal

Make the copper fishing net intro mail follow Stardew Valley's normal mail cadence. When the player reaches fishing level 1, the mod should queue Willy's copper net mail for the next morning instead of placing it in the mailbox immediately.

## Current Behavior

The copper net unlock requirement is already fishing level 1. `QuestProgressTracker` can return immediate mail through `MailToDeliverNow`, and `ModEntry` calls the unlock evaluation from save load, day start, and periodic update paths with immediate delivery enabled. That can put `FishingNet_WillyRequest` directly into `player.mailbox` on the same day the condition is met.

## Desired Behavior

- Fishing level 0 does not start the copper net mail flow.
- Fishing level 1 queues `FishingNet_WillyRequest` in `player.mailForTomorrow`.
- The mod does not add the copper mail directly to `player.mailbox`.
- After the game has delivered the queued mail and the mail flag is present, the mod unlocks the copper fishing net recipe.
- Existing iron, gold, and iridium quest mail behavior remains unchanged.

## Approach

Remove the copper net immediate-delivery path from the quest unlock model. `QuestUnlockPlan` should only describe recipes to unlock and mail to queue for tomorrow. `QuestProgressTracker.EvaluateUnlocks` should no longer accept an immediate-delivery option, and `ModEntry.ApplyQuestUnlocks` should stop moving copper mail into the mailbox.

This keeps the unlock state machine aligned with the base game's mail system and avoids hidden same-day delivery behavior from save-load or tick events.

## Testing

Update quest tracker tests to verify:

- Level 0 produces no copper mail or recipe unlock.
- Level 1 queues `FishingNet_WillyRequest` for tomorrow and does not expose an immediate mail path.
- Once the copper mail is present in received/current mail flags, the copper net recipe unlocks.
- A previously queued-but-missing copper mail flag is re-queued for tomorrow, not delivered immediately.

Run the existing test suite after implementation.
