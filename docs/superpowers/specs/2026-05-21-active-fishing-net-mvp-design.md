# FishingNetMod 主动撒网 MVP 设计

## 目标

实现第一阶段可玩的主动撒网闭环：玩家通过 SMAPI 控制台命令获得临时捕鱼网，手持捕鱼网面对水域按工具使用键时捕获物品并扣除体力。

本阶段只验证核心玩法，不实现正式贴图、制作配方、威利任务链、Content Patcher 内容包、被动放置和多人限制。

## 范围

### 包含

- 四级捕鱼网的等级数据：铜、铁、金、铱星。
- 调试命令：`fishing_net give copper|iron|gold|iridium`。
- 临时捕鱼网物品创建与识别。
- 主动撒网输入处理。
- 前方水域检测。
- 成功撒网后的随机捕获、背包添加或掉落、体力扣除、HUD 提示。
- 非水域使用时提示失败且不扣体力。

### 不包含

- 正式自定义物品数据、贴图和配方。
- Content Patcher 包。
- 被动放置、每日产出和收取。
- 任务进度、邮件、威利对话。
- 多人游戏限制。
- 完全复刻原版钓鱼公式。

## 项目结构

```text
FishingNetMod/
├── Data/
│   └── NetLevelData.cs
├── Items/
│   └── FishingNetItemFactory.cs
├── Mechanics/
│   └── ActiveFishingNet.cs
├── FishingNetMod.csproj
├── manifest.json
└── ModEntry.cs
```

## 组件设计

### Data/NetLevelData.cs

定义 `NetLevel` 枚举：

- `Copper`
- `Iron`
- `Gold`
- `Iridium`

定义 `NetLevelData`，包含：

- 等级；
- 显示名称；
- 最小捕获数；
- 最大捕获数；
- 体力消耗。

等级数值：

| 等级 | 捕获数量 | 体力消耗 |
|------|----------|----------|
| 铜 | 2-3 | 10 |
| 铁 | 3-4 | 8 |
| 金 | 4-5 | 6 |
| 铱星 | 5-7 | 4 |

### Items/FishingNetItemFactory.cs

负责临时捕鱼网物品的创建与识别。

- 创建物品时使用一个原版物品作为临时载体。
- 通过 `Item.modData["ChenJianCan.FishingNetMod/NetLevel"]` 写入等级。
- 识别当前手持物时读取该 `modData`。
- 无标记或等级无效的物品不被视为捕鱼网。

### Mechanics/ActiveFishingNet.cs

负责主动撒网行为。

流程：

1. 从玩家手持物读取捕鱼网等级。
2. 根据玩家朝向计算前方一格 tile。
3. 调用当前地图水域检测。
4. 如果前方不是水域：显示“这里不能撒网。”，不扣体力。
5. 如果前方是水域：按等级随机捕获数量。
6. 从 MVP 捕获池生成物品。
7. 捕获物优先加入背包；背包满时掉落在玩家脚下。
8. 扣除等级对应体力。
9. 显示捕获数量提示。

MVP 捕获池使用稳定的基础物品池，包含常见鱼类和垃圾。后续功能阶段再替换为 `GameLocation.getFish(...)`。

### ModEntry.cs

负责 SMAPI 接线：

- 初始化 `FishingNetItemFactory` 和 `ActiveFishingNet`。
- 注册命令 `fishing_net`：
  - `fishing_net give copper`
  - `fishing_net give iron`
  - `fishing_net give gold`
  - `fishing_net give iridium`
- 注册 `ButtonPressed` 事件：
  - 仅在玩家已加载、手持捕鱼网、按下工具使用键时触发主动撒网。

## 错误处理

- 无效命令参数：输出合法用法和等级列表。
- 背包满：捕获物掉落在玩家脚下。
- 非水域：提示失败，不扣体力。
- 非捕鱼网物品：按钮事件不处理。

## 验证方式

1. 运行 `dotnet build "FishingNetMod/FishingNetMod.csproj"`，构建应成功。
2. 启动 SMAPI。
3. 在 SMAPI 控制台执行 `fishing_net give copper`，玩家应获得铜捕鱼网。
4. 手持铜捕鱼网面对非水域按工具键，应显示失败提示且不扣体力。
5. 手持铜捕鱼网面对水域按工具键，应获得 2-3 个捕获物并减少 10 体力。
6. 执行 `fishing_net give diamond`，应输出合法等级提示。
