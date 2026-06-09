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
