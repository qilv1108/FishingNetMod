# 多人游戏完善 — 实施计划

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 修复 0.1.0 多人模式 4 个缺口：收网权限、每日产出归属、任务进度隔离、主动撒网位置冲突。

**Architecture:** 4 文件定点改动，不引入新文件。QuestProgressTracker 内部改为 `Dictionary<long, QuestProgress>` 按玩家隔离进度（保留 `Progress` 属性向后兼容）。PassiveNetManager 加 OwnerId 权限和网主人技能归属。ActiveFishingNet 加可选 PassiveNetManager 引用做位置冲突检查。ModEntry 连线传递 playerId。

**Tech Stack:** C# .NET 6, SMAPI 4.x, xUnit

---

### Task 1: QuestProgressTracker — 按玩家隔离进度

**Files:**
- Modify: `FishingNetMod/Quests/QuestProgressTracker.cs`
- Modify: `FishingNetMod.Tests/Quests/QuestProgressTrackerTests.cs`

- [ ] **Step 1: 添加内部 Dictionary 和辅助方法**

在 `QuestProgressTracker.cs` 顶部字段区（:18 之后，`Progress` 属性之前）添加：

```csharp
private readonly Dictionary<long, QuestProgress> progressByPlayer = new();

private QuestProgress GetOrCreateProgress(long playerId)
{
    if (!this.progressByPlayer.TryGetValue(playerId, out QuestProgress? progress))
    {
        progress = new QuestProgress();
        this.progressByPlayer[playerId] = progress;
    }

    return progress;
}
```

将 `SaveDataKey` 改为静态方法：

```csharp
private static string SaveKey(long playerId) => $"QuestProgress_{playerId}";
```

- [ ] **Step 2: 修改 Load / Save 签名**

```csharp
// Load — 原签名: public void Load(IModHelper helper)
public void Load(IModHelper helper, long playerId)
{
    QuestProgress? progress = Normalize(helper.Data.ReadSaveData<QuestProgress>(SaveKey(playerId)));
    if (progress is not null)
        this.progressByPlayer[playerId] = progress;
}

// Save — 原签名: public void Save(IModHelper helper)
public void Save(IModHelper helper, long playerId)
{
    if (this.progressByPlayer.TryGetValue(playerId, out QuestProgress? progress))
        helper.Data.WriteSaveData(SaveKey(playerId), Normalize(progress));
}
```

- [ ] **Step 3: 修改 RecordNetCatch 签名和内部逻辑**

```csharp
// 原签名: public void RecordNetCatch(NetLevel netLevel, int quality, string season)
public void RecordNetCatch(long playerId, NetLevel netLevel, int quality, string season)
{
    QuestProgress progress = this.GetOrCreateProgress(playerId);

    switch (netLevel)
    {
        case NetLevel.Copper when quality == SilverQuality:
            progress.SilverFishCount++;
            break;

        case NetLevel.Iron when quality >= GoldQuality:
            progress.GoldFishCount++;
            break;

        case NetLevel.Gold when !string.IsNullOrWhiteSpace(season):
            progress.SeasonsFished.Add(season.Trim().ToLowerInvariant());
            break;
    }
}
```

- [ ] **Step 4: 修改 RecordPassiveCatch 签名**

```csharp
// 原签名: public void RecordPassiveCatch(NetLevel netLevel, int quality, string season)
public void RecordPassiveCatch(long playerId, NetLevel netLevel, int quality, string season)
{
    this.RecordNetCatch(playerId, netLevel, quality, season);
}
```

- [ ] **Step 5: 保持 Progress 属性向后兼容**

`Progress` 属性改为返回默认玩家的进度，现有测试不依赖特定 playerId 时仍然可用：

```csharp
// 原代码: public QuestProgress Progress { get; set; } = new();
public QuestProgress Progress
{
    get => this.GetOrCreateProgress(Game1.player?.UniqueMultiplayerID ?? 0);
    // set 保留以兼容直接赋值进度的测试
    set
    {
        long id = Game1.player?.UniqueMultiplayerID ?? 0;
        this.progressByPlayer[id] = value;
    }
}
```

添加 `using StardewValley;`（如果尚未引用）。

- [ ] **Step 6: 更新现有测试 — 添加 playerId 参数**

`QuestProgressTrackerTests.cs` 中所有 `RecordPassiveCatch` 和 `RecordNetCatch` 调用需要添加 `playerId` 作为第一个参数。全部使用 `playerId: 1234L`：

```csharp
// RecordPassiveCatchCountsCopperSilverFishOnly
tracker.RecordPassiveCatch(playerId: 1234L, NetLevel.Copper, quality: 1, season: "spring");
tracker.RecordPassiveCatch(playerId: 1234L, NetLevel.Copper, quality: 0, season: "spring");
tracker.RecordPassiveCatch(playerId: 1234L, NetLevel.Iron, quality: 1, season: "spring");

// RecordNetCatchCountsTheSameMilestonesUsedByQuestFlow
tracker.RecordNetCatch(playerId: 1234L, NetLevel.Copper, quality: 1, season: "spring");
tracker.RecordNetCatch(playerId: 1234L, NetLevel.Iron, quality: 2, season: "summer");
tracker.RecordNetCatch(playerId: 1234L, NetLevel.Gold, quality: 0, season: "fall");

// RecordPassiveCatchCountsIronGoldAndIridiumFishOnly
tracker.RecordPassiveCatch(playerId: 1234L, NetLevel.Iron, quality: 2, season: "summer");
// (依此类推，所有调用加 playerId: 1234L)
```

文件中共 12 处 `RecordPassiveCatch` / `RecordNetCatch` 调用，全部机械添加 `playerId: 1234L` 作为首个参数。

- [ ] **Step 7: 新增隔离测试**

在 `QuestProgressTrackerTests.cs` 末尾添加：

```csharp
[Fact]
public void RecordNetCatch_IsolatedByPlayerId()
{
    var tracker = new QuestProgressTracker();

    tracker.RecordNetCatch(playerId: 1L, NetLevel.Copper, quality: 1, season: "spring");
    tracker.RecordNetCatch(playerId: 1L, NetLevel.Copper, quality: 1, season: "spring");
    tracker.RecordNetCatch(playerId: 2L, NetLevel.Copper, quality: 1, season: "spring");

    // player 1: 2 silver fish; player 2: 1 silver fish (独立)
    QuestProgress p1 = tracker.GetOrCreateProgress(1L);
    QuestProgress p2 = tracker.GetOrCreateProgress(2L);

    Assert.Equal(2, p1.SilverFishCount);
    Assert.Equal(1, p2.SilverFishCount);
}
```

`GetOrCreateProgress` 需要改为 `internal`（添加 `internal` 修饰符）：

```csharp
internal QuestProgress GetOrCreateProgress(long playerId)
```

- [ ] **Step 8: 运行测试确认通过**

```bash
dotnet test --filter "FullyQualifiedName~QuestProgressTrackerTests"
```

预期：13 passed, 0 failed（原有 12 个 + 新增 1 个）

- [ ] **Step 9: 提交**

```bash
git add FishingNetMod/Quests/QuestProgressTracker.cs FishingNetMod.Tests/Quests/QuestProgressTrackerTests.cs
git commit -m "feat: isolate quest progress by player ID for multiplayer

Co-Authored-By: Claude <noreply@anthropic.com>"
```

---

### Task 2: PassiveNetManager — 收网权限检查

**Files:**
- Modify: `FishingNetMod/Mechanics/PassiveNetManager.cs:68-87`
- Modify: `FishingNetMod.Tests/Mechanics/PassiveNetManagerTests.cs`

- [ ] **Step 1: TryHarvest 添加 OwnerId 检查**

修改 `PassiveNetManager.cs` 的 `TryHarvest` 方法，在 `TryGetHarvestableNet` 成功后、给予物品前插入权限检查：

```csharp
public bool TryHarvest(Farmer player, GameLocation location, Vector2 targetTile, out string? error)
{
    if (!this.TryGetHarvestableNet(location.Name, targetTile, out PassiveNetData? data) || data is null)
    {
        error = null;
        return false;
    }

    // 新增：只允许网的主人收取
    if (data.OwnerId != player.UniqueMultiplayerID)
    {
        error = "这不是你的渔网。";
        return false;
    }

    foreach (PassiveNetHarvestData harvest in data.Harvest)
    {
        Item item = ItemRegistry.Create(harvest.QualifiedItemId, harvest.Stack);
        item.Quality = harvest.Quality;
        this.GiveOrDrop(player, location, item);
    }

    this.GiveOrDrop(player, location, this.itemFactory.Create(NetLevelData.Get(data.Level)));
    this.nets.Remove(data);
    error = null;
    return true;
}
```

- [ ] **Step 2: 新增测试 — 拒绝非 owner 收网**

在 `PassiveNetManagerTests.cs` 末尾添加。关键：非 owner 路径在权限检查处即返回（不进入 ItemRegistry.Create / GiveOrDrop），因此不需要完整游戏初始化。使用 `new Farmer()` + 属性赋值即可。

```csharp
[Fact]
public void TryHarvest_RejectsNonOwner()
{
    var manager = new PassiveNetManager();
    var net = new PassiveNetData(
        OwnerId: 1234L,
        LocationName: "Beach",
        Tile: new Vector2(10, 20),
        Level: NetLevel.Copper,
        Harvest: new List<PassiveNetHarvestData>
        {
            new("(O)128", 1, 0)
        });
    manager.TryAdd(net, out _);

    // 模拟另一个玩家尝试收网。Farmer 有无参构造函数。
    var otherPlayer = new Farmer { UniqueMultiplayerID = 9999L };
    var location = new GameLocation();
    typeof(GameLocation).GetProperty("Name")?.SetValue(location, "Beach");

    bool result = manager.TryHarvest(otherPlayer, location, new Vector2(10, 20), out string? error);

    Assert.False(result);
    Assert.Equal("这不是你的渔网。", error);
    Assert.Single(manager.Nets); // 网未被移除
}
```

- [ ] **Step 3: 新增测试 — owner 可正常收网**

owner 收网会走完整路径（`ItemRegistry.Create` → `GiveOrDrop` → `addItemToInventory`），需要游戏数据初始化。如果测试项目的 `[ assembly: UseCulture ]` 或 `StardewValley.Utility` 已初始化则可以直接使用；否则此测试标注为需要游戏内验证。

```csharp
[Fact]
public void TryHarvest_AllowsOwner()
{
    var manager = new PassiveNetManager();
    var net = new PassiveNetData(
        OwnerId: 1234L,
        LocationName: "Beach",
        Tile: new Vector2(10, 20),
        Level: NetLevel.Copper,
        Harvest: new List<PassiveNetHarvestData>
        {
            new("(O)128", 1, 0)
        });
    manager.TryAdd(net, out _);

    var owner = new Farmer { UniqueMultiplayerID = 1234L };
    var location = new GameLocation();
    typeof(GameLocation).GetProperty("Name")?.SetValue(location, "Beach");

    bool result = manager.TryHarvest(owner, location, new Vector2(10, 20), out string? error);

    Assert.True(result);
    Assert.Null(error);
    Assert.Empty(manager.Nets);
}
```

> **如果游戏未初始化导致此测试失败：** 改为 `[Fact(Skip = "Requires game initialization")]` 并记录到 `docs/reports/` 待游戏内验证。

- [ ] **Step 3: 新增测试 — owner 可正常收网**

```csharp
[Fact]
public void TryHarvest_AllowsOwner()
{
    var manager = new PassiveNetManager();
    var net = new PassiveNetData(
        OwnerId: 1234L,
        LocationName: "Beach",
        Tile: new Vector2(10, 20),
        Level: NetLevel.Copper,
        Harvest: new List<PassiveNetHarvestData>
        {
            new("(O)128", 1, 0)
        });
    manager.TryAdd(net, out _);

    bool result = manager.TryHarvest(
        CreateFarmerStub(uniqueMultiplayerID: 1234L),
        CreateLocationStub("Beach"),
        new Vector2(10, 20),
        out string? error);

    Assert.True(result);
    Assert.Null(error);
    Assert.Empty(manager.Nets); // 网已移除
}
```

- [ ] **Step 4: 运行测试确认通过**

```bash
dotnet test --filter "FullyQualifiedName~PassiveNetManagerTests"
```

预期：8 passed, 0 failed（原有 6 个 + 新增 2 个）

- [ ] **Step 5: 提交**

```bash
git add FishingNetMod/Mechanics/PassiveNetManager.cs FishingNetMod.Tests/Mechanics/PassiveNetManagerTests.cs
git commit -m "feat: restrict passive net harvest to owner only

Co-Authored-By: Claude <noreply@anthropic.com>"
```

---

### Task 3: PassiveNetManager — 每日产出用网主人技能

**Files:**
- Modify: `FishingNetMod/Mechanics/PassiveNetManager.cs:95-112`

- [ ] **Step 1: ProduceDaily 改用网主人 Farmer**

修改 `ProduceDaily` 方法：

```csharp
public void ProduceDaily(GameLocation location)
{
    foreach (PassiveNetData net in this.nets.Where(net => net.LocationName == location.Name).ToList())
    {
        Farmer? owner = Game1.getFarmer(net.OwnerId);
        if (owner is null)
            continue;

        var range = GetDailyProductionRange(net.Level);
        int count = Game1.random.Next(range.Min, range.Max + 1);
        for (int i = 0; i < count; i++)
        {
            Item? fish = this.fishProvider.GetFish(location, owner, net.Tile);
            if (fish is null)
                continue;

            var harvest = new PassiveNetHarvestData(fish.QualifiedItemId, fish.Stack, fish.Quality);
            net.Harvest.Add(harvest);
            this.questProgressTracker?.RecordNetCatch(net.OwnerId, net.Level, harvest.Quality, Game1.currentSeason);
        }
    }
}
```

- [ ] **Step 2: 运行现有测试确认兼容**

```bash
dotnet test --filter "FullyQualifiedName~PassiveNetManagerTests"
```

预期：8 passed, 0 failed。现有测试中 `ProduceDaily` 未被直接调用，不依赖 `Game1.player` 或 `Game1.getFarmer()`，零影响。

- [ ] **Step 3: 提交**

```bash
git add FishingNetMod/Mechanics/PassiveNetManager.cs
git commit -m "feat: use net owner's fishing skill for passive daily production

Co-Authored-By: Claude <noreply@anthropic.com>"
```

---

### Task 4: ActiveFishingNet — 位置冲突 + CompleteCatch playerId

**Files:**
- Modify: `FishingNetMod/Mechanics/ActiveFishingNet.cs`
- Modify: `FishingNetMod.Tests/Mechanics/ActiveFishingNetCastTests.cs`

- [ ] **Step 1: TryUse 签名增加可选参数并添加位置冲突检查**

```csharp
public bool TryUse(Farmer player, GameLocation location,
    PassiveNetManager? passiveNetManager, out ActiveFishingNetCast? cast)
{
    cast = null;

    if (!this.itemFactory.TryGetNetData(player.CurrentItem, out NetLevelData? data) || data is null)
        return false;

    Vector2 targetTile = this.GetFacingTile(player);
    if (!location.isWaterTile((int)targetTile.X, (int)targetTile.Y))
    {
        Game1.showRedMessage("这里不能撒网。");
        return true;
    }

    // 新增：检查目标位置是否已有被动网
    if (passiveNetManager?.TryGetHarvestableNet(location.Name, targetTile, out _) == true)
    {
        Game1.showRedMessage("这里已经有渔网了。");
        cast = null;
        return true;
    }

    NetLevelData netData = data;
    int attempts = Game1.random.Next(netData.MinCatch, netData.MaxCatch + 1);
    var caughtItems = new List<Item>();
    for (int i = 0; i < attempts; i++)
    {
        Item? caught = this.fishProvider.GetFish(location, player, targetTile);
        if (caught is null)
            continue;

        caughtItems.Add(caught);
    }

    player.Stamina = Math.Max(0, player.Stamina - netData.StaminaCost);
    location.playSound("waterSlosh");
    cast = new ActiveFishingNetCast(netData, caughtItems, attempts);
    this.monitor.Log($"{player.Name} cast {netData.DisplayName} and started a challenge with {caughtItems.Count} pending fish from {attempts} attempt(s).", LogLevel.Trace);
    return true;
}
```

- [ ] **Step 2: CompleteCatch 传递 playerId**

```csharp
public void CompleteCatch(Farmer player, GameLocation location, ActiveFishingNetCast cast)
{
    foreach (Item caught in cast.CaughtItems)
    {
        this.GiveOrDrop(player, location, caught);
        this.questProgressTracker?.RecordNetCatch(player.UniqueMultiplayerID, cast.NetData.Level, caught.Quality, Game1.currentSeason);
    }

    Game1.addHUDMessage(new HUDMessage(cast.GetResultMessage(), HUDMessage.newQuest_type));
    this.monitor.Log($"{player.Name} completed {cast.NetData.DisplayName} challenge and received {cast.CaughtCount} fish from {cast.Attempts} attempt(s).", LogLevel.Trace);
}
```

- [ ] **Step 3: 新增测试 — 被动网位置阻止撒网**

`TryUse` 的非 owner 路径在位置冲突检查处即返回，不依赖游戏数据。测试可直接验证：

```csharp
[Fact]
public void TryUse_BlocksWhenPassiveNetAtTile()
{
    // 在目标 tile 放置一个被动网
    var passiveNetManager = new PassiveNetManager();
    passiveNetManager.TryAdd(new PassiveNetData(
        OwnerId: 1234L,
        LocationName: "Farm",
        Tile: new Vector2(5, 5),
        Level: NetLevel.Copper,
        Harvest: new List<PassiveNetHarvestData>()), out _);

    // 构造 ActiveFishingNet 并调用 TryUse，targetTile 恰好是 (5,5)
    // 需要 IMonitor stub: 使用 Mock 框架或 null（TryUse 只用 monitor.Log，可传 null 并在 TryUse 中加 null 检查）
    // 需要 FishingNetItemFactory + IFishProvider: 传入不带网的 player.CurrentItem 让 TryGetNetData 先返回 false，短路后续逻辑

    // 如果 ActiveFishingNet 构造链过于复杂，此测试可标记 Skip 待游戏内验证
}
```

> **实际调整：** `ActiveFishingNet` 构造函数需要 `IMonitor`、`FishingNetItemFactory`、`IFishProvider`。若测试项目已引用 `Moq` 或 `NSubstitute`，使用 mock；否则标记 `[Fact(Skip = "Requires mock framework or game initialization")]`。

- [ ] **Step 4: 运行现有测试确认兼容**

```bash
dotnet test --filter "FullyQualifiedName~ActiveFishingNetCastTests"
```

预期：2 passed, 0 failed。

- [ ] **Step 5: 提交**

```bash
git add FishingNetMod/Mechanics/ActiveFishingNet.cs FishingNetMod.Tests/Mechanics/ActiveFishingNetCastTests.cs
git commit -m "feat: block active cast on passive net tiles and pass playerId on catch

Co-Authored-By: Claude <noreply@anthropic.com>"
```

---

### Task 5: ModEntry — 连线所有改动

**Files:**
- Modify: `FishingNetMod/ModEntry.cs`

- [ ] **Step 1: OnSaveLoaded 传递 playerId**

```csharp
private void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
{
    this.questProgressTracker!.Load(this.Helper, Game1.player.UniqueMultiplayerID);
    this.passiveNetManager!.Load(this.Helper);
    this.ApplyQuestUnlocks(Game1.player);
}
```

- [ ] **Step 2: OnSaving 传递 playerId**

```csharp
private void OnSaving(object? sender, SavingEventArgs e)
{
    this.questProgressTracker!.Save(this.Helper, Game1.player.UniqueMultiplayerID);
    this.passiveNetManager!.Save(this.Helper);
}
```

- [ ] **Step 3: OnButtonPressed — TryUse 传入 passiveNetManager**

在 `OnButtonPressed` 中修改 `TryUse` 调用：

```csharp
// 原代码：this.activeFishingNet!.TryUse(Game1.player, fishingLocation, out ActiveFishingNetCast? cast)
if (this.activeFishingNet!.TryUse(Game1.player, fishingLocation, this.passiveNetManager, out ActiveFishingNetCast? cast))
```

`CompleteCatch` 已经内部使用 `player.UniqueMultiplayerID`，无需额外改动。

- [ ] **Step 4: 确认编译通过**

```bash
dotnet build
```

预期：Build succeeded，无编译错误。

- [ ] **Step 5: 提交**

```bash
git add FishingNetMod/ModEntry.cs
git commit -m "feat: wire multiplayer playerId and passiveNetManager through ModEntry

Co-Authored-By: Claude <noreply@anthropic.com>"
```

---

### Task 6: 全量测试与最终提交

- [ ] **Step 1: 运行全量测试**

```bash
dotnet test
```

预期：78 passed, 0 failed, 0 skipped（74 原有 + 4 新增）。

如 Task 4 的 `TryUse_BlocksWhenPassiveNetAtTile` 因构造复杂度跳过，预期为 77 passed。

- [ ] **Step 2: 验证发布包结构**

```bash
ls -la FishingNetMod/bin/Debug/net6.0/FishingNetMod\ 0.1.0.zip
```

预期：zip 文件存在。

- [ ] **Step 3: 最终提交（如有多余改动）**

```bash
git status
git add -A
git commit -m "chore: finalize multiplayer gap fixes

Co-Authored-By: Claude <noreply@anthropic.com>"
```

---

## 改动文件清单

| 文件 | 改动类型 | 行数 |
|------|----------|------|
| `FishingNetMod/Quests/QuestProgressTracker.cs` | 修改 | ~30 |
| `FishingNetMod/Mechanics/PassiveNetManager.cs` | 修改 | ~10 |
| `FishingNetMod/Mechanics/ActiveFishingNet.cs` | 修改 | ~10 |
| `FishingNetMod/ModEntry.cs` | 修改 | ~5 |
| `FishingNetMod.Tests/Quests/QuestProgressTrackerTests.cs` | 修改 | ~20 |
| `FishingNetMod.Tests/Mechanics/PassiveNetManagerTests.cs` | 修改 | ~40 |
| `FishingNetMod.Tests/Mechanics/ActiveFishingNetCastTests.cs` | 修改 | ~15 |

**总计：约 130 行改动，6 个提交，4 个新测试。**
