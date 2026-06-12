# 2026-06-11 发布前检查记录

## 执行范围

本记录覆盖 `0.1.0` 发布前四项关键任务：

1. 自动化测试与发布包结构检查。
2. 本机游戏内复测尝试。
3. 多人模式限制说明同步。
4. 仓库临时文件检查。

## 自动化测试结果

命令：

```bash
dotnet test
```

结果：`PASS`

摘要：

- 还原成功。
- 编译成功。
- 测试结果：74 passed, 0 failed, 0 skipped。
- 构建生成发布包：`FishingNetMod/bin/Debug/net6.0/FishingNetMod 0.1.0.zip`。

## 发布包结构检查

目标文件：

```text
FishingNetMod/bin/Debug/net6.0/FishingNetMod 0.1.0.zip
```

结果：`PASS`

必需文件核对：

- [x] `FishingNetMod/FishingNetMod/manifest.json`
- [x] `FishingNetMod/FishingNetMod/FishingNetMod.dll`
- [x] `FishingNetMod/[CP] FishingNetMod/manifest.json`
- [x] `FishingNetMod/[CP] FishingNetMod/content.json`
- [x] `FishingNetMod/[CP] FishingNetMod/assets/fishing_net.png`
- [x] `FishingNetMod/[CP] FishingNetMod/assets/i18n/default.json`

缺失文件：无。

## 本机游戏内复测

结果：`BLOCKED`

已完成的检查：

- 已确认 SMAPI 可执行文件存在：`E:\SteamLibrary\steamapps\common\Stardew Valley\StardewModdingAPI.exe`。
- 已尝试启动 SMAPI。
- 启动后可观察到 `StardewModdingAPI` 进程运行。
- 验证结束后已停止该进程。

未完成的检查：

- 当前 CLI 环境无法可靠驱动 Stardew Valley 的 GUI 窗口。
- 未能确认进入存档。
- 未能在存档内执行 `fishing_net give copper`。
- 未能验证主动撒网、被动放置或其他核心游戏内功能。

阻塞原因：当前环境可以启动 SMAPI 进程，但没有可用的窗口交互能力来进入存档并操作游戏。因此本轮不能把“SMAPI 进程运行”判定为“游戏内复测通过”。

后续动作：需要在可操作 GUI 的环境中人工进入存档，确认 SMAPI 控制台无红色报错，并至少验证 `fishing_net give copper` 或一个核心玩法动作后，才能把本项状态改为 `PASS`。

## 多人模式限制说明同步

结果：`PASS`

已更新：

- `README.md`：已说明 `0.1.0` 主要验证单人核心玩法，多人模式仍是实验性。
- `docs/release-notes/0.1.0.md`：已在 Known Limits 中明确多人模式未完整定义或验证的边界。

当前文档明确列出的多人未定义/未验证事项：

- 其他玩家是否允许收取别人放置的网。
- 被动渔网每日产出应计入哪个玩家的任务进度。
- 多人存档中配方解锁或进度是否应按玩家独立。

本轮未修改多人游戏逻辑；该项只同步发布前限制说明，避免把实验性多人模式误写成完整支持。

## 仓库临时文件检查

命令：

```bash
git status --short
```

结果：`PASS`

当前变更：

- `README.md`：本轮同步多人模式限制说明。
- `docs/release-notes/0.1.0.md`：本轮同步多人模式限制说明。
- `docs/reports/2026-06-11-pre-release-check.md`：本轮新增发布前检查记录。
- `docs/superpowers/plans/2026-06-11-pre-release-four-tasks.md`：本轮新增实施计划。

结论：未发现来源不明的图片、日志或临时文件；当前未提交变更均可解释为本轮工作产物。

## 最终发布前结论

总体结论：`BLOCKED`

| 检查项 | 状态 | 说明 |
|---|---|---|
| 自动化测试 | PASS | `dotnet test` 通过，74 passed, 0 failed, 0 skipped。 |
| 发布包结构 | PASS | `FishingNetMod 0.1.0.zip` 存在，必需 C# Mod 与 Content Patcher 文件均存在。 |
| 本机游戏内复测 | BLOCKED | SMAPI 可执行文件存在且进程可启动，但当前 CLI 无法驱动 GUI 进入存档并验证核心功能。 |
| 多人模式限制说明 | PASS | README 与 release notes 已明确多人模式实验性及未完整定义/验证的规则。 |
| 仓库临时文件检查 | PASS | 未发现来源不明临时文件；当前变更均为本轮工作产物。 |

发布判断：当前不能标记为“发布前全部通过”，因为游戏内复测未达到最低标准。需要在可操作 GUI 的环境中进入存档，并至少验证 `fishing_net give copper` 或一个核心玩法动作后，才能解除阻塞。
