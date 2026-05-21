# 被动渔网可视化对象 MVP Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 实现可见、可放置、可过夜产鱼、可交互收获并回收的被动渔网 MVP。

**Architecture:** 新增被动渔网数据模型、管理器和渲染器。`PassiveNetManager` 负责放置、收获、过夜产鱼和存档；`PassiveNetRenderer` 只负责绘制；`ModEntry` 负责事件接线，并保留当前主动撒网逻辑。

**Tech Stack:** C#、.NET 6、SMAPI 4.5.2、Stardew Valley 1.6.15、xUnit。

---

## File Structure

- Create `FishingNetMod/Mechanics/PassiveNetData.cs`
  - 保存单个被动渔网状态。
- Create `FishingNetMod/Mechanics/PassiveNetHarvestData.cs`
  - 保存渔网中待收获的鱼物品 ID 和数量。
- Create `FishingNetMod/Mechanics/PassiveNetManager.cs`
  - 管理放置、收获、每日产出、保存和读取。
- Create `FishingNetMod/Mechanics/PassiveNetRenderer.cs`
  - 在世界地图上绘制被动渔网占位图标。
- Modify `FishingNetMod/Mechanics/VanillaFishProvider.cs`
  - 保持作为主动/被动共用的原版鱼池来源。
- Modify `FishingNetMod/ModEntry.cs`
  - 接入 `Input.ButtonPressed` 的交互键逻辑、`DayStarted`、`SaveLoaded`、`Saving`、`RenderedWorld`。
- Create `FishingNetMod.Tests/Mechanics/PassiveNetManagerTests.cs`
  - 覆盖纯逻辑：每个玩家只能放一个网、同一水格不能重复放、每日产出数量。

---

### Task 1: Add passive net data records

**Files:**
- Create: `FishingNetMod/Mechanics/PassiveNetHarvestData.cs`
- Create: `FishingNetMod/Mechanics/PassiveNetData.cs`
- Test: `FishingNetMod.Tests/Mechanics/PassiveNetManagerTests.cs`

- [ ] **Step 1: Write failing tests for data shape**

Create `FishingNetMod.Tests/Mechanics/PassiveNetManagerTests.cs`:

```csharp
using FishingNetMod.Data;
using FishingNetMod.Mechanics;
using Microsoft.Xna.Framework;
using Xunit;

namespace FishingNetMod.Tests.Mechanics;

public sealed class PassiveNetManagerTests
{
    [Fact]
    public void PassiveNetDataStoresOwnerLocationLevelAndHarvest()
    {
        var fish = new PassiveNetHarvestData("(O)128", 2);
        var data = new PassiveNetData(1234L, "Beach", new Vector2(10, 20), NetLevel.Copper, new List<PassiveNetHarvestData> { fish });

        Assert.Equal(1234L, data.OwnerId);
        Assert.Equal("Beach", data.LocationName);
        Assert.Equal(new Vector2(10, 20), data.Tile);
        Assert.Equal(NetLevel.Copper, data.Level);
        Assert.Single(data.Harvest);
        Assert.Equal("(O)128", data.Harvest[0].QualifiedItemId);
        Assert.Equal(2, data.Harvest[0].Stack);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run:

```bash
dotnet test "FishingNetMod.Tests/FishingNetMod.Tests.csproj" --filter "FullyQualifiedName~PassiveNetManagerTests"
```

Expected: FAIL because `PassiveNetData` and `PassiveNetHarvestData` do not exist.

- [ ] **Step 3: Create harvest data record**

Create `FishingNetMod/Mechanics/PassiveNetHarvestData.cs`:

```csharp
namespace FishingNetMod.Mechanics;

internal sealed record PassiveNetHarvestData(string QualifiedItemId, int Stack);
```

- [ ] **Step 4: Create passive net data record**

Create `FishingNetMod/Mechanics/PassiveNetData.cs`:

```csharp
using FishingNetMod.Data;
using Microsoft.Xna.Framework;

namespace FishingNetMod.Mechanics;

internal sealed record PassiveNetData(
    long OwnerId,
    string LocationName,
    Vector2 Tile,
    NetLevel Level,
    List<PassiveNetHarvestData> Harvest);
```

- [ ] **Step 5: Run test to verify it passes**

Run:

```bash
dotnet test "FishingNetMod.Tests/FishingNetMod.Tests.csproj" --filter "FullyQualifiedName~PassiveNetManagerTests"
```

Expected: PASS, 1 test passes.

---

### Task 2: Add passive net placement rules

**Files:**
- Modify: `FishingNetMod.Tests/Mechanics/PassiveNetManagerTests.cs`
- Create: `FishingNetMod/Mechanics/PassiveNetManager.cs`

- [ ] **Step 1: Add failing placement tests**

Append these tests to `PassiveNetManagerTests`:

```csharp
    [Fact]
    public void TryAddRejectsSecondNetForSameOwner()
    {
        var manager = new PassiveNetManager();
        var first = new PassiveNetData(1234L, "Beach", new Vector2(10, 20), NetLevel.Copper, new List<PassiveNetHarvestData>());
        var second = new PassiveNetData(1234L, "Town", new Vector2(30, 40), NetLevel.Iron, new List<PassiveNetHarvestData>());

        Assert.True(manager.TryAdd(first, out string? firstError));
        Assert.Null(firstError);
        Assert.False(manager.TryAdd(second, out string? secondError));
        Assert.Equal("你已经放置了一个渔网。", secondError);
    }

    [Fact]
    public void TryAddRejectsOccupiedTile()
    {
        var manager = new PassiveNetManager();
        var first = new PassiveNetData(1234L, "Beach", new Vector2(10, 20), NetLevel.Copper, new List<PassiveNetHarvestData>());
        var second = new PassiveNetData(5678L, "Beach", new Vector2(10, 20), NetLevel.Iron, new List<PassiveNetHarvestData>());

        Assert.True(manager.TryAdd(first, out string? firstError));
        Assert.Null(firstError);
        Assert.False(manager.TryAdd(second, out string? secondError));
        Assert.Equal("这里已经有渔网了。", secondError);
    }
```

- [ ] **Step 2: Run tests to verify they fail**

Run:

```bash
dotnet test "FishingNetMod.Tests/FishingNetMod.Tests.csproj" --filter "FullyQualifiedName~PassiveNetManagerTests"
```

Expected: FAIL because `PassiveNetManager` does not exist.

- [ ] **Step 3: Implement manager add rules**

Create `FishingNetMod/Mechanics/PassiveNetManager.cs`:

```csharp
namespace FishingNetMod.Mechanics;

internal sealed class PassiveNetManager
{
    private readonly List<PassiveNetData> nets = new();

    public IReadOnlyList<PassiveNetData> Nets => this.nets;

    public bool TryAdd(PassiveNetData data, out string? error)
    {
        if (this.nets.Any(net => net.OwnerId == data.OwnerId))
        {
            error = "你已经放置了一个渔网。";
            return false;
        }

        if (this.nets.Any(net => net.LocationName == data.LocationName && net.Tile == data.Tile))
        {
            error = "这里已经有渔网了。";
            return false;
        }

        this.nets.Add(data);
        error = null;
        return true;
    }
}
```

- [ ] **Step 4: Run placement tests to verify green**

Run:

```bash
dotnet test "FishingNetMod.Tests/FishingNetMod.Tests.csproj" --filter "FullyQualifiedName~PassiveNetManagerTests"
```

Expected: PASS, 3 tests pass.

---

### Task 3: Add passive production count rules

**Files:**
- Modify: `FishingNetMod.Tests/Mechanics/PassiveNetManagerTests.cs`
- Modify: `FishingNetMod/Mechanics/PassiveNetManager.cs`

- [ ] **Step 1: Add failing production count tests**

Append these tests to `PassiveNetManagerTests`:

```csharp
    [Theory]
    [InlineData(NetLevel.Copper, 1, 1)]
    [InlineData(NetLevel.Iron, 1, 2)]
    [InlineData(NetLevel.Gold, 1, 2)]
    [InlineData(NetLevel.Iridium, 2, 2)]
    public void GetDailyProductionRangeMatchesDesign(NetLevel level, int expectedMin, int expectedMax)
    {
        var range = PassiveNetManager.GetDailyProductionRange(level);

        Assert.Equal(expectedMin, range.Min);
        Assert.Equal(expectedMax, range.Max);
    }
```

- [ ] **Step 2: Run tests to verify they fail**

Run:

```bash
dotnet test "FishingNetMod.Tests/FishingNetMod.Tests.csproj" --filter "FullyQualifiedName~GetDailyProductionRangeMatchesDesign"
```

Expected: FAIL because `GetDailyProductionRange` does not exist.

- [ ] **Step 3: Implement production range**

Add this method inside `PassiveNetManager`:

```csharp
    public static (int Min, int Max) GetDailyProductionRange(NetLevel level)
    {
        return level switch
        {
            NetLevel.Copper => (1, 1),
            NetLevel.Iron => (1, 2),
            NetLevel.Gold => (1, 2),
            NetLevel.Iridium => (2, 2),
            _ => (1, 1)
        };
    }
```

Add this using at the top of `PassiveNetManager.cs`:

```csharp
using FishingNetMod.Data;
```

- [ ] **Step 4: Run production tests to verify green**

Run:

```bash
dotnet test "FishingNetMod.Tests/FishingNetMod.Tests.csproj" --filter "FullyQualifiedName~GetDailyProductionRangeMatchesDesign"
```

Expected: PASS, 4 tests pass.

---

### Task 4: Add runtime placement and harvest manager methods

**Files:**
- Modify: `FishingNetMod/Mechanics/PassiveNetManager.cs`

- [ ] **Step 1: Add runtime methods to manager**

Replace `PassiveNetManager.cs` with:

```csharp
using FishingNetMod.Data;
using FishingNetMod.Items;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;

namespace FishingNetMod.Mechanics;

internal sealed class PassiveNetManager
{
    private const string SaveDataKey = "PassiveNets";

    private readonly List<PassiveNetData> nets = new();
    private readonly FishingNetItemFactory itemFactory;
    private readonly IFishProvider fishProvider;

    public PassiveNetManager()
        : this(new FishingNetItemFactory(), new VanillaFishProvider())
    {
    }

    public PassiveNetManager(FishingNetItemFactory itemFactory, IFishProvider fishProvider)
    {
        this.itemFactory = itemFactory;
        this.fishProvider = fishProvider;
    }

    public IReadOnlyList<PassiveNetData> Nets => this.nets;

    public bool TryAdd(PassiveNetData data, out string? error)
    {
        if (this.nets.Any(net => net.OwnerId == data.OwnerId))
        {
            error = "你已经放置了一个渔网。";
            return false;
        }

        if (this.nets.Any(net => net.LocationName == data.LocationName && net.Tile == data.Tile))
        {
            error = "这里已经有渔网了。";
            return false;
        }

        this.nets.Add(data);
        error = null;
        return true;
    }

    public bool TryPlace(Farmer player, GameLocation location, Vector2 targetTile, NetLevelData netData, out string? error)
    {
        if (!location.isWaterTile((int)targetTile.X, (int)targetTile.Y))
        {
            error = "这里不能放置渔网。";
            return false;
        }

        var data = new PassiveNetData(player.UniqueMultiplayerID, location.Name, targetTile, netData.Level, new List<PassiveNetHarvestData>());
        if (!this.TryAdd(data, out error))
            return false;

        player.removeItemFromInventory(player.CurrentItem);
        return true;
    }

    public bool TryHarvest(Farmer player, GameLocation location, Vector2 targetTile, out string? error)
    {
        PassiveNetData? data = this.nets.FirstOrDefault(net => net.LocationName == location.Name && net.Tile == targetTile);
        if (data is null)
        {
            error = null;
            return false;
        }

        foreach (PassiveNetHarvestData harvest in data.Harvest)
        {
            Item item = ItemRegistry.Create(harvest.QualifiedItemId, harvest.Stack);
            this.GiveOrDrop(player, location, item);
        }

        this.GiveOrDrop(player, location, this.itemFactory.Create(NetLevelData.Get(data.Level)));
        this.nets.Remove(data);
        error = null;
        return true;
    }

    public void ProduceDaily(GameLocation location)
    {
        foreach (PassiveNetData net in this.nets.Where(net => net.LocationName == location.Name).ToList())
        {
            var range = GetDailyProductionRange(net.Level);
            int count = Game1.random.Next(range.Min, range.Max + 1);
            for (int i = 0; i < count; i++)
            {
                Item? fish = this.fishProvider.GetFish(location, Game1.player, net.Tile);
                if (fish is null)
                    continue;

                net.Harvest.Add(new PassiveNetHarvestData(fish.QualifiedItemId, fish.Stack));
            }
        }
    }

    public void Load(IModHelper helper)
    {
        this.nets.Clear();
        List<PassiveNetData>? data = helper.Data.ReadSaveData<List<PassiveNetData>>(SaveDataKey);
        if (data is not null)
            this.nets.AddRange(data);
    }

    public void Save(IModHelper helper)
    {
        helper.Data.WriteSaveData(SaveDataKey, this.nets);
    }

    public static (int Min, int Max) GetDailyProductionRange(NetLevel level)
    {
        return level switch
        {
            NetLevel.Copper => (1, 1),
            NetLevel.Iron => (1, 2),
            NetLevel.Gold => (1, 2),
            NetLevel.Iridium => (2, 2),
            _ => (1, 1)
        };
    }

    private void GiveOrDrop(Farmer player, GameLocation location, Item item)
    {
        Item? leftover = player.addItemToInventory(item);
        if (leftover is null)
            return;

        Game1.createItemDebris(leftover, player.getStandingPosition(), player.FacingDirection, location);
    }
}
```

- [ ] **Step 2: Build to verify runtime API usage**

Run:

```bash
dotnet build "FishingNetMod/FishingNetMod.csproj"
```

Expected: build succeeds. If `Farmer.removeItemFromInventory` signature differs, replace that line with the matching inventory removal method for Stardew Valley 1.6.15.

---

### Task 5: Add passive net renderer

**Files:**
- Create: `FishingNetMod/Mechanics/PassiveNetRenderer.cs`

- [ ] **Step 1: Create renderer**

Create `FishingNetMod/Mechanics/PassiveNetRenderer.cs`:

```csharp
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;

namespace FishingNetMod.Mechanics;

internal sealed class PassiveNetRenderer
{
    public void Draw(SpriteBatch spriteBatch, IEnumerable<PassiveNetData> nets)
    {
        foreach (PassiveNetData net in nets)
        {
            if (Game1.currentLocation?.Name != net.LocationName)
                continue;

            Vector2 position = Game1.GlobalToLocal(Game1.viewport, net.Tile * Game1.tileSize);
            Rectangle source = Game1.getSourceRectForStandardTileSheet(Game1.objectSpriteSheet, 771, 16, 16);
            spriteBatch.Draw(Game1.objectSpriteSheet, position, source, Color.White, 0f, Vector2.Zero, Game1.pixelZoom, SpriteEffects.None, 1f);
        }
    }
}
```

- [ ] **Step 2: Build to verify renderer API usage**

Run:

```bash
dotnet build "FishingNetMod/FishingNetMod.csproj"
```

Expected: build succeeds.

---

### Task 6: Wire passive net events in ModEntry

**Files:**
- Modify: `FishingNetMod/ModEntry.cs`

- [ ] **Step 1: Add fields**

In `ModEntry.cs`, add fields next to `activeFishingNet`:

```csharp
    private PassiveNetManager? passiveNetManager;
    private PassiveNetRenderer? passiveNetRenderer;
```

- [ ] **Step 2: Initialize passive services and events**

In `Entry`, after `activeFishingNet` initialization, add:

```csharp
        this.passiveNetManager = new PassiveNetManager(this.itemFactory, new VanillaFishProvider());
        this.passiveNetRenderer = new PassiveNetRenderer();
```

Also add event subscriptions after `ButtonPressed` subscription:

```csharp
        helper.Events.GameLoop.SaveLoaded += this.OnSaveLoaded;
        helper.Events.GameLoop.Saving += this.OnSaving;
        helper.Events.GameLoop.DayStarted += this.OnDayStarted;
        helper.Events.Display.RenderedWorld += this.OnRenderedWorld;
```

- [ ] **Step 3: Extend button handling**

Replace `OnButtonPressed` with:

```csharp
    private void OnButtonPressed(object? sender, ButtonPressedEventArgs e)
    {
        if (!Context.IsWorldReady)
            return;

        if (e.Button.IsUseToolButton())
        {
            if (this.activeFishingNet!.TryUse(Game1.player, Game1.currentLocation))
                this.Helper.Input.Suppress(e.Button);
            return;
        }

        if (!e.Button.IsActionButton())
            return;

        Vector2 targetTile = this.GetFacingTile(Game1.player);
        if (this.passiveNetManager!.TryHarvest(Game1.player, Game1.currentLocation, targetTile, out string? harvestError))
        {
            this.Helper.Input.Suppress(e.Button);
            if (harvestError is not null)
                Game1.showRedMessage(harvestError);
            return;
        }

        if (!this.itemFactory!.TryGetNetData(Game1.player.CurrentItem, out NetLevelData? data) || data is null)
            return;

        if (!this.passiveNetManager.TryPlace(Game1.player, Game1.currentLocation, targetTile, data, out string? placeError))
        {
            if (placeError is not null)
                Game1.showRedMessage(placeError);
            this.Helper.Input.Suppress(e.Button);
            return;
        }

        Game1.addHUDMessage(new HUDMessage("渔网已放置。", HUDMessage.newQuest_type));
        this.Helper.Input.Suppress(e.Button);
    }
```

- [ ] **Step 4: Add helper and event handlers**

Add these methods inside `ModEntry`:

```csharp
    private Vector2 GetFacingTile(Farmer player)
    {
        Vector2 tile = player.Tile;
        return player.FacingDirection switch
        {
            Game1.up => tile + new Vector2(0, -1),
            Game1.right => tile + new Vector2(1, 0),
            Game1.down => tile + new Vector2(0, 1),
            Game1.left => tile + new Vector2(-1, 0),
            _ => tile
        };
    }

    private void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
    {
        this.passiveNetManager!.Load(this.Helper);
    }

    private void OnSaving(object? sender, SavingEventArgs e)
    {
        this.passiveNetManager!.Save(this.Helper);
    }

    private void OnDayStarted(object? sender, DayStartedEventArgs e)
    {
        foreach (GameLocation location in Game1.locations)
            this.passiveNetManager!.ProduceDaily(location);
    }

    private void OnRenderedWorld(object? sender, RenderedWorldEventArgs e)
    {
        this.passiveNetRenderer!.Draw(e.SpriteBatch, this.passiveNetManager!.Nets);
    }
```

- [ ] **Step 5: Add missing using**

At the top of `ModEntry.cs`, add:

```csharp
using Microsoft.Xna.Framework;
```

- [ ] **Step 6: Build to verify ModEntry wiring**

Run:

```bash
dotnet build "FishingNetMod/FishingNetMod.csproj"
```

Expected: build succeeds.

---

### Task 7: Full verification

**Files:**
- Verify: whole mod and tests

- [ ] **Step 1: Run all tests**

Run:

```bash
dotnet test "FishingNetMod.Tests/FishingNetMod.Tests.csproj"
```

Expected: all tests pass.

- [ ] **Step 2: Build and deploy mod**

Run:

```bash
dotnet build "FishingNetMod/FishingNetMod.csproj"
```

Expected: build succeeds with 0 errors and deploys to `E:\SteamLibrary\steamapps\common\Stardew Valley\Mods\FishingNetMod`.

- [ ] **Step 3: Runtime SMAPI check**

Run:

```bash
"E:/SteamLibrary/steamapps/common/Stardew Valley/StardewModdingAPI.exe"
```

Expected startup output includes:

```text
[Fishing Net Mod] Fishing Net Mod loaded.
[SMAPI] Mods loaded and ready!
```

Manual in-game verification:

1. Load a save.
2. Run `fishing_net give copper` in the SMAPI console.
3. Hold the copper net, face water, press action key. Expected: HUD `渔网已放置。`, visible net icon appears on water, copper net removed from inventory.
4. Try placing another net. Expected: red message `你已经放置了一个渔网。`.
5. Sleep one night.
6. Return to the net and press action key. Expected: generated fish is added to inventory or dropped, copper net returns, visible net disappears.
7. Face non-water and press action key with copper net. Expected: red message `这里不能放置渔网。`.

---

## Self-Review

- Spec coverage: plan includes visible object, one net per player, water placement, overnight production, direct harvest, save/load, and no chest UI.
- Placeholder scan: no TBD/TODO/fill-later placeholders remain.
- Type consistency: `PassiveNetData`, `PassiveNetHarvestData`, `PassiveNetManager`, and `PassiveNetRenderer` signatures match across tasks.
