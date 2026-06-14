# 技术债务清理 — 实施计划

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 消除 4 项已知技术债务：CS0618 编译警告、C# i18n 硬编码中文、历史文档过时类名、2 个 Skip 测试。

**Architecture:** 4 文件注入可选参数（向后兼容），2 个新增 i18n JSON，1 个历史文档更新，2 个测试修复。不改现有测试签名。

**Tech Stack:** C# .NET 6, SMAPI 4.x, xUnit, FormatterServices 反射桩

---

### Task 1: 修复 CS0618 编译警告

**Files:**
- Modify: `FishingNetMod/Mechanics/PassiveNetManager.cs:105`

- [ ] **Step 1: 替换过时 API**

`PassiveNetManager.cs:105`：

```diff
- Farmer? owner = Game1.getFarmer(net.OwnerId);
+ Farmer? owner = Game1.GetPlayer(net.OwnerId, onlineOnly: true) ?? Game1.MasterPlayer;
```

- [ ] **Step 2: 编译验证**

```bash
dotnet build
```

预期：Build succeeded，无 CS0618 警告。

- [ ] **Step 3: 提交**

```bash
git add FishingNetMod/Mechanics/PassiveNetManager.cs
git commit -m "fix: replace deprecated Game1.getFarmer with Game1.GetPlayer

Co-Authored-By: Claude <noreply@anthropic.com>"
```

---

### Task 2: 创建 i18n 翻译文件

**Files:**
- Create: `FishingNetMod/i18n/default.json`
- Create: `FishingNetMod/i18n/zh.json`

- [ ] **Step 1: 创建英文回退文件 `FishingNetMod/i18n/default.json`**

```json
{
  "challenge.failure": "Fishing failed.",
  "challenge.title": "Fishing Challenge",
  "challenge.instruction": "Press the numbers in order within 30 seconds.",
  "harvest.failure": "Harvest failed.",
  "net.placed": "Fishing net placed.",
  "harvest.success": "Net harvested.",
  "error.already-placed": "You already have a fishing net placed.",
  "error.tile-occupied": "There is already a fishing net here.",
  "error.cannot-place": "Cannot place a fishing net here.",
  "error.not-your-net": "This is not your fishing net.",
  "error.cannot-cast": "Cannot cast a net here.",
  "error.tile-has-net": "There is already a fishing net here.",
  "cast.caught": "Caught {{count}} fish!",
  "cast.no-fish": "No fish caught."
}
```

- [ ] **Step 2: 创建中文翻译文件 `FishingNetMod/i18n/zh.json`**

```json
{
  "challenge.failure": "捕鱼失败。",
  "challenge.title": "捕鱼挑战",
  "challenge.instruction": "按顺序输入显示的数字，在 30 秒内完成。",
  "harvest.failure": "收网失败。",
  "net.placed": "渔网已放置。",
  "harvest.success": "收网成功。",
  "error.already-placed": "你已经放置了一个渔网。",
  "error.tile-occupied": "这里已经有渔网了。",
  "error.cannot-place": "这里不能放置渔网。",
  "error.not-your-net": "这不是你的渔网。",
  "error.cannot-cast": "这里不能撒网。",
  "error.tile-has-net": "这里已经有渔网了。",
  "cast.caught": "捕获了 {{count}} 条鱼！",
  "cast.no-fish": "没有捕到鱼。"
}
```

- [ ] **Step 3: 确认 JSON 格式正确**

```bash
python -c "import json; json.load(open('FishingNetMod/i18n/default.json')); json.load(open('FishingNetMod/i18n/zh.json')); print('OK')"
```

预期：`OK`

- [ ] **Step 4: 提交**

```bash
git add FishingNetMod/i18n/
git commit -m "feat: add i18n translation files for C# mod strings

Co-Authored-By: Claude <noreply@anthropic.com>"
```

---

### Task 3: PassiveNetManager — translation + createHarvestItem 注入

**Files:**
- Modify: `FishingNetMod/Mechanics/PassiveNetManager.cs`

- [ ] **Step 1: 添加 using 引用和字段**

在 `PassiveNetManager.cs` 顶部 `using` 区添加：

```csharp
using StardewModdingAPI;
```

在字段区（`private readonly FishingNetItemFactory itemFactory;` 之后）添加：

```csharp
private readonly ITranslationHelper? translation;
private readonly Func<string, int, Item> createHarvestItem;
```

- [ ] **Step 2: 替换构造函数**

将现有的无参构造函数和执行构造函数替换为：

```csharp
public PassiveNetManager()
    : this(new FishingNetItemFactory(), new VanillaFishProvider())
{
}

public PassiveNetManager(
    FishingNetItemFactory itemFactory,
    IFishProvider fishProvider,
    QuestProgressTracker? questProgressTracker = null,
    ITranslationHelper? translation = null,
    Func<string, int, Item>? createHarvestItem = null)
{
    this.itemFactory = itemFactory;
    this.fishProvider = fishProvider;
    this.questProgressTracker = questProgressTracker;
    this.translation = translation;
    this.createHarvestItem = createHarvestItem ?? ItemRegistry.Create;
}
```

- [ ] **Step 3: 添加 T 辅助方法**

在字段区之后、`Nets` 属性之前添加：

```csharp
private string T(string key, string fallback)
    => this.translation?.Get(key).ToString() ?? fallback;
```

- [ ] **Step 4: 替换 TryAdd 中的错误字符串**

`TryAdd` 方法内，将两处硬编码中文替换为 `T(...)` 调用：

```csharp
// 原: error = "你已经放置了一个渔网。";
error = T("error.already-placed", "你已经放置了一个渔网。");

// 原: error = "这里已经有渔网了。";
error = T("error.tile-occupied", "这里已经有渔网了。");
```

- [ ] **Step 5: 替换 TryPlace 中的错误字符串**

```csharp
// 原: error = "这里不能放置渔网。";
error = T("error.cannot-place", "这里不能放置渔网。");
```

- [ ] **Step 6: 替换 TryHarvest 中的错误字符串和 ItemRegistry.Create**

```csharp
// 原: error = "这不是你的渔网。";
error = T("error.not-your-net", "这不是你的渔网。");

// 原: Item item = ItemRegistry.Create(harvest.QualifiedItemId, harvest.Stack);
Item item = this.createHarvestItem(harvest.QualifiedItemId, harvest.Stack);
```

- [ ] **Step 7: 编译验证无回归**

```bash
dotnet build
```

预期：Build succeeded。

- [ ] **Step 8: 运行现有测试确认兼容**

```bash
dotnet test --filter "FullyQualifiedName~PassiveNetManagerTests"
```

预期：7 passed, 0 failed, 1 skipped。

- [ ] **Step 9: 提交**

```bash
git add FishingNetMod/Mechanics/PassiveNetManager.cs
git commit -m "feat: add i18n translation support and createHarvestItem injection to PassiveNetManager

Co-Authored-By: Claude <noreply@anthropic.com>"
```

---

### Task 4: ActiveFishingNet + ActiveFishingNetCast — translation 注入

**Files:**
- Modify: `FishingNetMod/Mechanics/ActiveFishingNet.cs`
- Modify: `FishingNetMod/Mechanics/ActiveFishingNetCast.cs`

- [ ] **Step 1: ActiveFishingNet — 添加 using 引用和字段**

`ActiveFishingNet.cs` 顶部添加：

```csharp
using StardewModdingAPI;
```

在字段区添加：

```csharp
private readonly ITranslationHelper? translation;
```

- [ ] **Step 2: ActiveFishingNet — 替换构造函数**

```csharp
public ActiveFishingNet(
    IMonitor monitor,
    FishingNetItemFactory itemFactory,
    IFishProvider fishProvider,
    QuestProgressTracker? questProgressTracker = null,
    ITranslationHelper? translation = null)
{
    this.monitor = monitor;
    this.itemFactory = itemFactory;
    this.fishProvider = fishProvider;
    this.questProgressTracker = questProgressTracker;
    this.translation = translation;
}
```

- [ ] **Step 3: ActiveFishingNet — 添加 T 辅助方法**

在字段区之后添加：

```csharp
private string T(string key, string fallback)
    => this.translation?.Get(key).ToString() ?? fallback;
```

- [ ] **Step 4: ActiveFishingNet — 替换 TryUse 中的错误字符串**

```csharp
// 原: Game1.showRedMessage("这里不能撒网。");
Game1.showRedMessage(T("error.cannot-cast", "这里不能撒网。"));

// 原: Game1.showRedMessage("这里已经有渔网了。");
Game1.showRedMessage(T("error.tile-has-net", "这里已经有渔网了。"));
```

- [ ] **Step 5: ActiveFishingNet — 修改 CompleteCatch 传递 translation**

`CompleteCatch` 方法中的 `cast.GetResultMessage()` 改为：

```csharp
Game1.addHUDMessage(new HUDMessage(cast.GetResultMessage(this.translation), HUDMessage.newQuest_type));
```

- [ ] **Step 6: ActiveFishingNetCast — 添加 using 引用并修改 GetResultMessage**

`ActiveFishingNetCast.cs` 顶部添加：

```csharp
using StardewModdingAPI;
```

将 `GetResultMessage()` 替换为：

```csharp
public string GetResultMessage(ITranslationHelper? translation = null)
{
    if (this.CaughtCount > 0)
    {
        string msg = translation?.Get("cast.caught")
            .Tokens(new { count = this.CaughtCount }).ToString()
            ?? $"捕获了 {this.CaughtCount} 条鱼！";
        return msg;
    }

    return translation?.Get("cast.no-fish").ToString() ?? "没有捕到鱼。";
}
```

- [ ] **Step 7: 编译验证**

```bash
dotnet build
```

预期：Build succeeded。

- [ ] **Step 8: 运行现有测试确认兼容**

```bash
dotnet test --filter "FullyQualifiedName~ActiveFishingNetCastTests"
```

预期：2 passed, 0 failed, 1 skipped（`EmptyCastUsesNoFishMessage` 不传参数走 fallback，`CastStoresNetDataCaughtItemsAndAttempts` 不调用 `GetResultMessage`）。

- [ ] **Step 9: 提交**

```bash
git add FishingNetMod/Mechanics/ActiveFishingNet.cs FishingNetMod/Mechanics/ActiveFishingNetCast.cs
git commit -m "feat: add i18n translation support to ActiveFishingNet and ActiveFishingNetCast

Co-Authored-By: Claude <noreply@anthropic.com>"
```

---

### Task 5: ModEntry — 传递 translation 并替换硬编码字符串

**Files:**
- Modify: `FishingNetMod/ModEntry.cs:26-27`（构造函数调用）
- Modify: `FishingNetMod/ModEntry.cs:98-145`（6 处硬编码字符串）

- [ ] **Step 1: 传递 translation 到子对象构造函数**

`ModEntry.cs:26-27`：

```csharp
// 原:
this.activeFishingNet = new ActiveFishingNet(this.Monitor, this.itemFactory, fishProvider, this.questProgressTracker);
this.passiveNetManager = new PassiveNetManager(this.itemFactory, fishProvider, this.questProgressTracker);

// 改为:
this.activeFishingNet = new ActiveFishingNet(this.Monitor, this.itemFactory, fishProvider, this.questProgressTracker, this.Helper.Translation);
this.passiveNetManager = new PassiveNetManager(this.itemFactory, fishProvider, this.questProgressTracker, this.Helper.Translation);
```

- [ ] **Step 2: 替换 OnButtonPressed 中的 5 处硬编码字符串**

`OnButtonPressed` 方法内：

```csharp
// 原: onFailure: () => Game1.showRedMessage("捕鱼失败。"),
onFailure: () => Game1.showRedMessage(this.Helper.Translation.Get("challenge.failure")),

// 原: title: "捕鱼挑战",
title: this.Helper.Translation.Get("challenge.title"),

// 原: instruction: "按顺序输入显示的数字，在 30 秒内完成。");
instruction: this.Helper.Translation.Get("challenge.instruction"));

// 原: onFailure: () => Game1.showRedMessage("收网失败。"),
onFailure: () => Game1.showRedMessage(this.Helper.Translation.Get("harvest.failure")),

// 原: Game1.addHUDMessage(new HUDMessage("渔网已放置。", HUDMessage.newQuest_type));
Game1.addHUDMessage(new HUDMessage(this.Helper.Translation.Get("net.placed"), HUDMessage.newQuest_type));
```

- [ ] **Step 3: 替换 CompletePassiveHarvest 中的硬编码字符串**

```csharp
// 原: Game1.addHUDMessage(new HUDMessage("收网成功。", HUDMessage.newQuest_type));
Game1.addHUDMessage(new HUDMessage(this.Helper.Translation.Get("harvest.success"), HUDMessage.newQuest_type));
```

- [ ] **Step 4: 编译 + 全量测试验证**

```bash
dotnet build && dotnet test
```

预期：Build succeeded，76 passed, 0 failed, 2 skipped。

- [ ] **Step 5: 提交**

```bash
git add FishingNetMod/ModEntry.cs
git commit -m "feat: wire i18n translations through ModEntry and replace hardcoded strings

Co-Authored-By: Claude <noreply@anthropic.com>"
```

---

### Task 6: 历史文档添加类名对照表

**Files:**
- Modify: `2026-05-20-fishing-net-implementation-design.md`

- [ ] **Step 1: 在文档末尾追加类名演化对照章节**

```markdown

---

## 类名演化对照（2026-06-13 追加）

本文档中的类名基于早期设计，当前实现已有变化：

| 早期设计名 | 当前实现名 | 文件位置 |
|-----------|-----------|----------|
| PassiveNetObject | PassiveNetData | Mechanics/PassiveNetData.cs |
| NetPlacementManager | PassiveNetManager | Mechanics/PassiveNetManager.cs |
| (无对应) | PassiveNetRenderer | Mechanics/PassiveNetRenderer.cs |
| FishingNetMod | ModEntry | ModEntry.cs |
| ChallengeMinigame | NetHarvestChallengeMenu | Menus/NetHarvestChallengeMenu.cs |
| FishingNetChallenge | NetHarvestChallenge | Mechanics/NetHarvestChallenge.cs |
```

- [ ] **Step 2: 提交**

```bash
git add 2026-05-20-fishing-net-implementation-design.md
git commit -m "docs: add class name mapping table to historical design document

Co-Authored-By: Claude <noreply@anthropic.com>"
```

---

### Task 7: 修复 TryHarvest_AllowsOwner 测试

**Files:**
- Modify: `FishingNetMod.Tests/Mechanics/PassiveNetManagerTests.cs:119-143`

- [ ] **Step 1: 重写测试，传入 createHarvestItem stub**

将 `PassiveNetManagerTests.cs` 中的 Skip 测试替换为：

```csharp
[Fact]
public void TryHarvest_AllowsOwner()
{
    // 用 FormatterServices 创建假的 Item，绕过 ItemRegistry.Create 对游戏数据的需求
    Func<string, int, Item> createItem = (id, stack) =>
        (Item)FormatterServices.GetUninitializedObject(typeof(SObject));

    var manager = new PassiveNetManager(
        new FishingNetItemFactory(),
        new VanillaFishProvider(),
        questProgressTracker: null,
        translation: null,
        createHarvestItem: createItem);

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

    // 验证 owner 通过了权限检查并进入了 harvest 逻辑
    Assert.True(result);
    Assert.Null(error);
    Assert.Empty(manager.Nets);
}
```

> `GiveOrDrop` 内的 `player.addItemToInventory(item)` 使用未初始化的 stub Item，行为不确定但不影响断言——核心验证的是"owner 通过了权限检查进入了 harvest 代码路径"。

- [ ] **Step 2: 运行测试**

```bash
dotnet test --filter "FullyQualifiedName~TryHarvest_AllowsOwner"
```

预期：PASS。

- [ ] **Step 3: 运行 PassiveNetManager 全量测试确认无回归**

```bash
dotnet test --filter "FullyQualifiedName~PassiveNetManagerTests"
```

预期：8 passed, 0 failed, 0 skipped。

- [ ] **Step 4: 提交**

```bash
git add FishingNetMod.Tests/Mechanics/PassiveNetManagerTests.cs
git commit -m "test: enable TryHarvest_AllowsOwner with createHarvestItem injection

Co-Authored-By: Claude <noreply@anthropic.com>"
```

---

### Task 8: 修复 TryUse_BlocksWhenPassiveNetAtTile 测试

**Files:**
- Modify: `FishingNetMod.Tests/Mechanics/ActiveFishingNetCastTests.cs:19-24`

- [ ] **Step 1: 添加测试辅助类 DummyMonitor**

在 `ActiveFishingNetCastTests` 类顶部（与测试方法同级）添加：

```csharp
private sealed class DummyMonitor : IMonitor
{
    public void Log(string message, LogLevel level = LogLevel.Trace) { }
    public void LogOnce(string message, LogLevel level = LogLevel.Trace) { }
    public bool IsVerbose => false;
    public string Name => "Test";
}
```

添加 `using StardewModdingAPI;`。

- [ ] **Step 2: 重写测试**

将 Skip 测试替换为：

```csharp
[Fact]
public void TryUse_BlocksWhenPassiveNetAtTile()
{
    // 1. 放置一个被动网在 (1, 0) —— 面朝上的 tile
    var passiveNetManager = new PassiveNetManager();
    passiveNetManager.TryAdd(new PassiveNetData(
        OwnerId: 9999L,
        LocationName: "Farm",
        Tile: new Vector2(1, 0),
        Level: NetLevel.Copper,
        Harvest: new List<PassiveNetHarvestData>()), out _);

    // 2. 构造 ActiveFishingNet
    var activeNet = new ActiveFishingNet(
        new DummyMonitor(),
        new FishingNetItemFactory(),
        new VanillaFishProvider());

    // 3. 构造手持铜网的 player，站在 (1, 1)，面朝上
    //    使用 FormatterServices 创建未初始化的 Farmer 和 SObject
    var player = (Farmer)FormatterServices.GetUninitializedObject(typeof(Farmer));
    SetUniqueMultiplayerID(player, 1111L);

    // 设置 CurrentItem: 需要是一个 SObject 含 modData "ChenJianCan.FishingNetMod/NetLevel" = "copper"
    var netItem = (SObject)FormatterServices.GetUninitializedObject(typeof(SObject));
    // SObject 的 modData 属性通过反射设置
    var modData = new ModDataDictionary();
    modData["ChenJianCan.FishingNetMod/NetLevel"] = "copper";
    typeof(Item).GetProperty("modData")?.SetValue(netItem, modData);
    typeof(Item).GetProperty("QualifiedItemId")?.SetValue(netItem, "(O)ChenJianCan.FishingNetMod_CopperNet");

    // 将 netItem 设置为 player 的 CurrentItem
    var toolField = typeof(Farmer).GetField("currentItem",
        BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
        ?? typeof(Farmer).GetField("<CurrentItem>k__BackingField",
        BindingFlags.Instance | BindingFlags.NonPublic);
    if (toolField is not null)
    {
        var netRef = (Netcode.NetRef<Item>)FormatterServices.GetUninitializedObject(
            typeof(Netcode.NetRef<Item>));
        typeof(Netcode.NetRef<Item>).GetProperty("Value")?.SetValue(netRef, netItem);
        toolField.SetValue(player, netRef);
    }

    // 设置 FacingDirection = 0 (up)
    var facingField = typeof(Farmer).GetField("facingDirection",
        BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
        ?? typeof(Farmer).GetField("<FacingDirection>k__BackingField",
        BindingFlags.Instance | BindingFlags.NonPublic);
    if (facingField is not null)
    {
        var netInt = (Netcode.NetInt)FormatterServices.GetUninitializedObject(typeof(Netcode.NetInt));
        typeof(Netcode.NetInt).GetProperty("Value")?.SetValue(netInt, 0);
        facingField.SetValue(player, netInt);
    }

    // 4. 构造名为 "Farm" 的 location，isWaterTile(1, 0) 需要返回 true
    var location = (GameLocation)FormatterServices.GetUninitializedObject(typeof(GameLocation));
    SetLocationName(location, "Farm");

    // 5. 验证冲突检查
    bool result = activeNet.TryUse(player, location, passiveNetManager, out var cast);

    Assert.True(result);
    Assert.Null(cast);
    Assert.Single(passiveNetManager.Nets); // 被动网未被移除
}
```

> 此测试使用与 `PassiveNetManagerTests` 相同的 `FormatterServices` + 反射桩模式。`TryGetNetData` → `isWaterTile` → 冲突检查路径不依赖游戏数据（`ItemRegistry`、`getFish` 等），因此在冲突检查处即返回。如果其中某步因未初始化字段导致 `NullReferenceException`，改为 `[Fact(Skip = "Formatterservices stub insufficient for full Farmer/Item state")]` 并在提交信息中注明。

- [ ] **Step 3: 运行测试**

```bash
dotnet test --filter "FullyQualifiedName~TryUse_BlocksWhenPassiveNetAtTile"
```

预期：PASS 或 SKIP（如 Skip，记录原因到提交信息）。

- [ ] **Step 4: 运行 ActiveFishingNetCastTests 全量确认**

```bash
dotnet test --filter "FullyQualifiedName~ActiveFishingNetCastTests"
```

预期：3 passed（如果 TryUse 测试 PASS）或 2 passed, 1 skipped。

- [ ] **Step 5: 提交**

```bash
git add FishingNetMod.Tests/Mechanics/ActiveFishingNetCastTests.cs
git commit -m "test: implement TryUse_BlocksWhenPassiveNetAtTile with reflection stubs

Co-Authored-By: Claude <noreply@anthropic.com>"
```

---

### Task 9: 全量测试与最终验证

- [ ] **Step 1: 运行全量测试**

```bash
dotnet test
```

预期：78 passed, 0 failed, 0 skipped（如果 Task 8 测试 PASS）或 77 passed, 0 failed, 1 skipped。

- [ ] **Step 2: 确认编译无 CS0618 警告**

```bash
dotnet build 2>&1 | grep -i "CS0618"
```

预期：空输出（无 CS0618 警告）。

- [ ] **Step 3: 确认发布包生成**

```bash
ls -la "FishingNetMod/bin/Debug/net6.0/FishingNetMod 0.1.0.zip"
```

预期：文件存在，且 zip 内现在包含 `FishingNetMod/i18n/default.json` 和 `FishingNetMod/i18n/zh.json`。

- [ ] **Step 4: 最终提交（如有未提交变更）**

```bash
git status
```

如果干净，跳过。如有残余变更：

```bash
git add -A
git commit -m "chore: finalize tech debt cleanup for 0.1.0

Co-Authored-By: Claude <noreply@anthropic.com>"
```

---

## 改动文件清单

| 文件 | 改动类型 | 预估行数 |
|------|----------|----------|
| `FishingNetMod/Mechanics/PassiveNetManager.cs` | 修改 | +12 |
| `FishingNetMod/Mechanics/ActiveFishingNet.cs` | 修改 | +10 |
| `FishingNetMod/Mechanics/ActiveFishingNetCast.cs` | 修改 | +8 |
| `FishingNetMod/ModEntry.cs` | 修改 | +8 |
| `FishingNetMod/i18n/default.json` | 新增 | +14 |
| `FishingNetMod/i18n/zh.json` | 新增 | +14 |
| `2026-05-20-fishing-net-implementation-design.md` | 修改 | +12 |
| `FishingNetMod.Tests/Mechanics/PassiveNetManagerTests.cs` | 修改 | +20 |
| `FishingNetMod.Tests/Mechanics/ActiveFishingNetCastTests.cs` | 修改 | +60 |
| **总计** | | **~158 行** |

---
