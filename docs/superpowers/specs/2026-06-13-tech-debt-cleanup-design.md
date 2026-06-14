# 技术债务清理 — 设计文档

> 日期：2026-06-13  
> 版本：`0.1.0` post-release  
> 目标：消除已知技术债务 4 项

---

## 一、范围

本轮覆盖以下 4 项技术债务：

| # | 问题 | 优先级 |
|---|------|--------|
| 1 | `Game1.getFarmer(long)` CS0618 编译警告 | 低 |
| 2 | C# 硬编码中文字符串未走 SMAPI i18n | 中 |
| 3 | 历史设计文档中过时类名与当前代码不一致 | 低 |
| 4 | 2 个测试因需游戏初始化被 Skip | 中 |

范围外：不新增功能、不重做收网界面、不引入新依赖包。

---

## 二、详细设计

### 2.1 CS0618 编译警告 `PassiveNetManager.cs:105`

当前代码：

```csharp
Farmer? owner = Game1.getFarmer(net.OwnerId);
```

改为 SMAPI 4.x 推荐写法：

```csharp
Farmer? owner = Game1.GetPlayer(net.OwnerId, onlineOnly: true) ?? Game1.MasterPlayer;
```

`ProduceDaily` 不被单元测试直接调用，测试影响为零。

---

### 2.2 i18n 翻译提取

#### 新增文件

- `FishingNetMod/i18n/default.json` — 英文回退
- `FishingNetMod/i18n/zh.json` — 中文翻译

SMAPI 自动根据游戏语言选择翻译文件，无需额外代码控制。

#### 翻译 Key 清单（14 个）

| Key | EN (default) | ZH | 使用位置 |
|-----|-------------|-----|----------|
| `challenge.failure` | Fishing failed. | 捕鱼失败。 | ModEntry |
| `challenge.title` | Fishing Challenge | 捕鱼挑战 | ModEntry |
| `challenge.instruction` | Press the numbers in order within 30 seconds. | 按顺序输入显示的数字，在 30 秒内完成。 | ModEntry |
| `harvest.failure` | Harvest failed. | 收网失败。 | ModEntry |
| `net.placed` | Fishing net placed. | 渔网已放置。 | ModEntry |
| `harvest.success` | Net harvested. | 收网成功。 | ModEntry |
| `error.already-placed` | You already have a fishing net placed. | 你已经放置了一个渔网。 | PassiveNetManager |
| `error.tile-occupied` | There is already a fishing net here. | 这里已经有渔网了。 | PassiveNetManager |
| `error.cannot-place` | Cannot place a fishing net here. | 这里不能放置渔网。 | PassiveNetManager |
| `error.not-your-net` | This is not your fishing net. | 这不是你的渔网。 | PassiveNetManager |
| `error.cannot-cast` | Cannot cast a net here. | 这里不能撒网。 | ActiveFishingNet |
| `error.tile-has-net` | There is already a fishing net here. | 这里已经有渔网了。 | ActiveFishingNet |
| `cast.caught` | Caught {{count}} fish! | 捕获了 {{count}} 条鱼！ | ActiveFishingNetCast |
| `cast.no-fish` | No fish caught. | 没有捕到鱼。 | ActiveFishingNetCast |

> 注：`error.tile-occupied` 和 `error.tile-has-net` 共用相同中文文本但语义不同（一个是放置冲突，一个是撒网冲突），分成两个 key 以便将来独立调整。

#### 注入方式

3 个类各加一个可选 `ITranslationHelper? translation = null` 构造参数：

- **`ActiveFishingNet`**：已有 4 参数构造，加第 5 可选参数
- **`PassiveNetManager`**：已有 3 参数构造，加第 4 可选参数；无参构造传 null
- **`ActiveFishingNetCast.GetResultMessage()`**：加可选参数 `ITranslationHelper? translation = null`

每个类内部添加私有辅助方法：

```csharp
private string T(string key, string fallback)
    => this.translation?.Get(key).ToString() ?? fallback;
```

带 token 的版本：

```csharp
private string T(string key, object tokens, string fallback)
    => this.translation?.Get(key).Tokens(tokens).ToString() ?? fallback;
```

**`ModEntry`** 中直接使用 `this.Helper.Translation`，不需要辅助方法。

**向后兼容：** `translation` 为 null 时回退到现有硬编码中文字符串。所有现有测试不传 `translation`，保持原行为不变。

---

### 2.3 历史文档类名对照表

**文件：** `2026-05-20-fishing-net-implementation-design.md`

该文档已标注"历史设计文档"。在末尾追加类名对照章节：

```markdown
## 类名演化对照

本文档中的类名基于早期设计，当前实现已有变化：

| 早期设计名 | 当前实现名 | 说明 |
|-----------|-----------|------|
| PassiveNetObject | PassiveNetData | 被动网数据记录 |
| NetPlacementManager | PassiveNetManager | 被动网放置/收取/产出管理 |
| (无对应) | PassiveNetRenderer | 世界内渔网渲染 |
| FishingNetMod | ModEntry | SMAPI 入口类 |
| ChallengeMinigame | NetHarvestChallengeMenu | 数字挑战 UI |
```

---

### 2.4 两个 Skip 测试修复

#### 4a. `TryHarvest_AllowsOwner`

**问题：** `TryHarvest` 在权限检查通过后调用 `ItemRegistry.Create(harvest.QualifiedItemId, harvest.Stack)`，测试环境无游戏数据导致失败。

**方案：** 给 `PassiveNetManager` 注入 `createItem` 委托。

```diff
- private readonly FishingNetItemFactory itemFactory;
+ private readonly Func<string, int, Item> createHarvestItem;

  public PassiveNetManager(
      FishingNetItemFactory itemFactory, 
      IFishProvider fishProvider, 
-     QuestProgressTracker? questProgressTracker = null)
+     QuestProgressTracker? questProgressTracker = null,
+     Func<string, int, Item>? createHarvestItem = null)
  {
+     this.createHarvestItem = createHarvestItem ?? ItemRegistry.Create;
  }
```

`TryHarvest` 中：

```diff
- Item item = ItemRegistry.Create(harvest.QualifiedItemId, harvest.Stack);
+ Item item = this.createHarvestItem(harvest.QualifiedItemId, harvest.Stack);
```

测试中传入轻量 stub：

```csharp
Func<string, int, Item> createItem = (id, stack) =>
    (Item)FormatterServices.GetUninitializedObject(typeof(SObject));
var manager = new PassiveNetManager(..., createHarvestItem: createItem);
```

验证 `TryHarvest` 返回 true、error 为 null、网被移除。

**注意：** `GiveOrDrop` 中的 `player.addItemToInventory(item)` 在未初始化 farmer 上调用时行为不确定，但即便它抛出异常也是测试通过之后的事。测试核心验证的是"owner 通过了权限检查并进入了 harvest 逻辑"。

#### 4b. `TryUse_BlocksWhenPassiveNetAtTile`

**方案：** 利用现有 `FormatterServices` + 反射模式构造所有依赖：

- `IMonitor` stub
- `FishingNetItemFactory` 实例
- `IFishProvider` stub
- 持有铜网的 player（`SObject` + `modData`）

关键验证：当目标 tile 已有被动网时，`TryUse` 返回 true 且 `cast` 为 null。

**测试结构：**

```csharp
[Fact]
public void TryUse_BlocksWhenPassiveNetAtTile()
{
    // 1. 构造被动网管理器，放置一个网在 (1, 0)（面朝上的前方）
    var passiveNetManager = new PassiveNetManager();
    passiveNetManager.TryAdd(new PassiveNetData(
        OwnerId: 9999L,
        LocationName: "Farm",
        Tile: new Vector2(1, 0),
        Level: NetLevel.Copper,
        Harvest: new List<PassiveNetHarvestData>()), out _);

    // 2. 构造 ActiveFishingNet（IMonitor 可用 null，构造时加 null 守卫）
    var activeNet = new ActiveFishingNet(
        monitor: null!,
        new FishingNetItemFactory(),
        new VanillaFishProvider());

    // 3. 构造 player（位置 (1,1)，面朝上，手持铜网）
    var player = CreatePlayerWithCopperNetAt(new Vector2(1, 1), facingDirection: 0); // 0 = up

    // 4. 构造 location（Farm，水域 tile）
    var location = CreateWaterLocation("Farm", new Vector2(1, 0));

    // 5. 验证
    bool result = activeNet.TryUse(player, location, passiveNetManager, out var cast);

    Assert.True(result);       // 被处理了（不算未处理）
    Assert.Null(cast);         // 但没有创建 cast（被阻止）
}
```

`CreatePlayerWithCopperNetAt` 和 `CreateWaterLocation` 使用与现有 `SetUniqueMultiplayerID`/`SetLocationName` 相同的 `FormatterServices` + 反射模式。

---

## 三、测试策略

### 现有测试

76 个全部零改动。所有修改均为"加可选参数"，向后兼容。

### 改动后预期

| 指标 | 当前 | 改动后 |
|------|------|--------|
| passed | 76 | 78 |
| failed | 0 | 0 |
| skipped | 2 | 0 |
| total | 78 | 78 |

---

## 四、改动文件汇总

| 文件 | 改动类型 | 预估行数 |
|------|----------|----------|
| `FishingNetMod/Mechanics/PassiveNetManager.cs` | 修改 | ~10 |
| `FishingNetMod/Mechanics/ActiveFishingNet.cs` | 修改 | ~8 |
| `FishingNetMod/Mechanics/ActiveFishingNetCast.cs` | 修改 | ~6 |
| `FishingNetMod/ModEntry.cs` | 修改 | ~15 |
| `FishingNetMod/i18n/default.json` | 新增 | ~20 |
| `FishingNetMod/i18n/zh.json` | 新增 | ~20 |
| `2026-05-20-fishing-net-implementation-design.md` | 修改 | ~15 |
| `PassiveNetManagerTests.cs` | 修改 | ~25 |
| `ActiveFishingNetCastTests.cs` | 修改 | ~35 |
| **总计** | | **~154 行** |

---

## 五、验收标准

1. `dotnet build` 无编译错误、无 CS0618 警告
2. `dotnet test` 78 passed, 0 failed, 0 skipped
3. 切换游戏语言后 C# 提示文本跟随 SMAPI 翻译切换
4. 历史设计文档末尾有类名对照表
