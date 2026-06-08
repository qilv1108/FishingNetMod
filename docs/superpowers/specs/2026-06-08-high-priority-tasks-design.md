# 高优先级三件套设计文档

**日期：** 2026-06-08
**目标：** 完成完成度报告中标记的高优先级项，按 `.sln` → 贴图+银网改名 → 测试清单 顺序推进。
**范围外：** 整套中文 i18n（`zh.json`）、收网交互改造、10% 垃圾概率、多人模式规则——均属其他优先级，不在本次范围。

---

## 任务 A：添加解决方案文件

在仓库根目录新建 `FishingNet.sln`，引用以下两个项目：

- `FishingNetMod/FishingNetMod.csproj`
- `FishingNetMod.Tests/FishingNetMod.Tests.csproj`

**验收标准：** 在根目录直接执行 `dotnet test` 能跑通全部测试（当前 70 个），无需再指定 `.csproj` 路径。

**风险：** 极低，纯工程文件，不触碰任何游戏逻辑。

---

## 任务 B：被动渔网使用自定义贴图 + 第二级改名"银网"

### B1 — 修复被动渔网渲染器（核心）

**现状：** `FishingNetMod/Mechanics/PassiveNetRenderer.cs:17` 写死原版 `Game1.objectSpriteSheet, 771`，与 CP 已注册的自定义贴图不一致。

**已知事实：**

- CP 资源 `assets/fishing_net.png` 为 64×16，即 4 个 16×16 精灵。
- CP `content.json` 已将其注册为资产 `Mods/ChenJianCan.FishingNetMod/FishingNet`，物品按 `SpriteIndex` 0/1/2/3 映射铜/铁/金/铱星。
- 枚举 `NetLevel`（`Copper=0, Iron=1, Gold=2, Iridium=3`）顺序与 SpriteIndex 完全一致，可直接 `(int)level` 取索引。
- `PassiveNetData` 含 `NetLevel Level` 字段。

**实现方案（采用方案 1）：**

渲染器用 `Game1.content.Load<Texture2D>("Mods/ChenJianCan.FishingNetMod/FishingNet")` 加载并缓存自定义贴图；按 `net.Level` 计算源矩形 `new Rectangle((int)net.Level * 16, 0, 16, 16)` 绘制对应等级图标，替换原 771 逻辑。

**被否决的方案：**

- 方案 2：构造真实物品实例调用 `drawInMenu` —— 更重、更绕，无收益。
- 方案 3：维持 771 仅换原版图标 —— 治标不治本。

### B2 — 第二级改显示名为"银网"（仅显示层，内部保持 iron）

**改动（玩家可见文本）：**

- CP `i18n/default.json`：
  - `item.iron.name` → `Silver Fishing Net`
  - `item.iron.description` → 改为"银网"措辞
  - `mail.iron` → 改为"银网"措辞
- C# `Data/NetLevelData.cs`：`NetLevel.Iron` 行的 `DisplayName` → `Silver Fishing Net`（用于 HUD 与命令反馈）。

**保持不变（玩家不可见）：**

- 枚举 `NetLevel.Iron`
- 命令名 `iron`（`fishing_net give iron`）
- 物品 ID `ChenJianCan.FishingNetMod_IronNet`
- 配方 ID `FishingNet_Iron` 及其材料（铁锭 335）

**说明：** CP 文本当前为英文，因此银网名采用英文 `Silver Fishing Net`，与现有文件语言一致。

### B3 — 测试

- 新增 `PassiveNetRendererTests`：验证 `NetLevel` → SpriteIndex 源矩形映射正确（0/1/2/3）。
- 调整 `NetLevelDataTests`：第二级 `DisplayName` 断言改为 `Silver Fishing Net`。
- 全量测试在改动后仍应全绿。

---

## 任务 C：游戏内手动测试清单

在 `docs/` 下生成一份可勾选的 Markdown 清单（`docs/manual-test-checklist.md`），覆盖：

1. Mod 正常加载，无 SMAPI 报错
2. `fishing_net give copper|iron|gold|iridium` 能给到四级网
3. 主动撒网：河流 / 湖泊 / 海洋分别可捕获
4. 主动撒网体力消耗正确（铜10/铁8/金6/铱星4）
5. 非水域撒网无体力消耗
6. 被动放置渔网成功，背包移除网
7. 睡一天后被动网产出鱼
8. 收网成功，鱼与网返还（背包满则掉落）
9. 邮件触发与配方解锁链（铜→铁→金→铱星）
10. 背包满时捕获物掉落在脚下
11. 第二级物品显示名为"Silver Fishing Net"
12. 被动渔网在地图上显示对应等级的自定义贴图（非原版纤维图标）

用途：玩家照此进游戏逐项打勾验证。

---

## 整体执行顺序与验收

1. 任务 A：建 `.sln` → 根目录 `dotnet test` 通过。
2. 任务 B：改渲染器 + 银网显示名 + 测试 → 测试仍全绿。
3. 任务 C：写测试清单文档。

每一步完成后运行测试，确保测试数不减、全部通过。
