# 2 个 Skip 测试修复 — 实施计划

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task.

**Goal:** 给 PassiveNetManager 加 deliverHarvest 委托、ActiveFishingNet 加 getHeldNet+isWaterTileFunc 委托，修复 TryHarvest_AllowsOwner 和 TryUse_BlocksWhenPassiveNetAtTile

**Architecture:** 2 个生产文件各加可选委托参数，测试传入 stub，跳过游戏依赖步骤。

**Tech Stack:** C# .NET 6, SMAPI 4.x, xUnit

---

### Task 1: PassiveNetManager — deliverHarvest 注入 + TryHarvest_AllowsOwner

**Files:**
- Modify: `FishingNetMod/Mechanics/PassiveNetManager.cs`
- Modify: `FishingNetMod.Tests/Mechanics/PassiveNetManagerTests.cs`

- [ ] **Step 1: 在 PassiveNetManager 中添加 deliverHarvest 字段和构造参数**

读取 `PassiveNetManager.cs`，在字段区添加：

```csharp
private readonly Action<Farmer, GameLocation, PassiveNetData>? deliverHarvest;
```

构造函数签名改为：

```csharp
public PassiveNetManager(
    FishingNetItemFactory itemFactory,
    IFishProvider fishProvider,
    QuestProgressTracker? questProgressTracker = null,
    ITranslationHelper? translation = null,
    Func<string, int, Item>? createHarvestItem = null,
    Action<Farmer, GameLocation, PassiveNetData>? deliverHarvest = null)
{
    this.itemFactory = itemFactory;
    this.fishProvider = fishProvider;
    this.questProgressTracker = questProgressTracker;
    this.translation = translation;
    this.createHarvestItem = createHarvestItem ?? ItemRegistry.Create;
    this.deliverHarvest = deliverHarvest;
}
```

- [ ] **Step 2: 在 TryHarvest 中分流交付逻辑**

将 `TryHarvest` 中的 foreach 循环包裹在 `deliverHarvest` 检查中：

```csharp
if (this.deliverHarvest != null)
{
    this.deliverHarvest(player, location, data);
}
else
{
    foreach (PassiveNetHarvestData harvest in data.Harvest)
    {
        Item item = this.createHarvestItem(harvest.QualifiedItemId, harvest.Stack);
        item.Quality = harvest.Quality;
        this.GiveOrDrop(player, location, item);
    }

    this.GiveOrDrop(player, location, this.itemFactory.Create(NetLevelData.Get(data.Level)));
}

this.nets.Remove(data);
error = null;
return true;
```

- [ ] **Step 3: 编译验证**

```bash
dotnet build
```

- [ ] **Step 4: 更新测试，移除 Skip**

在 `PassiveNetManagerTests.cs` 中修改 `TryHarvest_AllowsOwner`：

```csharp
[Fact]
public void TryHarvest_AllowsOwner()
{
    var manager = new PassiveNetManager(
        new FishingNetItemFactory(),
        new VanillaFishProvider(),
        questProgressTracker: null,
        translation: null,
        createHarvestItem: null,
        deliverHarvest: (player, location, data) => { });

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

    var owner = (Farmer)FormatterServices.GetUninitializedObject(typeof(Farmer));
    SetUniqueMultiplayerID(owner, 1234L);
    var location = (GameLocation)FormatterServices.GetUninitializedObject(typeof(GameLocation));
    SetLocationName(location, "Beach");

    bool result = manager.TryHarvest(owner, location, new Vector2(10, 20), out string? error);

    Assert.True(result);
    Assert.Null(error);
    Assert.Empty(manager.Nets);
}
```

移除 `Func<string, int, Item> createItem = ...` 声明（不再需要）。

- [ ] **Step 5: 运行测试验证通过**

```bash
dotnet test --filter "FullyQualifiedName~TryHarvest_AllowsOwner"
```

预期：PASS。

- [ ] **Step 6: 运行 PassiveNetManager 全量确认**

```bash
dotnet test --filter "FullyQualifiedName~PassiveNetManagerTests"
```

预期：11 passed, 0 failed, 0 skipped。

- [ ] **Step 7: 提交**

```bash
git add FishingNetMod/Mechanics/PassiveNetManager.cs FishingNetMod.Tests/Mechanics/PassiveNetManagerTests.cs
git commit -m "feat: inject deliverHarvest delegate to enable TryHarvest_AllowsOwner test

Co-Authored-By: Claude <noreply@anthropic.com>"
```

---

### Task 2: ActiveFishingNet — getHeldNet+isWaterTileFunc 注入 + TryUse_BlocksWhenPassiveNetAtTile

**Files:**
- Modify: `FishingNetMod/Mechanics/ActiveFishingNet.cs`
- Modify: `FishingNetMod.Tests/Mechanics/ActiveFishingNetCastTests.cs`

- [ ] **Step 1: 添加字段和构造参数**

在 `ActiveFishingNet.cs` 字段区添加：

```csharp
private readonly Func<Farmer, NetLevelData?>? getHeldNet;
private readonly Func<GameLocation, int, int, bool>? isWaterTileFunc;
```

构造函数签名改为：

```csharp
public ActiveFishingNet(
    IMonitor monitor,
    FishingNetItemFactory itemFactory,
    IFishProvider fishProvider,
    QuestProgressTracker? questProgressTracker = null,
    ITranslationHelper? translation = null,
    Func<Farmer, NetLevelData?>? getHeldNet = null,
    Func<GameLocation, int, int, bool>? isWaterTileFunc = null)
{
    this.monitor = monitor;
    this.itemFactory = itemFactory;
    this.fishProvider = fishProvider;
    this.questProgressTracker = questProgressTracker;
    this.translation = translation;
    this.getHeldNet = getHeldNet;
    this.isWaterTileFunc = isWaterTileFunc;
}
```

- [ ] **Step 2: 在 TryUse 中分流手持网识别**

将现有 `TryGetNetData` 调用改为先检查注入委托：

```csharp
NetLevelData? data;
if (this.getHeldNet != null)
{
    data = this.getHeldNet(player);
    if (data is null)
    {
        cast = null;
        return false;
    }
}
else if (!this.itemFactory.TryGetNetData(player.CurrentItem, out data) || data is null)
{
    cast = null;
    return false;
}
```

- [ ] **Step 3: 在 TryUse 中分流水域检查**

将 `isWaterTile` 调用改为先检查注入委托：

```csharp
Vector2 targetTile = this.GetFacingTile(player);
if (this.isWaterTileFunc != null
    ? !this.isWaterTileFunc(location, (int)targetTile.X, (int)targetTile.Y)
    : !location.isWaterTile((int)targetTile.X, (int)targetTile.Y))
{
    Game1.showRedMessage(T("error.cannot-cast", "这里不能撒网。"));
    cast = null;
    return true;
}
```

- [ ] **Step 4: 编译验证**

```bash
dotnet build
```

- [ ] **Step 5: 重写测试**

将 `ActiveFishingNetCastTests.cs` 中的 `TryUse_BlocksWhenPassiveNetAtTile` 全部替换为简洁版本：

```csharp
[Fact]
public void TryUse_BlocksWhenPassiveNetAtTile()
{
    // 在 (1, 0) 放置一个被动网
    var passiveNetManager = new PassiveNetManager();
    passiveNetManager.TryAdd(new PassiveNetData(
        OwnerId: 9999L,
        LocationName: "Farm",
        Tile: new Vector2(1, 0),
        Level: NetLevel.Copper,
        Harvest: new List<PassiveNetHarvestData>()), out _);

    // 构造 ActiveFishingNet，注入 stub 委托
    var activeNet = new ActiveFishingNet(
        new DummyMonitor(),
        new FishingNetItemFactory(),
        new VanillaFishProvider(),
        getHeldNet: _ => NetLevelData.All.First(d => d.Level == NetLevel.Copper),
        isWaterTileFunc: (loc, x, y) => true);

    // 构造最小 player（不需要 CurrentItem，因为 getHeldNet 已被注入）
    var player = (Farmer)FormatterServices.GetUninitializedObject(typeof(Farmer));
    SetUniqueMultiplayerID(player, 1111L);

    // 构造 location（不需要 isWaterTile 工作，因为 isWaterTileFunc 已被注入）
    var location = (GameLocation)FormatterServices.GetUninitializedObject(typeof(GameLocation));
    SetLocationName(location, "Farm");

    bool result = activeNet.TryUse(player, location, passiveNetManager, out var cast);

    Assert.True(result);
    Assert.Null(cast);
    Assert.Single(passiveNetManager.Nets);
}
```

删除以下不再需要的反射桩代码：
- 所有 `temporaryItem` / `netItems` / `currentItem` / `modData` / `FacingDirection` 反射设置
- `using System.Reflection;` 和 `using System.Runtime.Serialization;`（如果只在这个测试用）
- 保留 `DummyMonitor` 类、`SetUniqueMultiplayerID`、`SetLocationName` 辅助方法

- [ ] **Step 6: 运行测试验证通过**

```bash
dotnet test --filter "FullyQualifiedName~TryUse_BlocksWhenPassiveNetAtTile"
```

预期：PASS。

- [ ] **Step 7: 运行全量 ActiveFishingNetCastTests**

```bash
dotnet test --filter "FullyQualifiedName~ActiveFishingNetCastTests"
```

预期：3 passed, 0 failed, 0 skipped。

- [ ] **Step 8: 提交**

```bash
git add FishingNetMod/Mechanics/ActiveFishingNet.cs FishingNetMod.Tests/Mechanics/ActiveFishingNetCastTests.cs
git commit -m "feat: inject getHeldNet and isWaterTileFunc delegates to enable TryUse_BlocksWhenPassiveNetAtTile test

Co-Authored-By: Claude <noreply@anthropic.com>"
```

---

### Task 3: 全量验证

- [ ] **Step 1: 全量测试**

```bash
dotnet test
```

预期：78 passed, 0 failed, 0 skipped。

- [ ] **Step 2: 最终提交（如有残留）**

```bash
git status
git add -A
git commit -m "chore: finalize skip test fixes

Co-Authored-By: Claude <noreply@anthropic.com>" # 如有必要
```
