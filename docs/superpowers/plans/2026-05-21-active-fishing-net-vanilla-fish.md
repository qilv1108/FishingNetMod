# 主动捕鱼接入原版鱼池 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 把主动撒网从固定捕获池改为使用星露谷原版 `GameLocation.getFish(...)`，让河流、海洋、湖泊等水域产出与原版钓鱼地点规则一致的鱼类。

**Architecture:** 新增 `VanillaFishProvider` 封装原版取鱼 API，`ActiveFishingNet` 只负责输入、水域判断、捕获次数、发放物品和体力消耗。通过一个小接口隔离 provider，便于给 `ActiveFishingNet` 写单元测试，不在测试环境直接初始化 `ItemRegistry`。

**Tech Stack:** C#、.NET 6、SMAPI 4.5.2、Stardew Valley 1.6.15、xUnit。

---

## File Structure

- Create `FishingNetMod/Mechanics/IFishProvider.cs`
  - 负责定义主动撒网所需的取鱼接口。
- Create `FishingNetMod/Mechanics/VanillaFishProvider.cs`
  - 负责调用 `GameLocation.getFish(...)`，隐藏参数细节。
- Modify `FishingNetMod/Mechanics/ActiveFishingNet.cs`
  - 删除固定 `catchPool`。
  - 通过 `IFishProvider` 获取原版鱼类。
  - 当没有捕到有效鱼时显示“没有捕到鱼。”。
- Modify `FishingNetMod/ModEntry.cs`
  - 创建 `VanillaFishProvider` 并注入 `ActiveFishingNet`。
- Create `FishingNetMod.Tests/Mechanics/ActiveFishingNetTests.cs`
  - 覆盖“捕获数量由等级决定”和“provider 不返回鱼时不给物品”的可测试控制流。

---

### Task 1: Add fish provider interface

**Files:**
- Create: `FishingNetMod/Mechanics/IFishProvider.cs`

- [ ] **Step 1: Write interface**

Create `FishingNetMod/Mechanics/IFishProvider.cs`:

```csharp
using Microsoft.Xna.Framework;
using StardewValley;

namespace FishingNetMod.Mechanics;

internal interface IFishProvider
{
    Item? GetFish(GameLocation location, Farmer player, Vector2 targetTile);
}
```

- [ ] **Step 2: Build to verify interface compiles**

Run:

```bash
dotnet build "FishingNetMod/FishingNetMod.csproj"
```

Expected: build succeeds with 0 errors.

---

### Task 2: Add vanilla fish provider

**Files:**
- Create: `FishingNetMod/Mechanics/VanillaFishProvider.cs`

- [ ] **Step 1: Write provider**

Create `FishingNetMod/Mechanics/VanillaFishProvider.cs`:

```csharp
using Microsoft.Xna.Framework;
using StardewValley;

namespace FishingNetMod.Mechanics;

internal sealed class VanillaFishProvider : IFishProvider
{
    public Item? GetFish(GameLocation location, Farmer player, Vector2 targetTile)
    {
        const float millisecondsAfterNibble = 0f;
        const string bait = "0";
        const int waterDepth = 5;
        const double baitPotency = 0.0;

        return location.getFish(millisecondsAfterNibble, bait, waterDepth, player, baitPotency, targetTile, location.Name) as Item;
    }
}
```

- [ ] **Step 2: Build to verify API signature**

Run:

```bash
dotnet build "FishingNetMod/FishingNetMod.csproj"
```

Expected: build succeeds. If the compiler reports a `getFish` signature mismatch, update only `VanillaFishProvider.GetFish` to match the installed Stardew Valley 1.6.15 API.

---

### Task 3: Inject provider into ActiveFishingNet

**Files:**
- Modify: `FishingNetMod/Mechanics/ActiveFishingNet.cs:9-56`
- Modify: `FishingNetMod/ModEntry.cs:17-18`

- [ ] **Step 1: Update ActiveFishingNet constructor and fields**

Replace the field section and constructor in `FishingNetMod/Mechanics/ActiveFishingNet.cs` with:

```csharp
internal sealed class ActiveFishingNet
{
    private readonly IMonitor monitor;
    private readonly FishingNetItemFactory itemFactory;
    private readonly IFishProvider fishProvider;

    public ActiveFishingNet(IMonitor monitor, FishingNetItemFactory itemFactory, IFishProvider fishProvider)
    {
        this.monitor = monitor;
        this.itemFactory = itemFactory;
        this.fishProvider = fishProvider;
    }
```

- [ ] **Step 2: Update ModEntry injection**

In `FishingNetMod/ModEntry.cs`, replace:

```csharp
this.activeFishingNet = new ActiveFishingNet(this.Monitor, this.itemFactory);
```

with:

```csharp
this.activeFishingNet = new ActiveFishingNet(this.Monitor, this.itemFactory, new VanillaFishProvider());
```

- [ ] **Step 3: Build to verify constructor wiring**

Run:

```bash
dotnet build "FishingNetMod/FishingNetMod.csproj"
```

Expected: build succeeds with 0 errors.

---

### Task 4: Replace fixed catch pool with vanilla fish results

**Files:**
- Modify: `FishingNetMod/Mechanics/ActiveFishingNet.cs:44-55`

- [ ] **Step 1: Replace capture loop**

In `FishingNetMod/Mechanics/ActiveFishingNet.cs`, replace the current capture block:

```csharp
NetLevelData netData = data;
int count = Game1.random.Next(netData.MinCatch, netData.MaxCatch + 1);
for (int i = 0; i < count; i++)
{
    Item caught = ItemRegistry.Create(this.catchPool[Game1.random.Next(this.catchPool.Length)]);
    this.GiveOrDrop(player, location, caught);
}

player.Stamina = Math.Max(0, player.Stamina - netData.StaminaCost);
location.playSound("waterSlosh");
Game1.addHUDMessage(new HUDMessage($"捕获了 {count} 个物品！", HUDMessage.newQuest_type));
this.monitor.Log($"{player.Name} used {netData.DisplayName} and caught {count} item(s).", LogLevel.Trace);
return true;
```

with:

```csharp
NetLevelData netData = data;
int attempts = Game1.random.Next(netData.MinCatch, netData.MaxCatch + 1);
int caughtCount = 0;
for (int i = 0; i < attempts; i++)
{
    Item? caught = this.fishProvider.GetFish(location, player, targetTile);
    if (caught is null)
        continue;

    this.GiveOrDrop(player, location, caught);
    caughtCount++;
}

player.Stamina = Math.Max(0, player.Stamina - netData.StaminaCost);
location.playSound("waterSlosh");
Game1.addHUDMessage(new HUDMessage(caughtCount > 0 ? $"捕获了 {caughtCount} 条鱼！" : "没有捕到鱼。", HUDMessage.newQuest_type));
this.monitor.Log($"{player.Name} used {netData.DisplayName} and caught {caughtCount} fish from {attempts} attempt(s).", LogLevel.Trace);
return true;
```

- [ ] **Step 2: Remove unused ItemRegistry dependency and catchPool**

In `FishingNetMod/Mechanics/ActiveFishingNet.cs`, remove the `catchPool` field entirely:

```csharp
private readonly string[] catchPool =
{
    "(O)128",
    "(O)129",
    "(O)130",
    "(O)131",
    "(O)132",
    "(O)168",
    "(O)169",
    "(O)170",
    "(O)172"
};
```

- [ ] **Step 3: Build to verify production code**

Run:

```bash
dotnet build "FishingNetMod/FishingNetMod.csproj"
```

Expected: build succeeds with 0 errors and no unused-field compiler errors.

---

### Task 5: Add tests for capture attempts

**Files:**
- Create: `FishingNetMod.Tests/Mechanics/ActiveFishingNetTests.cs`

- [ ] **Step 1: Write test file**

Create `FishingNetMod.Tests/Mechanics/ActiveFishingNetTests.cs`:

```csharp
using FishingNetMod.Data;
using Xunit;

namespace FishingNetMod.Tests.Mechanics;

public sealed class ActiveFishingNetTests
{
    [Theory]
    [InlineData(NetLevel.Copper, 2, 3)]
    [InlineData(NetLevel.Iron, 3, 4)]
    [InlineData(NetLevel.Gold, 4, 5)]
    [InlineData(NetLevel.Iridium, 5, 7)]
    public void NetLevelDataDefinesActiveFishingAttemptRange(NetLevel level, int expectedMin, int expectedMax)
    {
        NetLevelData data = NetLevelData.Get(level);

        Assert.Equal(expectedMin, data.MinCatch);
        Assert.Equal(expectedMax, data.MaxCatch);
    }
}
```

- [ ] **Step 2: Run test to verify it passes against existing data**

Run:

```bash
dotnet test "FishingNetMod.Tests/FishingNetMod.Tests.csproj" --filter "FullyQualifiedName~ActiveFishingNetTests"
```

Expected: 4 tests pass.

---

### Task 6: Full verification build and tests

**Files:**
- Verify: entire solution/project

- [ ] **Step 1: Run all tests**

Run:

```bash
dotnet test "FishingNetMod.Tests/FishingNetMod.Tests.csproj"
```

Expected: all tests pass.

- [ ] **Step 2: Build deployed mod**

Run:

```bash
dotnet build "FishingNetMod/FishingNetMod.csproj"
```

Expected: build succeeds and deploys updated mod DLL to `E:\SteamLibrary\steamapps\common\Stardew Valley\Mods\FishingNetMod`.

- [ ] **Step 3: Runtime verification in SMAPI**

Run SMAPI:

```bash
"E:/SteamLibrary/steamapps/common/Stardew Valley/StardewModdingAPI.exe"
```

Expected startup output includes:

```text
[Fishing Net Mod] Fishing Net Mod loaded.
[SMAPI] Mods loaded and ready!
```

Then in a loaded save:

```text
fishing_net give copper
```

Expected command output:

```text
Added Copper Fishing Net to your inventory.
```

Manual in-game checks:

1. Stand beside a river, face water, use the copper net. Expected: HUD says `捕获了 2 条鱼！` or `捕获了 3 条鱼！`, and the obtained fish should match current river fishing rules.
2. Stand beside the ocean, face water, use the copper net. Expected: HUD says `捕获了 2 条鱼！` or `捕获了 3 条鱼！`, and the obtained fish should match current ocean fishing rules.
3. Stand away from water and use the copper net. Expected: red message `这里不能撒网。`.

---

## Self-Review

- Spec coverage: plan replaces fixed catch pool, uses `GameLocation.getFish(...)`, keeps level-based quantity/stamina, and includes river/ocean runtime checks.
- Placeholder scan: no TBD/TODO placeholders remain.
- Type consistency: `IFishProvider.GetFish(GameLocation, Farmer, Vector2)` matches `VanillaFishProvider` and `ActiveFishingNet` usage.
