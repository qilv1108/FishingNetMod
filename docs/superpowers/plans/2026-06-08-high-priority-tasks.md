# 高优先级三件套 实现计划

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 完成完成度报告中的高优先级项——添加根 `.sln`、让被动渔网使用自定义贴图、第二级渔网改显示名为"银网"、产出游戏内手动测试清单。

**Architecture:** 在现有 SMAPI + Content Patcher 分离架构上做最小侵入改动。被动渲染器复用 CP 已注册的贴图资产，按 `NetLevel` 枚举值直接映射 SpriteIndex；银网仅改玩家可见文本，内部 ID/枚举/命令/材料全部保持 iron 不变。

**Tech Stack:** C# .NET 6, SMAPI 4.x, Content Patcher, xUnit, MonoGame/XNA (Rectangle/Vector2)

---

## 文件结构

| 文件 | 动作 | 职责 |
|---|---|---|
| `FishingNet.sln`（根） | 创建 | 聚合两个项目，使根目录可直接 `dotnet test` |
| `FishingNetMod/Mechanics/PassiveNetRenderer.cs` | 修改 | 改用自定义贴图，新增可测的 `GetSourceRect` 映射 |
| `FishingNetMod.Tests/Mechanics/PassiveNetRendererTests.cs` | 创建 | 验证 `NetLevel` → 源矩形映射 |
| `FishingNetMod/Data/NetLevelData.cs` | 修改 | 第二级 `DisplayName` → `Silver Fishing Net` |
| `FishingNetMod.Tests/Data/NetLevelDataTests.cs` | 修改 | 断言第二级显示名为 `Silver Fishing Net` |
| `FishingNetMod/[CP] FishingNetMod/assets/i18n/default.json` | 修改 | 银网名称/描述/邮件文案 |
| `docs/manual-test-checklist.md` | 创建 | 游戏内手动验证清单 |

**绝不触碰：** `QuestProgressTracker` 中的 `SilverFishCount`（那是银星品质鱼计数，与"银网"无关）；`NetLevel.Iron` 枚举、命令名 `iron`、物品 ID `_IronNet`、配方 `FishingNet_Iron` 及其铁锭材料。

---

## Task 1: 添加解决方案文件

**Files:**
- Create: `FishingNet.sln`

- [ ] **Step 1: 创建空解决方案**

在仓库根目录（`/e/claude code/games mod/Fishing net`）运行：

```bash
dotnet new sln -n FishingNet
```

Expected: 生成 `FishingNet.sln`，输出 `已成功创建模板"解决方案文件"`。

- [ ] **Step 2: 把两个项目加入解决方案**

```bash
dotnet sln FishingNet.sln add "FishingNetMod/FishingNetMod.csproj" "FishingNetMod.Tests/FishingNetMod.Tests.csproj"
```

Expected: 输出 `已将项目"FishingNetMod/FishingNetMod.csproj"添加到解决方案中。` 及测试项目同样提示。

- [ ] **Step 3: 从根目录运行测试验证 sln 可用**

```bash
dotnet test
```

Expected: 不再报 `MSB1003`；输出 `已通过! - 失败: 0，通过: 70，已跳过: 0，总计: 70`。

- [ ] **Step 4: 提交**

```bash
git add FishingNet.sln
git commit -m "build: add solution file so dotnet test runs from root"
```

---

## Task 2: 被动渔网使用自定义贴图

**Files:**
- Modify: `FishingNetMod/Mechanics/PassiveNetRenderer.cs`
- Test: `FishingNetMod.Tests/Mechanics/PassiveNetRendererTests.cs`

- [ ] **Step 1: 写失败测试**

创建 `FishingNetMod.Tests/Mechanics/PassiveNetRendererTests.cs`：

```csharp
using FishingNetMod.Data;
using FishingNetMod.Mechanics;
using Microsoft.Xna.Framework;
using Xunit;

namespace FishingNetMod.Tests.Mechanics;

public sealed class PassiveNetRendererTests
{
    [Theory]
    [InlineData(NetLevel.Copper, 0)]
    [InlineData(NetLevel.Iron, 16)]
    [InlineData(NetLevel.Gold, 32)]
    [InlineData(NetLevel.Iridium, 48)]
    public void GetSourceRectMapsLevelToSpriteColumn(NetLevel level, int expectedX)
    {
        Rectangle source = PassiveNetRenderer.GetSourceRect(level);

        Assert.Equal(new Rectangle(expectedX, 0, 16, 16), source);
    }
}
```

- [ ] **Step 2: 运行测试，确认编译失败**

```bash
dotnet test --filter "FullyQualifiedName~PassiveNetRendererTests"
```

Expected: 编译错误，`PassiveNetRenderer`不包含`GetSourceRect`的定义。

- [ ] **Step 3: 修改渲染器**

将 `FishingNetMod/Mechanics/PassiveNetRenderer.cs` 整体替换为：

```csharp
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using FishingNetMod.Data;
using StardewValley;

namespace FishingNetMod.Mechanics;

internal sealed class PassiveNetRenderer
{
    internal const string TextureAssetName = "Mods/ChenJianCan.FishingNetMod/FishingNet";

    internal static Rectangle GetSourceRect(NetLevel level)
        => new Rectangle((int)level * 16, 0, 16, 16);

    public void Draw(SpriteBatch spriteBatch, IEnumerable<PassiveNetData> nets)
    {
        Texture2D texture = Game1.content.Load<Texture2D>(TextureAssetName);

        foreach (PassiveNetData net in nets)
        {
            if (Game1.currentLocation?.Name != net.LocationName)
                continue;

            Vector2 position = Game1.GlobalToLocal(Game1.viewport, net.Tile * Game1.tileSize);
            Rectangle source = GetSourceRect(net.Level);
            spriteBatch.Draw(texture, position, source, Color.White, 0f, Vector2.Zero, Game1.pixelZoom, SpriteEffects.None, 1f);
        }
    }
}
```

- [ ] **Step 4: 运行测试，确认通过**

```bash
dotnet test --filter "FullyQualifiedName~PassiveNetRendererTests"
```

Expected: `通过: 4`。

- [ ] **Step 5: 跑全量测试确认无回归**

```bash
dotnet test
```

Expected: `失败: 0，通过: 74`（原 70 + 新增 4）。

- [ ] **Step 6: 提交**

```bash
git add "FishingNetMod/Mechanics/PassiveNetRenderer.cs" "FishingNetMod.Tests/Mechanics/PassiveNetRendererTests.cs"
git commit -m "fix: render passive nets with custom net texture per level"
```

---

## Task 3: 第二级渔网改显示名为"银网"

**Files:**
- Modify: `FishingNetMod.Tests/Data/NetLevelDataTests.cs:11`
- Modify: `FishingNetMod/Data/NetLevelData.cs:17`
- Modify: `FishingNetMod/[CP] FishingNetMod/assets/i18n/default.json`

- [ ] **Step 1: 改测试期望值（先让它失败）**

把 `FishingNetMod.Tests/Data/NetLevelDataTests.cs` 第 11 行：

```csharp
    [InlineData("iron", NetLevel.Iron, FishingNetIds.IronNetItemId, "Iron Fishing Net", 3, 4, 8)]
```

改为：

```csharp
    [InlineData("iron", NetLevel.Iron, FishingNetIds.IronNetItemId, "Silver Fishing Net", 3, 4, 8)]
```

- [ ] **Step 2: 运行测试，确认失败**

```bash
dotnet test --filter "FullyQualifiedName~NetLevelDataTests"
```

Expected: 失败，提示 `Expected: Silver Fishing Net  Actual: Iron Fishing Net`。

- [ ] **Step 3: 改 `NetLevelData.cs` 显示名**

把 `FishingNetMod/Data/NetLevelData.cs` 第 17 行：

```csharp
        new NetLevelData(NetLevel.Iron, "iron", FishingNetIds.IronNetItemId, "Iron Fishing Net", 3, 4, 8),
```

改为：

```csharp
        new NetLevelData(NetLevel.Iron, "iron", FishingNetIds.IronNetItemId, "Silver Fishing Net", 3, 4, 8),
```

- [ ] **Step 4: 改 CP i18n 文案**

把 `FishingNetMod/[CP] FishingNetMod/assets/i18n/default.json` 中这三行：

```json
  "item.iron.name": "Iron Fishing Net",
  "item.iron.description": "A sturdier fishing net that can catch more fish at once.",
```
```json
  "mail.iron": "Your copper net has proved itself. Try making an iron net; it should help you gather experience faster.   -Willy",
```

分别改为：

```json
  "item.iron.name": "Silver Fishing Net",
  "item.iron.description": "A sturdier silver fishing net that can catch more fish at once.",
```
```json
  "mail.iron": "Your copper net has proved itself. Try making a silver net; it should help you gather experience faster.   -Willy",
```

（其余键不变。）

- [ ] **Step 5: 运行全量测试，确认通过**

```bash
dotnet test
```

Expected: `失败: 0，通过: 74`。

- [ ] **Step 6: 提交**

```bash
git add "FishingNetMod/Data/NetLevelData.cs" "FishingNetMod.Tests/Data/NetLevelDataTests.cs" "FishingNetMod/[CP] FishingNetMod/assets/i18n/default.json"
git commit -m "feat: rename second-tier net to Silver in player-facing text"
```

---

## Task 4: 游戏内手动测试清单

**Files:**
- Create: `docs/manual-test-checklist.md`

- [ ] **Step 1: 创建清单文档**

创建 `docs/manual-test-checklist.md`，内容：

```markdown
# Fishing Net Mod 游戏内手动测试清单

> 用途：构建并安装 Mod 后，进游戏逐项打勾验证。

## 加载与命令
- [ ] Mod 正常加载，SMAPI 控制台无红色报错
- [ ] `fishing_net give copper` 给到铜网
- [ ] `fishing_net give iron` 给到第二级网（显示名应为 **Silver Fishing Net**）
- [ ] `fishing_net give gold` 给到金网
- [ ] `fishing_net give iridium` 给到铱星网
- [ ] 背包满时 `give` 把网掉落在脚下

## 主动撒网
- [ ] 面朝河流撒网可捕获
- [ ] 面朝湖泊撒网可捕获
- [ ] 面朝海洋撒网可捕获
- [ ] 体力消耗正确：铜 10 / 银(铁) 8 / 金 6 / 铱星 4
- [ ] 面朝非水域撒网：不消耗体力
- [ ] 背包满时捕获物掉落在脚下
- [ ] 数字挑战菜单正常弹出，成功才发放捕获物

## 被动放置
- [ ] 手持网面朝水域按交互键可放置，背包中网被移除
- [ ] 同一玩家无法放置第二个网
- [ ] 地图上渔网显示 **对应等级的自定义贴图**（不是原版纤维 771 图标）
- [ ] 睡一天后被动网产出鱼
- [ ] 面朝已放置渔网按交互键，收网挑战成功后鱼与网返还
- [ ] 收网时背包满则掉落在地

## 任务与解锁
- [ ] 钓鱼达到 1 级后收到威利铜网邮件，解锁铜网配方
- [ ] 铜网捕获 10 条银星鱼后解锁第二级（银网）配方
- [ ] 第二级网捕获 10 条金星及以上鱼后解锁金网配方
- [ ] 金网四季均有捕鱼记录后解锁铱星网配方

## 打包
- [ ] `FishingNetMod x.y.z.zip` 安装到全新存档后可正常使用
```

- [ ] **Step 2: 提交**

```bash
git add docs/manual-test-checklist.md
git commit -m "docs: add in-game manual test checklist"
```

---

## 自查结果

- **Spec 覆盖：** 任务 A→Task 1；B1→Task 2；B2→Task 3；C→Task 4。全部覆盖，无遗漏。
- **占位符扫描：** 无 TBD/TODO，每个代码步骤均含完整代码。
- **类型一致性：** `GetSourceRect(NetLevel)` 在测试与实现中签名一致；`TextureAssetName` 与 `content.json` 中注册的 `Mods/ChenJianCan.FishingNetMod/FishingNet` 一致；SpriteIndex 映射（0/16/32/48）与枚举顺序一致。
- **边界确认：** 已显式标注 `SilverFishCount` 与内部 iron 标识符不得改动，避免误伤。
