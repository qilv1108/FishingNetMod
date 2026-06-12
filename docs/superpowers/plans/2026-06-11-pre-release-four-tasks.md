# 发布前四个关键任务 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 完成发布前四项关键检查，并把结果写成可追溯的发布前验证记录，同时把多人模式限制说明同步到当前权威文档。

**Architecture:** 这次只做验证与文档同步，不改游戏逻辑。执行顺序固定为：先跑自动化测试并生成 zip，再检查 zip 内容，然后尝试本机启动游戏并进入存档验证核心功能，最后补齐多人模式限制说明并检查仓库状态。所有结果统一写入一份新的发布前验证报告，避免信息分散。

**Tech Stack:** .NET 6、SMAPI、Content Patcher、xUnit、Git、ZIP 归档检查、Markdown 文档。

---

### Task 1: 运行自动化测试并核对发布包结构

**Files:**
- Modify: `FishingNetMod/bin/Debug/net6.0/FishingNetMod 0.1.0.zip`（由构建生成，不手改）
- Modify: `docs/reports/2026-06-11-pre-release-check.md`

- [ ] **Step 1: 先确认测试和 zip 目标文件路径**

```bash
git status --short
```

确认当前没有本轮未解释的临时文件，后续只把本轮生成的报告和构建产物当作正常变更。

- [ ] **Step 2: 运行全量自动化测试并生成 zip**

```bash
dotnet test
```

预期：
- 测试通过，当前仓库基线应仍为 `74 passed, 0 failed`。
- 生成 `FishingNetMod/bin/Debug/net6.0/FishingNetMod 0.1.0.zip`。

- [ ] **Step 3: 检查 zip 内必须存在的文件**

```bash
unzip -l "FishingNetMod/bin/Debug/net6.0/FishingNetMod 0.1.0.zip"
```

确认至少包含以下条目：
- `FishingNetMod/FishingNetMod/manifest.json`
- `FishingNetMod/FishingNetMod/FishingNetMod.dll`
- `FishingNetMod/[CP] FishingNetMod/manifest.json`
- `FishingNetMod/[CP] FishingNetMod/content.json`
- `FishingNetMod/[CP] FishingNetMod/assets/fishing_net.png`
- `FishingNetMod/[CP] FishingNetMod/assets/i18n/default.json`

- [ ] **Step 4: 把测试与 zip 结果写入发布前验证报告**

在 `docs/reports/2026-06-11-pre-release-check.md` 中记录：
- `dotnet test` 输出摘要
- zip 结构检查结果
- 如果有缺失文件，列出完整路径

- [ ] **Step 5: 记录这一任务是否可进入下一步**

如果 `dotnet test` 或 zip 检查失败，后续任务仍然执行，但最终报告必须标注为阻塞或失败，不能写成发布前通过。

### Task 2: 尝试本机启动游戏并完成最小实机验证

**Files:**
- Modify: `docs/reports/2026-06-11-pre-release-check.md`

- [ ] **Step 1: 确认可用的启动路径**

先检查仓库说明和现有发布说明，确认当前推荐的游戏启动方式是通过 SMAPI 启动星露谷物语，而不是直接运行测试项目。

- [ ] **Step 2: 启动游戏并尝试进入存档**

尝试本机打开游戏到可交互状态，最低目标是进入一个已有存档。

- [ ] **Step 3: 在存档内验证最少一个核心功能**

优先验证以下之一：
- 输入 `fishing_net give copper` 后能获得铜网；
- 或手持渔网在水边主动撒网能打开数字挑战；
- 或手持渔网在水域放置成功。

- [ ] **Step 4: 记录明确结果**

在 `docs/reports/2026-06-11-pre-release-check.md` 中写清楚：
- `PASS`：进入存档并完成核心功能验证
- `BLOCKED`：无法启动、无法进入存档、或无法完成核心功能验证
- `FAIL`：能够验证，但发现功能错误

- [ ] **Step 5: 记录阻塞信息或复现步骤**

如果失败或阻塞，必须写明：
- 具体卡在哪一步
- 控制台/界面看到的现象
- 下一步需要人工处理的事项

### Task 3: 同步多人模式限制说明

**Files:**
- Modify: `README.md`
- Modify: `docs/release-notes/0.1.0.md`
- Modify: `docs/reports/2026-06-11-pre-release-check.md`

- [ ] **Step 1: 先定位现有多人限制表述的位置**

当前 README 和 release notes 已经写有“多人模式仍是实验性”，先确认是否还需要补充未定义规则说明。

- [ ] **Step 2: 在 README 中补充简短限制说明**

建议放在“已知限制”或“当前状态”附近，写明：
- 0.1.0 主要验证单人核心玩法
- 多人模式仍是实验性
- 收网权限、产出归属、任务进度归属尚未完整定义

- [ ] **Step 3: 在 release notes 中补充同样的限制说明**

保持和 README 一致，但不要写得比实际验证更强。

- [ ] **Step 4: 在发布前验证报告里记录本轮结论**

如果本轮没有改代码，只需写明：
- 当前版本的多人模式边界是什么
- 哪些规则仍是后续版本事项

- [ ] **Step 5: 避免把实验性多人模式写成已完成支持**

检查措辞，确保不会让读者误以为多人玩法已经完整验收。

### Task 4: 检查仓库是否存在未解释的临时文件

**Files:**
- Modify: `docs/reports/2026-06-11-pre-release-check.md`

- [ ] **Step 1: 再次查看仓库状态**

```bash
git status --short
```

- [ ] **Step 2: 区分正常变更和异常文件**

正常变更：
- 新写的设计/计划/报告文档
- 由 `dotnet test` 生成的 zip 或构建产物

异常文件：
- 来源不明的图片
- 临时日志
- 不属于本轮工作的文件

- [ ] **Step 3: 把仓库状态结论写入报告**

在 `docs/reports/2026-06-11-pre-release-check.md` 中写清楚：
- 仓库是否干净
- 是否有未解释临时文件
- 如果有，列出文件名与建议处理方式

- [ ] **Step 4: 不要擅自删除来源不明文件**

如果发现不明确的文件，只记录，不删除；留给用户确认。

### Task 5: 汇总发布前结论并收尾

**Files:**
- Modify: `docs/reports/2026-06-11-pre-release-check.md`

- [ ] **Step 1: 汇总四项检查的最终状态**

把以下内容写成表格或分项列表：
- 自动测试结果
- zip 结构结果
- 实机复测结果
- 多人模式限制说明更新结果
- 仓库临时文件检查结果

- [ ] **Step 2: 给出明确最终结论**

结论只能是以下之一：
- `PASS`：四项都满足发布前要求
- `BLOCKED`：至少一项未达到最低标准
- `FAIL`：验证到了实际功能缺陷

- [ ] **Step 3: 复核报告里的措辞**

确保没有把旧的 2026-06-08 PASS 报告误写成本轮新结论，也不要把构建成功误写成实机通过。

- [ ] **Step 4: 最后检查工作树状态**

```bash
git status --short
```

确认本轮只留下预期的文档和构建产物变更，或者清楚列出了需要用户确认的文件。

- [ ] **Step 5: 提交收尾变更**

```bash
git add README.md docs/release-notes/0.1.0.md docs/reports/2026-06-11-pre-release-check.md
# 如果本轮没有改这些文件，只提交实际发生变化的文件
git commit -m "docs: record pre-release verification"
```

---

## Self-Review Checklist

- 覆盖了 spec 的四个任务：实机复测、zip 安装校验、多人限制说明、临时文件确认。
- 没有写占位内容或模糊步骤。
- 文件路径精确到仓库内现有位置。
- 结果状态使用了明确的 `PASS` / `BLOCKED` / `FAIL`。
- 计划没有要求修改游戏逻辑，符合 spec 范围。
- 计划中明确要求不要把旧的 2026-06-08 手动测试报告误当成本轮结果。
