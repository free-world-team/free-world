# Codex 结果报告

- 任务：完成《剑起青岚》Demo G3.6 Release Candidate、自动化门禁、CI 与最终证据归档
- 里程碑：G3.6
- 分支：`codex/qinglan-demo-implementation`
- Git Commit：候选 `9984bcc5582bd827372768993953645d754cc463`
- Git Tag：`qinglan-demo-g3.6-rc2`
- 日期：2026-08-10

## 1. 实现范围

完成一个仅装载正式 `qinglan.pack.demo` 的非 Development Windows x64 Release Candidate：正式 Catalog、
Addressables、Null Platform、Release Manifest、真实 Player Smoke、Compliance、干净 Checkout、自托管
GitHub Actions 和 DOD 汇总。修正汇总器，使人类 Reviewer 与最低规格物理机器成为真实阻断门禁，并支持
在证据提交晚于候选 Tag 时通过 `-CandidateCommit` 复核指定候选。

全部可自动化项目已完成；没有把缺失的外部人工/法律签字或最低规格硬件认证伪报为通过。因此当前里程碑
仍为 `INCOMPLETE`，发布决定为 `NO-GO`。

## 2. 新增和修改文件

| 文件 | 变更摘要 |
|---|---|
| `Assets/GameContent/QinglanDemo/Runtime/QinglanDemoContentPack.release.json` | 1 个正式 Pack、193 Definitions 的 Release 输入 |
| `Assets/Game/Editor/QinglanG36ReleaseBuild.cs` | 正式非 Development Windows Release 构建与 Manifest |
| `Assets/Game/Infrastructure/QinglanG36ReleasePlayerRunner.cs` | Release Player 正式资源、页面、Save、清理和离线 Smoke |
| `Scripts/audit-qinglan-g36-compliance.ps1` | 正式资产、Placeholder、权利、地址和包审计 |
| `Scripts/verify-clean-clone.ps1` | 干净克隆完整候选门禁与冷缓存 UPM 重试 |
| `Scripts/verify-qinglan-g36-release-candidate.ps1` | DOD 汇总、指定候选 Tag、人工 Reviewer 与最低规格阻断 |
| `.github/workflows/windows-self-hosted.yml` | Windows/Unity 自托管完整候选门禁 |
| `.gitattributes`、`QinglanG33AddressableGroupRules*` | 冷克隆 Hash/换行稳定性与 Localization Group 持久路由 |
| `Docs/DemoDevelopment/Assets/G3.6/Final/*` | RC2 Manifest、Player、CI、性能、合规、DOD 和未执行证据 |
| Demo Roadmap、Traceability、Known Issues、Execution Order | 更新为 RC2 实际 `NO-GO / INCOMPLETE` 状态 |

## 3. 关键架构决定

- Release 只允许一个正式 Pack；Placeholder、Default 测试组、未批准资产或远程平台后端均阻断。
- Release Player 真实启动 `GameBootstrapper`，不使用 Development/debug 标志，并验证正式视听、字体、
  双语/Pseudo、150% 布局、完整页面、胜利结算、Save、回据点、重开与 View 清零。
- Localization 使用持久化 Group Resolver，确保冷克隆导入不会把正式 String Table 移回 Default Group。
- 冷克隆中的暂时性 UPM `operation cancelled` 只在“非零退出、无 XML、日志精确命中”时重试一次；其他失败不吞掉。
- G3.6 合同优先于旧汇总器便利逻辑：`reviewerKind=human` 和最低规格 `physicalHardware=true` 都是发布必过项。
- 未改变 Tick、Content Schema、存档格式、程序集依赖方向或第三方包，无需新增 ADR。

## 4. 实际执行的命令

```text
Scripts/test.ps1 -Platform EditMode -ResultsDirectory TestResults/QinglanDemo/G3.6/CI
Scripts/test.ps1 -Platform PlayMode -ResultsDirectory TestResults/QinglanDemo/G3.6/CI
Scripts/validate.ps1 -LogPath TestResults/QinglanDemo/G3.6/CI/validation.log
Game.Editor.QinglanG28VerticalSliceCommand.Run
Game.Editor.QinglanG34BalanceCommand.Run
Scripts/run-qinglan-g35-performance.ps1 -Mode Cpu
Scripts/audit-qinglan-g36-compliance.ps1
Scripts/build-windows.ps1
Scripts/build-windows-release.ps1
Scripts/run-player-smoke.ps1
Scripts/verify-clean-clone.ps1
Scripts/verify-qinglan-g36-release-candidate.ps1 -CiStatus PASS -CandidateCommit qinglan-demo-g3.6-rc2
git tag -a qinglan-demo-g3.6-rc2 ...
git push origin codex/qinglan-demo-implementation
git push ... refs/tags/qinglan-demo-g3.6-rc2
```

GitHub Actions 成功 Run：<https://github.com/free-world-team/free-world/actions/runs/31370035860>。
首轮 Run `31367990349` 在 Checkout 因 `curl 56 connection reset` 失败，未执行 Unity/代码门禁；失败历史已保留。

## 5. 测试结果

| 检查 | 结果 | 证据 |
|---|---|---|
| 编译 | PASS | EditMode、PlayMode、Development/Release Build 均无编译失败 |
| EditMode | PASS | 460/460，0 失败 |
| PlayMode | PASS | 20/20，0 失败 |
| 内容验证 | PASS | `[Project Validation] PASS` |
| G2.8 Vertical Slice | PASS | 确定性、三路线与 Spawn Fairness |
| G3.4 Balance | PASS | 15 局 12 胜 3 负，路线胜局 3/4/5，18/18 奇物组合 |
| CPU Target | PASS | 54,000 Tick；平均 16.767 ms；p99 18.244 ms；热路径 0 B |
| GPU Target | PASS | 1080p 30 分钟；59.992 FPS；1% Low 59.891；GPU p99 2.163 ms |
| Compliance | PASS | issue 0、Placeholder 0、正式 Pack 1 |
| Windows Development | PASS | CI 当前候选构建成功 |
| Windows Release | PASS | 非 Development；Manifest Commit/Tag/Hash 匹配 RC2 |
| Release Player | PASS | Null Platform、正式资源、UI、Run、Save、Hub、Restart、清理均通过 |
| 干净 Checkout | PASS | 同一 RC2 提交全套门禁与源树清洁 |
| GitHub Actions | PASS | Run 31370035860 全步骤和 Artifact 上传成功 |
| 客观音频遮蔽压力 | PASS | 关键能量频段 SNR +10.557 dB；不替代人类试听 |
| 独立人工/法律签字 | NOT RUN | `manual-review.json` |
| 最低规格物理机器 | NOT RUN | `minimum-spec-review.json` |
| 2,000 Enemy 扩展 | FAIL | 非阻断容量观察；43.694 FPS、1% Low 36.768 |

## 6. 构建产物

- 配置：Windows x64 Release Candidate，非 Development，Null Platform
- CI Artifact：`qinglan-demo-release-candidate-9984bcc5582bd827372768993953645d754cc463`
- Artifact ID / 大小：`9056682211` / `358,377,900` bytes
- Release EXE SHA-256：`34c4e304e53e56499267dfd9c975c63dc279ed3011a69a8ca16eb207f1856a8f`
- Release Manifest SHA-256：`0eb04d0ca6519ee48dd865feb321bc76ff0255e63803d73ca449445ae42b6b6a`
- Build Manifest：`Docs/DemoDevelopment/Assets/G3.6/Final/release-build-manifest.json`
- 机器证据：`Docs/DemoDevelopment/Assets/G3.6/Final`

## 7. 未执行项目

- 独立人类简中/英文叙事、危险可读性、三构筑决策差异和目标声卡音频遮蔽评审：`NOT RUN`。
- 商业权利、Noto CJK 随包义务、Steam AI 披露与商店法律文本签字：`NOT RUN`。
- 4 核、8 GB、DX11、2 GB VRAM 最低规格物理机器认证：`NOT RUN`；参考目标机结果未冒充最低规格。
- Steam 商店发布、PR 与合并 main：不在当前授权范围，`NOT RUN`。

## 8. 已知限制和风险

- DOD-09—10 因两个外部门禁为 `NOT RUN`，最终汇总必须保持 `NO-GO`。
- 客观压力混音的未限制诊断叠加峰值为 +4.140 dBFS；监听文件已归一化，运行时依赖并发上限、优先级驱逐和 Ducking。
- 2,000 Enemy 扩展显示 CPU 容量拐点；正式 1,200 Enemy Target 已 PASS，但不得外推到更高同屏量。
- 最低规格、更多 GPU/驱动和真实目标声卡仍可能暴露参考目标机没有覆盖的问题。

## 9. 未完成项

- 用具名人类 Reviewer 完成并提交人工/法律签字。
- 在真实最低规格物理机器完成认证，并记录硬件、驱动、分辨率、帧时间、内存和 Player 结果。
- 两项均 PASS 后，对 `qinglan-demo-g3.6-rc2` 重新运行候选汇总；当前不允许发布。

## 10. 下一步前置条件

- `manual-review.json` 必须由人类 Reviewer 签署，设置 `reviewerKind=human`，四项结论全部为 `PASS`。
- `minimum-spec-review.json` 必须来自真实最低规格机器，设置 `physicalHardware=true` 且 Commit 为 RC2。
- 执行：`Scripts/verify-qinglan-g36-release-candidate.ps1 -CiStatus PASS -CandidateCommit qinglan-demo-g3.6-rc2`。

## 11. 结论

`INCOMPLETE`

自动化开发与 RC2 CI 已全部完成，但当前强制外部门禁为 `NOT RUN`；发布决定是 `NO-GO`。
