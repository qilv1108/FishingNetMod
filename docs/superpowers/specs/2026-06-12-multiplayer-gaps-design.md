# 多人游戏完善 — 差距分析与设计

> 日期：2026-06-12  
> 版本：`0.1.0`  
> 方案：A（最小定点修复）  
> 决策：严格私有收网 / 进度按玩家独立 / 产出用网主人技能

---

## 一、现状

`0.1.0` 多人模式存在 4 个缺口：

| # | 缺口 | 位置 | 优先级 |
|---|------|------|--------|
| 1 | 收网无 OwnerId 权限检查 | `PassiveNetManager.TryHarvest` | 高 |
| 2 | 每日产出使用 `Game1.player` 而非网主人 | `PassiveNetManager.ProduceDaily` | 高 |
| 3 | 任务进度存档 key 不隔离玩家 | `QuestProgressTracker` | 高 |
| 4 | 主动撒网不检查位置是否已有被动网 | `ActiveFishingNet.TryUse` | 中 |

已正确实现的规则：每人限 1 网（OwnerId 去重）、每格限 1 网（Tile 去重）、必须水域（isWaterTile）。

---

## 二、架构总览

涉及 4 个文件，不引入新文件：

```
FishingNetMod/
├── ModEntry.cs                    # 事件中传递 playerId / passiveNetManager
├── Mechanics/
│   ├── PassiveNetManager.cs       # TryHarvest 权限、ProduceDaily 归属
│   └── ActiveFishingNet.cs        # TryUse 位置冲突检查
└── Quests/
    └── QuestProgressTracker.cs    # 存档 key 按 playerId 隔离
```

**数据流：**

```
按钮按下 → ModEntry → player.UniqueMultiplayerID
  ├─ 主动撒网 → TryUse(player, location, passiveNetManager)
  │   └─ 检查目标 tile 是否已有被动网 → 冲突则红字提示
  │
  ├─ 被动放置 → TryPlace(player, ...)
  │   └─ OwnerId = player.UniqueMultiplayerID（不变）
  │
  └─ 收取渔网 → TryHarvest(player, ...)
      ├─ data.OwnerId != player.UniqueMultiplayerID → 拒绝："这不是你的渔网。"
      └─ 通过后正常收网

每日结算 → OnDayStarted
  └─ ProduceDaily(location)
      └─ 按 net.OwnerId 找 Farmer，用他的技能算鱼品质
      └─ RecordNetCatch(net.OwnerId, ...)

存档/读档
  ├─ QuestProgressTracker.Save(helper, playerId)  → key = "QuestProgress_{playerId}"
  └─ QuestProgressTracker.Load(helper, playerId)  → 按玩家隔离
```

---

## 三、详细改动

### 3.1 收网权限 — `PassiveNetManager.TryHarvest`

在 `TryGetHarvestableNet` 成功后、给予物品前增加：

```csharp
if (data.OwnerId != player.UniqueMultiplayerID)
{
    error = "这不是你的渔网。";
    return false;
}
```

约 +3 行。ModEntry 中已有 error 非 null 时弹红字的处理，无需额外改动。

### 3.2 每日产出归属 — `PassiveNetManager.ProduceDaily`

```csharp
// 原代码：
Item? fish = this.fishProvider.GetFish(location, Game1.player, net.Tile);

// 改为：
Farmer? owner = Game1.getFarmer(net.OwnerId);
if (owner is null)
    continue;
Item? fish = this.fishProvider.GetFish(location, owner, net.Tile);
```

```csharp
// 原代码：
this.questProgressTracker?.RecordNetCatch(net.Level, harvest.Quality, Game1.currentSeason);

// 改为：
this.questProgressTracker?.RecordNetCatch(net.OwnerId, net.Level, harvest.Quality, Game1.currentSeason);
```

约 +5 行。

### 3.3 任务进度隔离 — `QuestProgressTracker`

存档 key 按玩家 ID 隔离：

```csharp
private static string SaveKey(long playerId) => $"QuestProgress_{playerId}";

public void Load(IModHelper helper, long playerId)
{
    this.Progress = Normalize(helper.Data.ReadSaveData<QuestProgress>(SaveKey(playerId)));
}

public void Save(IModHelper helper, long playerId)
{
    helper.Data.WriteSaveData(SaveKey(playerId), Normalize(this.Progress));
}

public void RecordNetCatch(long playerId, NetLevel netLevel, int quality, string season)
{
    // 内部逻辑不变，仅从参数接收 playerId
}
```

约 +15 行。内部追踪逻辑不变，`Version` 和 `Normalize` 逻辑不变。

`RecordPassiveCatch` 是 `RecordNetCatch` 的简单委托，同步更新签名即可。

`EvaluateUnlocks` 和 `EvaluateCopperUnlocks` 签名不变，调用方在传入 `QuestPlayerSnapshot` 前已经确定了玩家。

### 3.4 主动撒网位置冲突 — `ActiveFishingNet.TryUse`

签名增加可选参数，在 isWaterTile 检查后、撒网前新增：

```csharp
public bool TryUse(Farmer player, GameLocation location, 
    PassiveNetManager? passiveNetManager, out ActiveFishingNetCast? cast)
{
    // ... isWaterTile 检查 ...
    if (passiveNetManager?.TryGetHarvestableNet(location.Name, targetTile, out _) == true)
    {
        Game1.showRedMessage("这里已经有渔网了。");
        cast = null;
        return true;
    }
    // ... 原有撒网逻辑 ...
}
```

约 +6 行。

此外 `ActiveFishingNet.CompleteCatch`（:68）中也有 `RecordNetCatch` 调用，需同步传入 `player.UniqueMultiplayerID`：

```csharp
// 原代码：
this.questProgressTracker?.RecordNetCatch(cast.NetData.Level, caught.Quality, Game1.currentSeason);

// 改为：
this.questProgressTracker?.RecordNetCatch(player.UniqueMultiplayerID, cast.NetData.Level, caught.Quality, Game1.currentSeason);
```

### 3.5 ModEntry 连带改动

- `OnSaveLoaded` / `OnSaving` → `QuestProgressTracker.Load/Save` 传入 `Game1.player.UniqueMultiplayerID`
- `OnDayEnding` → `EvaluateUnlocks` 按主玩家 ID 评估（`EvaluateUnlocks` 幂等，多人各自触发无副作用）
- `OnButtonPressed` → `TryUse` 调用传入 `this.passiveNetManager`
- `CompleteActiveFishing` → `CompleteCatch` 的调用者已经是 `ModEntry`，`player` 即 `Game1.player`，传入 `player.UniqueMultiplayerID`

---

## 四、异常处理

| 场景 | 处理 |
|------|------|
| Owner 离线 | `Game1.getFarmer(id)` 返回 null → `continue` 跳过该网 |
| Owner 是已删除角色 | 同上 |
| 离线玩家重连 | `QuestProgress_{id}` 保留在存档中，自动恢复 |
| 背包满收网 | 物品掉落在脚下（`GiveOrDrop` 已有处理） |
| 自己网和自己撒网冲突 | 冲突检查触发，提示"这里已经有渔网了" |
| 多人同时操作 | SMAPI 串行帧处理，无并发问题 |
| 旧存档 | `QuestProgress` 旧 key 不再读取，旧进度丢失但不崩溃 |
| 配方重复解锁 | `craftingRecipes[key] = 0` 幂等 |

---

## 五、测试策略

### 现有测试

74 个测试零改动。`TryHarvest`/`TryUse` 的现有测试不受新增条件路径影响，编译兼容。

### 新增测试（4 个，78 总）

| 测试 | 文件 | 验证 |
|------|------|------|
| `TryHarvest_RejectsNonOwner` | `PassiveNetManagerTests.cs` | 非 owner 收网返回 false，error 非 null |
| `TryHarvest_AllowsOwner` | `PassiveNetManagerTests.cs` | owner 收网返回 true |
| `TryUse_BlocksWhenPassiveNetAtTile` | `ActiveFishingNetCastTests.cs` | 目标 tile 有被动网时 cast=null |
| `RecordNetCatch_IsolatedByPlayerId` | `QuestProgressTrackerTests.cs` | 不同 playerId 累计各自的计数 |
