# Active Fishing Net MVP Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build the first playable Fishing Net Mod slice: debug command grants a temporary fishing net item, and using it facing water catches items and consumes stamina.

**Architecture:** Keep game-facing SMAPI wiring in `ModEntry.cs`, isolate level data in `Data/NetLevelData.cs`, isolate temporary item creation/recognition in `Items/FishingNetItemFactory.cs`, and isolate active net behavior in `Mechanics/ActiveFishingNet.cs`. Pure parsing/data behavior is unit-tested in a new test project; SMAPI runtime behavior is verified in-game through console command and tool use.

**Tech Stack:** C# 10, .NET 6, SMAPI 4.x, Stardew Valley 1.6, Pathoschild.Stardew.ModBuildConfig, xUnit for pure unit tests.

---

## File Structure

- Create: `FishingNetMod/Data/NetLevel.cs` — enum values for net levels.
- Create: `FishingNetMod/Data/NetLevelData.cs` — immutable level stats and lookup/parsing helpers.
- Create: `FishingNetMod/Items/FishingNetItemFactory.cs` — temporary item creation and `modData`-based net recognition.
- Create: `FishingNetMod/Mechanics/ActiveFishingNet.cs` — active cast behavior, front-tile detection, reward generation, inventory/debris handling.
- Modify: `FishingNetMod/ModEntry.cs` — initialize services, register `fishing_net` command, wire button input.
- Modify: `FishingNetMod/FishingNetMod.csproj` — expose internals to tests.
- Create: `FishingNetMod.Tests/FishingNetMod.Tests.csproj` — unit test project with project reference.
- Create: `FishingNetMod.Tests/Data/NetLevelDataTests.cs` — tests for level parsing and stats.
- Create: `FishingNetMod.Tests/Items/FishingNetItemFactoryTests.cs` — tests for item marking and recognition.

### Task 1: Add net level data

**Files:**
- Create: `FishingNetMod/Data/NetLevel.cs`
- Create: `FishingNetMod/Data/NetLevelData.cs`
- Modify: `FishingNetMod/FishingNetMod.csproj`
- Create: `FishingNetMod.Tests/FishingNetMod.Tests.csproj`
- Create: `FishingNetMod.Tests/Data/NetLevelDataTests.cs`

- [ ] **Step 1: Create the test project directory**

Run:

```bash
mkdir -p "FishingNetMod.Tests/Data"
```

Expected: command exits successfully.

- [ ] **Step 2: Add internals visibility for tests**

Modify `FishingNetMod/FishingNetMod.csproj` so the first `<PropertyGroup>` remains unchanged and add this item group before the package reference item group:

```xml
  <ItemGroup>
    <InternalsVisibleTo Include="FishingNetMod.Tests" />
  </ItemGroup>
```

The resulting file should contain:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net6.0</TargetFramework>
    <Version>0.1.0</Version>
    <GamePath>E:\SteamLibrary\steamapps\common\Stardew Valley</GamePath>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <InternalsVisibleTo Include="FishingNetMod.Tests" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="Pathoschild.Stardew.ModBuildConfig" Version="4.3.2" PrivateAssets="All" />
  </ItemGroup>
</Project>
```

- [ ] **Step 3: Create the xUnit test project**

Create `FishingNetMod.Tests/FishingNetMod.Tests.csproj` with exactly:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net6.0</TargetFramework>
    <IsPackable>false</IsPackable>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.11.1" />
    <PackageReference Include="xunit" Version="2.9.2" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.8.2">
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\FishingNetMod\FishingNetMod.csproj" />
  </ItemGroup>
</Project>
```

- [ ] **Step 4: Write the failing level data tests**

Create `FishingNetMod.Tests/Data/NetLevelDataTests.cs` with exactly:

```csharp
using FishingNetMod.Data;

namespace FishingNetMod.Tests.Data;

public sealed class NetLevelDataTests
{
    [Theory]
    [InlineData("copper", NetLevel.Copper, "Copper Fishing Net", 2, 3, 10)]
    [InlineData("iron", NetLevel.Iron, "Iron Fishing Net", 3, 4, 8)]
    [InlineData("gold", NetLevel.Gold, "Gold Fishing Net", 4, 5, 6)]
    [InlineData("iridium", NetLevel.Iridium, "Iridium Fishing Net", 5, 7, 4)]
    public void TryParseReturnsExpectedLevelData(string input, NetLevel expectedLevel, string expectedName, int expectedMin, int expectedMax, int expectedStamina)
    {
        bool parsed = NetLevelData.TryParse(input, out NetLevelData? data);

        Assert.True(parsed);
        Assert.NotNull(data);
        Assert.Equal(expectedLevel, data.Level);
        Assert.Equal(expectedName, data.DisplayName);
        Assert.Equal(expectedMin, data.MinCatch);
        Assert.Equal(expectedMax, data.MaxCatch);
        Assert.Equal(expectedStamina, data.StaminaCost);
    }

    [Fact]
    public void TryParseRejectsUnknownLevel()
    {
        bool parsed = NetLevelData.TryParse("diamond", out NetLevelData? data);

        Assert.False(parsed);
        Assert.Null(data);
    }

    [Fact]
    public void TryParseIgnoresCaseAndWhitespace()
    {
        bool parsed = NetLevelData.TryParse("  IRON  ", out NetLevelData? data);

        Assert.True(parsed);
        Assert.NotNull(data);
        Assert.Equal(NetLevel.Iron, data.Level);
    }

    [Fact]
    public void AllReturnsLevelsInUpgradeOrder()
    {
        NetLevel[] levels = NetLevelData.All.Select(data => data.Level).ToArray();

        Assert.Equal(new[] { NetLevel.Copper, NetLevel.Iron, NetLevel.Gold, NetLevel.Iridium }, levels);
    }
}
```

- [ ] **Step 5: Run the test to verify it fails**

Run:

```bash
dotnet test "FishingNetMod.Tests/FishingNetMod.Tests.csproj" --filter NetLevelDataTests
```

Expected: FAIL because `FishingNetMod.Data.NetLevel` and `NetLevelData` do not exist.

- [ ] **Step 6: Create `NetLevel` enum**

Create `FishingNetMod/Data/NetLevel.cs` with exactly:

```csharp
namespace FishingNetMod.Data;

internal enum NetLevel
{
    Copper,
    Iron,
    Gold,
    Iridium
}
```

- [ ] **Step 7: Create `NetLevelData` implementation**

Create `FishingNetMod/Data/NetLevelData.cs` with exactly:

```csharp
namespace FishingNetMod.Data;

internal sealed record NetLevelData(
    NetLevel Level,
    string CommandName,
    string DisplayName,
    int MinCatch,
    int MaxCatch,
    int StaminaCost)
{
    public static IReadOnlyList<NetLevelData> All { get; } = new[]
    {
        new NetLevelData(NetLevel.Copper, "copper", "Copper Fishing Net", 2, 3, 10),
        new NetLevelData(NetLevel.Iron, "iron", "Iron Fishing Net", 3, 4, 8),
        new NetLevelData(NetLevel.Gold, "gold", "Gold Fishing Net", 4, 5, 6),
        new NetLevelData(NetLevel.Iridium, "iridium", "Iridium Fishing Net", 5, 7, 4)
    };

    public static bool TryParse(string? value, out NetLevelData? data)
    {
        string normalized = value?.Trim().ToLowerInvariant() ?? string.Empty;
        data = All.FirstOrDefault(candidate => candidate.CommandName == normalized);
        return data is not null;
    }

    public static NetLevelData Get(NetLevel level)
    {
        return All.First(data => data.Level == level);
    }
}
```

- [ ] **Step 8: Run the level data tests to verify they pass**

Run:

```bash
dotnet test "FishingNetMod.Tests/FishingNetMod.Tests.csproj" --filter NetLevelDataTests
```

Expected: PASS, all `NetLevelDataTests` pass.

### Task 2: Add temporary fishing net item factory

**Files:**
- Create: `FishingNetMod/Items/FishingNetItemFactory.cs`
- Create: `FishingNetMod.Tests/Items/FishingNetItemFactoryTests.cs`

- [ ] **Step 1: Create item folders**

Run:

```bash
mkdir -p "FishingNetMod/Items" "FishingNetMod.Tests/Items"
```

Expected: command exits successfully.

- [ ] **Step 2: Write the failing item factory tests**

Create `FishingNetMod.Tests/Items/FishingNetItemFactoryTests.cs` with exactly:

```csharp
using FishingNetMod.Data;
using FishingNetMod.Items;
using StardewValley;

namespace FishingNetMod.Tests.Items;

public sealed class FishingNetItemFactoryTests
{
    [Fact]
    public void CreateAddsNetLevelMarker()
    {
        FishingNetItemFactory factory = new();
        NetLevelData data = NetLevelData.Get(NetLevel.Copper);

        Item item = factory.Create(data);

        Assert.True(item.modData.TryGetValue(FishingNetItemFactory.NetLevelModDataKey, out string? value));
        Assert.Equal("copper", value);
        Assert.Contains("Copper Fishing Net", item.DisplayName);
    }

    [Fact]
    public void TryGetNetDataReadsMarkedItem()
    {
        FishingNetItemFactory factory = new();
        Item item = factory.Create(NetLevelData.Get(NetLevel.Gold));

        bool found = factory.TryGetNetData(item, out NetLevelData? data);

        Assert.True(found);
        Assert.NotNull(data);
        Assert.Equal(NetLevel.Gold, data.Level);
    }

    [Fact]
    public void TryGetNetDataRejectsUnmarkedItem()
    {
        FishingNetItemFactory factory = new();
        Item item = ItemRegistry.Create("(O)388");

        bool found = factory.TryGetNetData(item, out NetLevelData? data);

        Assert.False(found);
        Assert.Null(data);
    }
}
```

- [ ] **Step 3: Run the item factory tests to verify they fail**

Run:

```bash
dotnet test "FishingNetMod.Tests/FishingNetMod.Tests.csproj" --filter FishingNetItemFactoryTests
```

Expected: FAIL because `FishingNetMod.Items.FishingNetItemFactory` does not exist.

- [ ] **Step 4: Implement item factory**

Create `FishingNetMod/Items/FishingNetItemFactory.cs` with exactly:

```csharp
using FishingNetMod.Data;
using StardewValley;

namespace FishingNetMod.Items;

internal sealed class FishingNetItemFactory
{
    public const string NetLevelModDataKey = "ChenJianCan.FishingNetMod/NetLevel";

    public Item Create(NetLevelData data)
    {
        Item item = ItemRegistry.Create("(O)771");
        item.modData[NetLevelModDataKey] = data.CommandName;
        item.Name = data.DisplayName;
        item.DisplayName = data.DisplayName;
        return item;
    }

    public bool TryGetNetData(Item? item, out NetLevelData? data)
    {
        data = null;

        if (item is null)
            return false;

        if (!item.modData.TryGetValue(NetLevelModDataKey, out string? value))
            return false;

        return NetLevelData.TryParse(value, out data);
    }
}
```

- [ ] **Step 5: Run the item factory tests to verify they pass**

Run:

```bash
dotnet test "FishingNetMod.Tests/FishingNetMod.Tests.csproj" --filter FishingNetItemFactoryTests
```

Expected: PASS, all `FishingNetItemFactoryTests` pass.

### Task 3: Add active fishing net mechanic

**Files:**
- Create: `FishingNetMod/Mechanics/ActiveFishingNet.cs`

- [ ] **Step 1: Create mechanics folder**

Run:

```bash
mkdir -p "FishingNetMod/Mechanics"
```

Expected: command exits successfully.

- [ ] **Step 2: Create active fishing net implementation**

Create `FishingNetMod/Mechanics/ActiveFishingNet.cs` with exactly:

```csharp
using FishingNetMod.Data;
using FishingNetMod.Items;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Delegates;
using StardewValley.Extensions;

namespace FishingNetMod.Mechanics;

internal sealed class ActiveFishingNet
{
    private readonly IMonitor monitor;
    private readonly FishingNetItemFactory itemFactory;
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

    public ActiveFishingNet(IMonitor monitor, FishingNetItemFactory itemFactory)
    {
        this.monitor = monitor;
        this.itemFactory = itemFactory;
    }

    public bool TryUse(Farmer player, GameLocation location)
    {
        if (!this.itemFactory.TryGetNetData(player.CurrentItem, out NetLevelData? data))
            return false;

        Vector2 targetTile = this.GetFacingTile(player);
        if (!location.isWaterTile((int)targetTile.X, (int)targetTile.Y))
        {
            Game1.showRedMessage("这里不能撒网。");
            return true;
        }

        int count = Game1.random.Next(data.MinCatch, data.MaxCatch + 1);
        for (int i = 0; i < count; i++)
        {
            Item caught = ItemRegistry.Create(this.catchPool[Game1.random.Next(this.catchPool.Length)]);
            this.GiveOrDrop(player, location, caught);
        }

        player.Stamina = Math.Max(0, player.Stamina - data.StaminaCost);
        location.playSound("waterSlosh");
        Game1.addHUDMessage(new HUDMessage($"捕获了 {count} 个物品！", HUDMessage.newQuest_type));
        this.monitor.Log($"{player.Name} used {data.DisplayName} and caught {count} item(s).", LogLevel.Trace);
        return true;
    }

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

    private void GiveOrDrop(Farmer player, GameLocation location, Item item)
    {
        Item? leftover = player.addItemToInventory(item);
        if (leftover is null)
            return;

        Game1.createItemDebris(leftover, player.getStandingPosition(), player.FacingDirection, location);
    }
}
```

- [ ] **Step 3: Build to catch API errors**

Run:

```bash
dotnet build "FishingNetMod/FishingNetMod.csproj"
```

Expected: build succeeds. If API errors occur, adjust only the failing API calls while preserving the behavior described above.

### Task 4: Wire command and input in ModEntry

**Files:**
- Modify: `FishingNetMod/ModEntry.cs`

- [ ] **Step 1: Replace `ModEntry.cs` with command/input wiring**

Replace `FishingNetMod/ModEntry.cs` with exactly:

```csharp
using FishingNetMod.Data;
using FishingNetMod.Items;
using FishingNetMod.Mechanics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

namespace FishingNetMod;

internal sealed class ModEntry : Mod
{
    private FishingNetItemFactory? itemFactory;
    private ActiveFishingNet? activeFishingNet;

    public override void Entry(IModHelper helper)
    {
        this.itemFactory = new FishingNetItemFactory();
        this.activeFishingNet = new ActiveFishingNet(this.Monitor, this.itemFactory);

        helper.ConsoleCommands.Add(
            "fishing_net",
            "Fishing Net Mod debug command. Usage: fishing_net give <copper|iron|gold|iridium>",
            this.HandleFishingNetCommand);

        helper.Events.Input.ButtonPressed += this.OnButtonPressed;
        this.Monitor.Log("Fishing Net Mod loaded.", LogLevel.Info);
    }

    private void HandleFishingNetCommand(string command, string[] args)
    {
        if (args.Length != 2 || !string.Equals(args[0], "give", StringComparison.OrdinalIgnoreCase))
        {
            this.Monitor.Log("Usage: fishing_net give <copper|iron|gold|iridium>", LogLevel.Info);
            return;
        }

        if (!Context.IsWorldReady)
        {
            this.Monitor.Log("Load a save before using this command.", LogLevel.Info);
            return;
        }

        if (!NetLevelData.TryParse(args[1], out NetLevelData? data))
        {
            this.Monitor.Log("Unknown net level. Valid levels: copper, iron, gold, iridium.", LogLevel.Info);
            return;
        }

        Item item = this.itemFactory!.Create(data);
        Item? leftover = Game1.player.addItemToInventory(item);
        if (leftover is not null)
        {
            Game1.createItemDebris(leftover, Game1.player.getStandingPosition(), Game1.player.FacingDirection, Game1.player.currentLocation);
            this.Monitor.Log($"Inventory full; dropped {data.DisplayName} at the player's feet.", LogLevel.Info);
            return;
        }

        this.Monitor.Log($"Added {data.DisplayName} to your inventory.", LogLevel.Info);
    }

    private void OnButtonPressed(object? sender, ButtonPressedEventArgs e)
    {
        if (!Context.IsWorldReady || !e.Button.IsUseToolButton())
            return;

        if (this.activeFishingNet!.TryUse(Game1.player, Game1.currentLocation))
            this.Helper.Input.Suppress(e.Button);
    }
}
```

- [ ] **Step 2: Build the wired mod**

Run:

```bash
dotnet build "FishingNetMod/FishingNetMod.csproj"
```

Expected: build succeeds with `0 Error(s)` and deploys to `E:\SteamLibrary\steamapps\common\Stardew Valley\Mods\FishingNetMod`.

### Task 5: Run all automated checks

**Files:**
- Verify: `FishingNetMod/FishingNetMod.csproj`
- Verify: `FishingNetMod.Tests/FishingNetMod.Tests.csproj`

- [ ] **Step 1: Run all unit tests**

Run:

```bash
dotnet test "FishingNetMod.Tests/FishingNetMod.Tests.csproj"
```

Expected: PASS, all tests pass.

- [ ] **Step 2: Run clean build**

Run:

```bash
dotnet clean "FishingNetMod/FishingNetMod.csproj" && dotnet build "FishingNetMod/FishingNetMod.csproj"
```

Expected: clean and build both succeed with `0 Error(s)`.

### Task 6: Manual SMAPI verification

**Files:**
- Runtime verify: `E:\SteamLibrary\steamapps\common\Stardew Valley\StardewModdingAPI.exe`

- [ ] **Step 1: Launch SMAPI**

Run:

```bash
"E:/SteamLibrary/steamapps/common/Stardew Valley/StardewModdingAPI.exe"
```

Expected: SMAPI logs `Fishing Net Mod loaded.` and `Mods loaded and ready!`.

- [ ] **Step 2: Verify invalid command probe**

In the SMAPI console, enter:

```text
fishing_net give diamond
```

Expected console output includes:

```text
Unknown net level. Valid levels: copper, iron, gold, iridium.
```

- [ ] **Step 3: Verify copper net command**

In a loaded save, enter:

```text
fishing_net give copper
```

Expected console output includes:

```text
Added Copper Fishing Net to your inventory.
```

- [ ] **Step 4: Verify non-water use**

In-game, equip the copper fishing net, face a non-water tile, and press the tool-use button.

Expected: red message `这里不能撒网。`; stamina does not decrease.

- [ ] **Step 5: Verify water use**

In-game, equip the copper fishing net, face a water tile, and press the tool-use button.

Expected: HUD message `捕获了 2 个物品！` or `捕获了 3 个物品！`; 2-3 items are added to inventory or dropped if inventory is full; stamina decreases by 10.

---

## Self-Review

- Spec coverage: all included scope items are mapped to Tasks 1-6: level data, debug command, temporary item marker, active input handling, water detection, capture generation, stamina cost, invalid command handling, non-water behavior, inventory full fallback, and SMAPI runtime verification.
- Scope exclusions preserved: no Content Patcher pack, no passive placement, no quest chain, no multiplayer restriction, no custom texture, and no full `GameLocation.getFish(...)` integration.
- Placeholder scan: no TBD/TODO/implement-later language remains; every code-changing step includes exact file content.
- Type consistency: `NetLevel`, `NetLevelData`, `FishingNetItemFactory`, `ActiveFishingNet`, and `ModEntry` names/signatures match across tests and implementation steps.
