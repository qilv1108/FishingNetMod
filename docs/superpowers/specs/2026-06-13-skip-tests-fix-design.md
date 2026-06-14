# 2 个 Skip 测试修复 — 设计文档

> 日期：2026-06-13  
> 方案：A — 最小生产代码注入

---

## 一、问题

| 测试 | Skip 原因 | 阻塞点 |
|------|----------|--------|
| `TryHarvest_AllowsOwner` | `item.Quality = harvest.Quality` 需初始化 `netQuality` NetField | `TryHarvest:97` |
| `TryUse_BlocksWhenPassiveNetAtTile` | `Farmer.get_CurrentItem()` / `isWaterTile` 需完整游戏对象 | `TryUse → TryGetNetData` |

两个测试的目标验证点都在代码路径中间，被前面的游戏初始化步骤阻塞。

---

## 二、方案

给 `PassiveNetManager` 和 `ActiveFishingNet` 各加可选委托参数，测试时注入 stub 跳过游戏依赖步骤。

### 2.1 TryHarvest — 注入 `deliverHarvest`

```csharp
// PassiveNetManager 新增字段
private readonly Action<Farmer, GameLocation, PassiveNetData>? deliverHarvest;

// 构造函数新增可选参数
Action<Farmer, GameLocation, PassiveNetData>? deliverHarvest = null

// TryHarvest 中：
if (this.deliverHarvest != null)
    this.deliverHarvest(player, location, data);
else
{
    // 原有 foreach + GiveOrDrop 逻辑
}
this.nets.Remove(data);
```

测试传入 `(p, l, d) => {}`，跳过物品创建和交付。

### 2.2 TryUse — 注入 `getHeldNet` + `isWaterTile`

```csharp
// ActiveFishingNet 新增字段
private readonly Func<Farmer, NetLevelData?>? getHeldNet;
private readonly Func<GameLocation, int, int, bool>? isWaterTileFunc;

// TryUse 中，手持网识别和水域判断优先用注入委托
```

测试传入固定返回值，跳过 `player.CurrentItem` 和 `isWaterTile` 调用。

---

## 三、测试策略

- 全部新参数可选默认 null，现有 76 个测试零改动
- 两个 Skip 测试移除 Skip 标记，传入 stub 委托
- 预期：78 passed, 0 failed, 0 skipped

---

## 四、改动文件

| 文件 | 改动 |
|------|------|
| `PassiveNetManager.cs` | +8 行 |
| `ActiveFishingNet.cs` | +14 行 |
| `PassiveNetManagerTests.cs` | 移除 Skip，+3 行 |
| `ActiveFishingNetCastTests.cs` | 移除 Skip，-50 行反射桩 |
| **净行数** | **-25 行** |
