# 星露谷物语 - 捕鱼网 Mod 实现设计文档

> **文档状态：历史设计。** 本文记录 2026-05-20 的早期实现设计，部分结构名、解锁条件和收网流程已经与当前 `0.1.0` 实现不同。当前发布状态以 `docs/release-notes/0.1.0.md`、`docs/reports/2026-06-08-0.1.0-manual-test-results.md` 和当前代码为准。

**Goal:** 基于现有功能设计，采用 SMAPI + Content Patcher 分离式架构，实现组合型捕鱼网工具（主动撒网 + 被动放置），包含铜/铁/金/铱星四个等级，通过威利任务链解锁。

**Architecture:** C# Mod 负责核心游戏机制与进度追踪，Content Patcher 包负责对话、邮件、配方数据等内容配置。

**Tech Stack:** SMAPI 4.x, Stardew Valley 1.6, C# .NET 6, Content Patcher 2.x

---

## 项目结构

```
FishingNetMod/
├── [C# Mod] FishingNetMod/
│   ├── FishingNetMod.csproj
│   ├── manifest.json
│   ├── ModEntry.cs                    # SMAPI 入口，注册事件和配置
│   ├── Items/
│   │   └── FishingNet.cs              # 自定义工具类（4 个等级）
│   ├── Mechanics/
│   │   ├── ActiveFishingNet.cs        # 主动撒网：检测水域 → 计算捕获 → 消耗体力
│   │   ├── PassiveNetObject.cs        # 被动放置：地图上的放置物，日产出
│   │   └── NetPlacementManager.cs     # 地图上所有网的统一管理（按玩家 ID）
│   ├── Quests/
│   │   └── QuestProgressTracker.cs    # 任务进度追踪与存档
│   └── Data/
│       └── NetLevelData.cs            # 等级数值定义（捕获数、体力消耗、材料）
│
└── [CP Pack] [CP] FishingNetMod/
    ├── manifest.json
    ├── content.json                     # CP 主配置（EditData 注入）
    └── assets/
        ├── i18n/
        │   └── default.json             # 对话、邮件、提示文本
        └── fishing_net.png              # 工具贴图（16x64，每级 16x16）
```

---

## 核心机制

### 主动撒网

**触发条件：**
1. 玩家手持捕鱼网（任意等级）
2. 按下工具使用键（鼠标左键 / 控制器键）
3. 玩家面朝水域（前方 1 格是水 tile）

**执行流程：**
1. 获取当前地图、玩家位置、朝向
2. 检查前方格子 `GameLocation.isWaterTile(x, y)`
3. **非水域** → 播放挥空动画，不消耗体力，直接返回
4. **是水域** → 调用 `GameLocation.getFish(...)` 获取可捕获列表
5. 根据捕鱼网等级，随机选取 N 条鱼：
   - 铜网：2-3 条
   - 铁网：3-4 条
   - 金网：4-5 条
   - 铱星网：5-7 条
6. 每条鱼根据钓鱼技能计算品质（普通 / 银 / 金 / 铱星）
7. 将鱼添加到玩家背包（背包满则掉落在地 `debris`）
8. 扣除对应体力：铜 10 / 铁 8 / 金 6 / 铱星 4
9. 播放撒网动画 + 水花音效
10. 显示捕获结果提示（"捕获了 3 条鱼！" + 列表）

**垃圾机制：**
- 当 `getFish` 返回垃圾时，直接给予垃圾物品
- 概率完全遵循原版钓鱼公式（基于 `Farmer.fishingSkill` 和位置）

**异常处理：**
| 场景 | 行为 |
|------|------|
| 前方不是水域 | 播放挥空动画，不消耗体力 |
| 背包满 | 鱼掉落在玩家脚下 |
| 多人游戏中另一玩家已放置网 | 显示提示"你已经有网在水中了" |

### 被动放置

**放置流程：**
1. 玩家手持捕鱼网，在水边按"放置"键（右键 / 控制器对应键）
2. `NetPlacementManager` 检查该玩家是否已有放置的网
3. 检测前方格子是否为水域
4. 如果是，在当前位置创建一个 `PassiveNetObject`
5. 捕鱼网从背包中移除（网被放在水里）
6. 地图上显示网的可视化贴图

**每日产出：**
- 在 `DayStarted` 事件中，`NetPlacementManager` 遍历所有 `PassiveNetObject`
- 每个网调用 `getFish` 获取可捕获列表，随机选取 1-2 条（铱星网固定 2 条）
- 10% 概率出垃圾代替鱼
- 将鱼存储在网的内部 `Chest` 库存中

**收取流程：**
1. 玩家靠近放置的网，按交互键
2. 弹出收获界面（类似宝箱），显示里面的鱼
3. 玩家点击取走所有鱼
4. 网回到玩家背包（有空间时），否则掉落在地

**关键规则：**
- 每人同时只能放置 1 个网（通过 ` farmer.UniqueMultiplayerID` 追踪）
- 网在地图上不会消失（除非被收取）
- 放置网不消耗体力

---

## 任务系统（C# + CP 协作）

### C# 进度追踪

`QuestProgressTracker` 保存到 SMAPI 存档数据：

```csharp
class QuestProgress
{
    public int SilverFishCount;              // 铜网捕获银星鱼计数
    public int GoldFishCount;                // 铁网捕获金星/铱星鱼计数
    public HashSet<string> SeasonsFished;    // 金网捕鱼记录的季节
    public string Version;                   // 数据版本号，用于迁移
}
```

**C# 何时更新进度：**
- `DayEnding` 事件：检查当天捕获，更新计数器
- 捕获符合条件品质的鱼时：实时增加对应计数

**C# 何时解锁配方：**
- 当计数达标或条件满足时，调用 `Game1.player.craftingRecipes.Add("FishingNet_Iron", 0)` 动态添加配方

### CP 内容配置

CP 通过 `content.json` 负责：

1. **威利对话**：`EditData` 注入 `Characters/Dialogue/Willy` 的自定义对话节点
   - 钓鱼 2 级后进入鱼店触发任务 1 对话
   - 后续任务通过邮件触发

2. **邮件**：`EditData` 注入 `Data/Mail` 的自定义邮件
   - 任务 2/3/4 的触发通过邮件通知玩家
   - 邮件内容包含任务描述和需求

3. **配方数据**：`EditData` 注入 `Data/CraftingRecipes`
   - 配方数据本身（材料需求、产出物品）通过 CP 预定义

4. **物品信息**：`EditData` 注入 `Data/ObjectInformation`

### 任务流程

| 任务 | CP 负责 | C# 负责 |
|------|---------|---------|
| 1. 威利的请求（解锁铜网） | 对话文本、配方数据注入 | 检测钓鱼 2 级、接收材料后解锁铜网配方 |
| 2. 初试锋芒（解锁铁网） | 邮件文本、配方数据 | 追踪铜网银星鱼计数、达标后解锁铁网配方 |
| 3. 品质追求（解锁金网） | 邮件文本、配方数据 | 追踪铁网金星/铱星鱼计数 |
| 4. 大师之路（解锁铱星网） | 邮件文本、配方数据 | 追踪金网四季捕鱼记录 |

---

## 数值与制作配方

| 等级 | 主动捕获 | 体力消耗 | 制作材料 | 被动日产出 |
|------|----------|----------|----------|------------|
| 铜网 | 2-3 条 | 10 | 铜锭 x5 + 木材 x50 + 纤维 x20 | 1 条 |
| 铁网 | 3-4 条 | 8 | 铁锭 x5 + 木材 x40 + 纤维 x15 + 铜网 | 1-2 条 |
| 金网 | 4-5 条 | 6 | 金锭 x5 + 木材 x30 + 纤维 x10 + 铁网 | 1-2 条 |
| 铱星网 | 5-7 条 | 4 | 铱锭 x5 + 木材 x20 + 纤维 x5 + 金网 | 2 条 |

**说明：**
- 升级时需要上交旧网 + 新材料（旧网被消耗）
- 被动放置的 10% 垃圾概率所有等级相同
- 主动撒网的垃圾概率 = 原版钓鱼垃圾概率（基于技能和位置）

---

## 技术实现要点

### SMAPI 关键事件

| 事件 | 用途 |
|------|------|
| `GameLaunched` | 检测 Content Patcher 是否安装，显示警告 |
| `ButtonPressed` | 检测捕鱼网使用（主动撒网 / 被动放置） |
| `DayStarted` | 被动网每日产出结算 |
| `DayEnding` | 检查任务进度（银星鱼计数、四季记录等） |
| `Saving` / `Saved` | 保存被动网的位置和库存状态 |

### 原版 API 复用

- `GameLocation.getFish(...)` — 获取可捕获鱼类
- `GameLocation.isWaterTile(x, y)` — 检测水域
- `Farmer.fishingSkill` — 获取钓鱼技能等级，影响鱼品质和垃圾概率
- `Game1.player.craftingRecipes.Add(...)` — 动态解锁配方

### 存档兼容性

- 被动网的状态用 SMAPI 的 `helper.Data.WriteSaveData` 保存，key 包含玩家 `UniqueMultiplayerID`
- 任务进度也写入存档数据
- **版本迁移**：`QuestProgress` 包含 `Version` 字段，加载时自动迁移旧数据结构
- 卸载 mod 后，存档中残留数据无害；重新安装后数据自动恢复

---

## 测试要点

1. **主动撒网**：站在各种水域测试（河流、湖泊、海洋），确认捕获数量和体力消耗正确
2. **被动放置**：放置后睡一天，确认产出正确；收取后确认网回到背包
3. **任务追踪**：捕获银星鱼后确认计数器增加；四季记录正确
4. **边界情况**：背包满时鱼掉落在地；没有鱼时出垃圾；不能在没有水的地方使用
5. **多人游戏**：每个玩家有自己的网和任务进度（通过 `UniqueMultiplayerID` 隔离）
6. **卸载重装**：放置网后保存 → 卸载 mod → 读档 → 重装 mod，验证数据恢复
