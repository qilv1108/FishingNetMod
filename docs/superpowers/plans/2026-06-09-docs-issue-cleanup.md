# 文档问题清理 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 根据 `docs/问题文档.md` 修正文档入口和历史状态标注，降低历史文档与当前 0.1.0 状态互相冲突造成的误读。

**Architecture:** 本计划只修改 Markdown 文档，不改 C#、Content Patcher 配置或发布产物。通过根 `README.md` 建立当前权威入口，并在历史设计/报告/测试清单中添加状态说明，保持历史内容可追溯但不再被误认为当前状态。

**Tech Stack:** Markdown, Git, Bash, .NET 6 项目文档。

---

## 文件结构

| 文件 | 动作 | 职责 |
|---|---|---|
| `README.md` | 创建 | 作为项目根入口，说明项目、当前版本、构建测试、安装验证、文档索引和历史文档状态 |
| `2026-05-20-fishing-net-implementation-design.md` | 修改 | 在顶部标记为历史设计文档，并指向当前权威文档 |
| `docs/reports/2026-06-05-completion-report.md` | 修改 | 在顶部标记为历史完成报告，说明部分问题已被 2026-06-08 release gate 解决 |
| `docs/manual-test-checklist.md` | 修改 | 明确该文件是可复用测试模板，0.1.0 实际测试结果见报告文件 |
| `docs/问题文档.md` | 修改 | 追加处理记录，说明本轮计划覆盖的问题项 |

---

### Task 1: 标记原始设计文档为历史设计

**Files:**
- Modify: `2026-05-20-fishing-net-implementation-design.md:1-8`

- [ ] **Step 1: 在标题后插入历史状态提示**

把文件开头从：

```markdown
# 星露谷物语 - 捕鱼网 Mod 实现设计文档

**Goal:** 基于现有功能设计，采用 SMAPI + Content Patcher 分离式架构，实现组合型捕鱼网工具（主动撒网 + 被动放置），包含铜/铁/金/铱星四个等级，通过威利任务链解锁。
```

改为：

```markdown
# 星露谷物语 - 捕鱼网 Mod 实现设计文档

> **文档状态：历史设计。** 本文记录 2026-05-20 的早期实现设计，部分结构名、解锁条件和收网流程已经与当前 `0.1.0` 实现不同。当前发布状态以 `docs/release-notes/0.1.0.md`、`docs/reports/2026-06-08-0.1.0-manual-test-results.md` 和当前代码为准。

**Goal:** 基于现有功能设计，采用 SMAPI + Content Patcher 分离式架构，实现组合型捕鱼网工具（主动撒网 + 被动放置），包含铜/铁/金/铱星四个等级，通过威利任务链解锁。
```

- [ ] **Step 2: 验证历史提示存在**

Run:

```bash
grep -n "文档状态：历史设计" "2026-05-20-fishing-net-implementation-design.md"
```

Expected:

```text
3:> **文档状态：历史设计。** 本文记录 2026-05-20 的早期实现设计，部分结构名、解锁条件和收网流程已经与当前 `0.1.0` 实现不同。当前发布状态以 `docs/release-notes/0.1.0.md`、`docs/reports/2026-06-08-0.1.0-manual-test-results.md` 和当前代码为准。
```

- [ ] **Step 3: 提交历史设计状态标注**

```bash
git add "2026-05-20-fishing-net-implementation-design.md"
git commit -m "docs: mark original design as historical" -m "Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

Expected: commit succeeds.

---

### Task 2: 标记 2026-06-05 完成报告为历史快照

**Files:**
- Modify: `docs/reports/2026-06-05-completion-report.md:1-6`

- [ ] **Step 1: 在标题后插入历史快照提示**

把文件开头从：

```markdown
# Fishing Net Mod 完成报告

生成日期：2026-06-05

## 验证结果
```

改为：

```markdown
# Fishing Net Mod 完成报告

生成日期：2026-06-05

> **文档状态：历史快照。** 本报告记录 2026-06-05 当时的项目状态，其中“缺少 `.sln`”“70 个测试”“被动渔网仍使用原版纤维图标”“缺少真实游戏内验证记录”等问题已在后续 0.1.0 release gate 中处理。当前发布状态以 `docs/release-notes/0.1.0.md` 和 `docs/reports/2026-06-08-0.1.0-manual-test-results.md` 为准。

## 验证结果
```

- [ ] **Step 2: 验证历史快照提示存在**

Run:

```bash
grep -n "文档状态：历史快照" "docs/reports/2026-06-05-completion-report.md"
```

Expected:

```text
5:> **文档状态：历史快照。** 本报告记录 2026-06-05 当时的项目状态，其中“缺少 `.sln`”“70 个测试”“被动渔网仍使用原版纤维图标”“缺少真实游戏内验证记录”等问题已在后续 0.1.0 release gate 中处理。当前发布状态以 `docs/release-notes/0.1.0.md` 和 `docs/reports/2026-06-08-0.1.0-manual-test-results.md` 为准。
```

- [ ] **Step 3: 提交完成报告状态标注**

```bash
git add "docs/reports/2026-06-05-completion-report.md"
git commit -m "docs: mark completion report as historical snapshot" -m "Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

Expected: commit succeeds.

---

### Task 3: 新增根 README 作为当前文档入口

**Files:**
- Create: `README.md`

- [ ] **Step 1: 创建根 README**

创建 `README.md`，内容如下：

```markdown
# Fishing Net Mod

Fishing Net Mod 是一个《星露谷物语》SMAPI Mod，提供四级捕鱼网玩法：主动撒网、被动放置、数字挑战、每日产出和配方解锁。

## 当前状态

- 当前版本：`0.1.0`
- 发布定位：测试版，主要验证单人核心玩法循环
- C# Mod：`FishingNetMod/`
- Content Patcher 内容包：`FishingNetMod/[CP] FishingNetMod/`
- 测试项目：`FishingNetMod.Tests/`

## 构建与测试

从仓库根目录运行：

```bash
dotnet test
```

当前 `0.1.0` 发布记录中的自动化测试基线为：

```text
74 passed, 0 failed
```

测试会构建 Mod，并生成调试发布包：

```text
FishingNetMod/bin/Debug/net6.0/FishingNetMod 0.1.0.zip
```

## 安装与验证

安装方式和已知限制见：

- `docs/release-notes/0.1.0.md`

`0.1.0` 手动验证结果见：

- `docs/reports/2026-06-08-0.1.0-manual-test-results.md`

可复用的手动测试模板见：

- `docs/manual-test-checklist.md`

## 当前权威文档

优先阅读顺序：

1. `README.md`：项目入口和当前文档索引。
2. `docs/release-notes/0.1.0.md`：当前测试版功能、安装、验证和已知限制。
3. `docs/reports/2026-06-08-0.1.0-manual-test-results.md`：0.1.0 实际手动测试结果。
4. `docs/问题文档.md`：当前文档审查发现的问题和处理建议。

## 历史文档说明

以下文档保留为历史资料，不代表当前实现的全部细节：

- `2026-05-20-fishing-net-implementation-design.md`：早期实现设计，部分结构名、解锁条件和收网流程已演化。
- `docs/reports/2026-06-05-completion-report.md`：2026-06-05 完成度快照，其中部分问题已在 0.1.0 release gate 中解决。
- `docs/superpowers/plans/`：开发过程中的实施计划，部分勾选项可能已由后续提交或 release gate 覆盖。

## 已知限制

`0.1.0` 已知限制以 `docs/release-notes/0.1.0.md` 为准，主要包括：

- 多人模式仍是实验性。
- 文本/i18n 尚未统一。
- 主动撒网视觉反馈较基础。
- 被动渔网不单独强制固定 10% 垃圾概率。
- 收网流程使用数字挑战后直接返还物品，不是宝箱式界面。
```

- [ ] **Step 2: 验证 README 包含关键入口**

Run:

```bash
grep -n "docs/release-notes/0.1.0.md\|docs/reports/2026-06-08-0.1.0-manual-test-results.md\|docs/问题文档.md" README.md
```

Expected includes:

```text
39:- `docs/release-notes/0.1.0.md`
43:- `docs/reports/2026-06-08-0.1.0-manual-test-results.md`
54:2. `docs/release-notes/0.1.0.md`：当前测试版功能、安装、验证和已知限制。
55:3. `docs/reports/2026-06-08-0.1.0-manual-test-results.md`：0.1.0 实际手动测试结果。
56:4. `docs/问题文档.md`：当前文档审查发现的问题和处理建议。
```

Line numbers can differ if Markdown wrapping changes; the listed paths must appear.

- [ ] **Step 3: 提交根 README**

```bash
git add README.md
git commit -m "docs: add project readme entry point" -m "Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

Expected: commit succeeds.

---

### Task 4: 明确手动测试清单是模板

**Files:**
- Modify: `docs/manual-test-checklist.md:1-4`

- [ ] **Step 1: 在说明区补充模板状态和结果链接**

把文件开头从：

```markdown
# Fishing Net Mod 游戏内手动测试清单

> 用途：构建并安装 Mod 后，进游戏逐项打勾验证。

## 加载与命令
```

改为：

```markdown
# Fishing Net Mod 游戏内手动测试清单

> 用途：构建并安装 Mod 后，进游戏逐项打勾验证。本文是可复用测试模板，不直接表示当前测试是否已完成。`0.1.0` 实际手动测试结果见 `docs/reports/2026-06-08-0.1.0-manual-test-results.md`。

## 加载与命令
```

- [ ] **Step 2: 验证模板状态说明存在**

Run:

```bash
grep -n "可复用测试模板" "docs/manual-test-checklist.md"
```

Expected:

```text
3:> 用途：构建并安装 Mod 后，进游戏逐项打勾验证。本文是可复用测试模板，不直接表示当前测试是否已完成。`0.1.0` 实际手动测试结果见 `docs/reports/2026-06-08-0.1.0-manual-test-results.md`。
```

- [ ] **Step 3: 提交测试清单说明**

```bash
git add "docs/manual-test-checklist.md"
git commit -m "docs: clarify manual checklist is a template" -m "Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

Expected: commit succeeds.

---

### Task 5: 更新问题文档处理记录

**Files:**
- Modify: `docs/问题文档.md:91-100`

- [ ] **Step 1: 在结论前追加处理计划记录**

把 `docs/问题文档.md` 末尾从：

```markdown
## 结论

文档当前完成度约为 85%。`0.1.0` 测试版发布材料基本齐全，但需要补充“文档状态标注”和“入口 README”，以避免历史文档与当前实现互相冲突造成误读。
```

改为：

```markdown
## 本轮处理计划

2026-06-09 的文档清理优先处理以下事项：

1. 给 `2026-05-20-fishing-net-implementation-design.md` 添加历史设计状态说明。
2. 给 `docs/reports/2026-06-05-completion-report.md` 添加历史快照状态说明。
3. 新增根目录 `README.md`，作为当前权威文档入口。
4. 在 `docs/manual-test-checklist.md` 中说明它是可复用测试模板，并指向 0.1.0 实际测试报告。

文档语言统一、当前架构说明和计划文档状态清理保留为后续独立整理项。

## 结论

文档当前完成度约为 85%。`0.1.0` 测试版发布材料基本齐全，但需要补充“文档状态标注”和“入口 README”，以避免历史文档与当前实现互相冲突造成误读。
```

- [ ] **Step 2: 验证处理计划记录存在**

Run:

```bash
grep -n "本轮处理计划\|新增根目录 `README.md`" "docs/问题文档.md"
```

Expected includes:

```text
98:## 本轮处理计划
104:3. 新增根目录 `README.md`，作为当前权威文档入口。
```

Line numbers can differ if prior edits change the file; both matched lines must appear.

- [ ] **Step 3: 提交问题文档处理记录**

```bash
git add "docs/问题文档.md"
git commit -m "docs: record issue cleanup plan" -m "Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

Expected: commit succeeds.

---

### Task 6: 最终文档一致性检查

**Files:**
- Verify: `README.md`
- Verify: `2026-05-20-fishing-net-implementation-design.md`
- Verify: `docs/reports/2026-06-05-completion-report.md`
- Verify: `docs/manual-test-checklist.md`
- Verify: `docs/问题文档.md`

- [ ] **Step 1: 验证所有新增状态标注存在**

Run:

```bash
grep -n "文档状态：历史设计" "2026-05-20-fishing-net-implementation-design.md"
grep -n "文档状态：历史快照" "docs/reports/2026-06-05-completion-report.md"
grep -n "可复用测试模板" "docs/manual-test-checklist.md"
grep -n "当前权威文档" README.md
grep -n "本轮处理计划" "docs/问题文档.md"
```

Expected: each command prints one matching line.

- [ ] **Step 2: 验证文档链接目标存在**

Run:

```bash
test -f README.md
test -f "docs/release-notes/0.1.0.md"
test -f "docs/reports/2026-06-08-0.1.0-manual-test-results.md"
test -f "docs/manual-test-checklist.md"
test -f "docs/问题文档.md"
```

Expected: no output and exit code 0.

- [ ] **Step 3: 验证工作区状态**

Run:

```bash
git status --short
```

Expected: no output after all task commits.

- [ ] **Step 4: 可选运行测试确认代码未受影响**

Run:

```bash
dotnet test
```

Expected includes:

```text
失败:     0
```

如果只改 Markdown，测试不是文档正确性的直接验证；但运行它可以确认没有意外破坏项目状态。

---

## 自查结果

- **Spec 覆盖：** `docs/问题文档.md` 中建议处理顺序的前 3 项均有任务覆盖；语言统一、当前架构说明和计划文档状态清理被明确保留为后续独立整理项，避免本次文档清理扩大范围。
- **占位符扫描：** 计划中没有 TBD、TODO、未填写段落或“类似上一步”的省略写法；每个文件改动都给出明确文本。
- **类型一致性：** 本计划只改 Markdown，不涉及代码类型。文件路径与已读取的仓库结构一致，新增根 `README.md` 不覆盖现有项目 README。
