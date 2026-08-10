# Codex 结果报告

- 任务：迁移 G3.6 归档外部门禁证据到防篡改 Schema
- 里程碑：G3.6 外部证据归档
- 分支：`codex/qinglan-demo-implementation`
- 候选 Git Commit：`9984bcc5582bd827372768993953645d754cc463`
- 日期：2026-08-10

## 1. 实现范围

把 RC2 归档中的旧版人工评审和最低规格 `NOT_RUN` 记录迁移为当前 Schema，并新增与 Review SHA-256 配对
的 Validation 文件。候选汇总重新读取完整自动化证据，确认 DOD-01—08 保持 `PASS`，DOD-09—10 因外部
工作尚未执行而保持 `NOT_RUN`。

同时修复人工评审与候选汇总脚本在 Windows PowerShell 5.1 下的 UTF-8 和相对路径兼容性。远程仓库 Runner
API 实际返回 `total_count=0`，因此没有可调度的最低规格实体机。

## 2. 新增和修改文件

| 文件 | 变更摘要 |
|---|---|
| `Docs/DemoDevelopment/Assets/G3.6/Final/manual-review.json` | Schema 2、候选锁定、四类证据 Hash、未签署状态 |
| `Docs/DemoDevelopment/Assets/G3.6/Final/manual-review-validation.json` | Review Hash 与 `NOT_RUN` 验证结果 |
| `Docs/DemoDevelopment/Assets/G3.6/Final/minimum-spec-review.json` | Schema 2、五项缺失源证据和 `NOT_RUN` 状态 |
| `Docs/DemoDevelopment/Assets/G3.6/Final/minimum-spec-validation.json` | Review Hash、候选和缺失源证据验证结果 |
| `Docs/DemoDevelopment/Assets/G3.6/Final/release-candidate-summary.json` | 加入两组 Validation Hash，保持 `NO-GO` |
| `Docs/DemoDevelopment/Assets/G3.6/Final/README.md` | 说明 Schema 迁移与真实性边界 |
| `Scripts/prepare-qinglan-g36-manual-review.ps1` | Windows PowerShell 5.1 相对路径和 UTF-8 兼容 |
| `Scripts/validate-qinglan-g36-manual-review.ps1` | Windows PowerShell 5.1 相对路径和 UTF-8 兼容 |
| `Scripts/verify-qinglan-g36-release-candidate.ps1` | UTF-8 JSON/XML 读取兼容 |

## 3. 关键架构决定

- `Validation=NOT_RUN` 只是确认当前 Review 的 Schema、候选和 Hash 正确，不代表外部评审已经执行。
- 人工表可以在未填写时以零验证问题保持 `NOT_RUN`；最低规格表明确列出五类缺失源证据。
- 只有 Validation 状态为 `PASS` 且 Hash 与 Review 一致时，候选汇总才会把外部门禁视为已运行并通过。
- 未改变游戏架构、Schema、存档、Tick 或第三方包，无需 ADR。

## 4. 实际执行的命令

```text
powershell.exe -File Scripts/prepare-qinglan-g36-manual-review.ps1 -Force
powershell.exe -File Scripts/validate-qinglan-g36-manual-review.ps1
powershell.exe -File Scripts/validate-qinglan-g36-minimum-spec.ps1（缺失源证据）
powershell.exe -File Scripts/verify-qinglan-g36-release-candidate.ps1 -CiStatus PASS -CandidateCommit 9984bcc...
gh api repos/free-world-team/free-world/actions/runners
Get-FileHash（四份 Review/Validation 与候选汇总配对）
```

## 5. 测试结果

| 检查 | 结果 | 证据 |
|---|---|---|
| 人工表生成 | PASS | Schema 2、候选 Commit/Tag 与 11 份证据 Hash 完整 |
| 人工表校验 | NOT RUN | 退出码 2、0 个 Schema/Hash 问题；尚无具名人类结论 |
| 最低规格校验 | NOT RUN | 退出码 2、明确缺少硬件/性能/Player/两个 Manifest |
| 完整候选重汇总 | PASS | 自动门禁与 DOD-01—08 全部 `PASS` |
| DOD-09—10 | NOT RUN | 两个外部门禁未执行，决定 `NO-GO` |
| Windows PowerShell 5.1 | PASS | 生成、校验与候选汇总不再因 UTF-8/`GetRelativePath` 失败 |
| GitHub Runner 可用性 | NOT RUN | 仓库注册 Runner 数为 0，无实体机任务可调度 |
| 编译/EditMode/PlayMode/构建 | NOT RUN | 本步骤只修改脚本、JSON 与 Markdown；沿用 RC2 已归档自动化证据 |

## 6. 构建产物

- 新构建：`NOT RUN`
- 复用候选：`qinglan-demo-g3.6-rc2`
- 自动化证据：GitHub Actions Run `31370035860`
- 当前决定：`NO-GO`

## 7. 未执行项目

- 具名人类视听、构筑差异和法律评审：`NOT RUN`。
- Windows 10 x64 / 4 核 / 8 GB / 2 GB VRAM 实体机认证：`NOT RUN`。

## 8. 已知限制和风险

- 仓库没有注册 Runner；实体机被注册或现场执行前无法产生最低规格结果。
- 用户的概括授权不是具名 Reviewer 签字，也不是实体机测量记录。
- 当前归档 Hash 在任何 Review 内容变化后都必须重新生成。

## 9. 未完成项

- 由具名人类完成并验证人工/法律 Review。
- 在符合包络的实体机完成并验证最低规格认证。

## 10. 下一步前置条件

- 提供真实 Reviewer 姓名、角色、逐项结论与签署时间。
- 提供或注册符合冻结包络的 Windows 10 物理机器。

## 11. 结论

`INCOMPLETE`

归档 Schema 和防篡改链已完成；外部事实尚未产生，发布决定继续为 `NO-GO`。
