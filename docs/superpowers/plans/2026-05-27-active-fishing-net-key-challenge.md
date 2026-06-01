# Active Fishing Net Key Challenge Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make active fishing open a 30-second 0-9 key challenge and only award fish after success.

**Architecture:** Extend the existing net challenge state machine to accept explicit digit sequences while preserving the passive harvest default. Refactor active fishing into a prepare/complete flow so `ModEntry` can open the challenge after stamina is consumed and call completion only on success.

**Tech Stack:** C# 10, .NET 6, SMAPI 4.x, Stardew Valley 1.6, xUnit.

---

## File Structure

- Modify: `FishingNetMod/Mechanics/NetHarvestChallenge.cs`
  - Store explicit target numbers, including `0`.
  - Keep the existing `targetCount` constructor for passive harvest.
- Modify: `FishingNetMod/Menus/NetHarvestChallengeMenu.cs`
  - Accept custom title/instruction/target numbers.
  - Map keyboard number keys and numpad keys to digits.
  - Treat Escape as failure.
- Create: `FishingNetMod/Mechanics/ActiveFishingNetCast.cs`
  - Store one prepared active fishing result until the challenge resolves.
- Modify: `FishingNetMod/Mechanics/ActiveFishingNet.cs`
  - Prepare fish without awarding them.
  - Deduct stamina when the challenge starts.
  - Award fish and record quest progress only through a completion method.
- Modify: `FishingNetMod/ModEntry.cs`
  - Open the active fishing challenge on tool use.
  - Call active completion only on challenge success.
- Modify tests under `FishingNetMod.Tests/Mechanics`
  - Cover digit sequences, key mapping, and cast result messaging.

## Tasks

- [x] Add failing tests for explicit digit sequences in `NetHarvestChallengeTests`.
- [x] Implement explicit digit sequence support in `NetHarvestChallenge`.
- [x] Add failing tests for `NetHarvestChallengeMenu.TryGetDigit`.
- [x] Implement keyboard digit mapping and Escape failure behavior in `NetHarvestChallengeMenu`.
- [x] Add failing tests for `ActiveFishingNetCast` result data and HUD message text.
- [x] Add `ActiveFishingNetCast`.
- [x] Refactor `ActiveFishingNet.TryUse` into a prepare flow and add completion.
- [x] Wire active challenge success/failure in `ModEntry`.
- [x] Run `dotnet test "FishingNetMod.Tests/FishingNetMod.Tests.csproj"`.
- [x] Run `dotnet build "FishingNetMod/FishingNetMod.csproj"`.
