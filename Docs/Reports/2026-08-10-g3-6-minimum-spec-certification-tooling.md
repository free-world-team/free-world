# Codex 结果报告

- 任务：补齐《剑起青岚》Demo G3.6 最低规格物理机认证与防伪证据链
- 里程碑：G3.6 外部门禁准备
- 分支：`codex/qinglan-demo-implementation`
- 候选 Git Commit：`9984bcc5582bd827372768993953645d754cc463`
- 日期：2026-08-10

## 1. 实现范围

实现 RC2 最低规格物理机的一键采集、双 Player 认证、机器校验、Hash 配对和候选证据安装。性能阶段使用同
Commit 的 Windows Development Player，以保留 G3.5 所需的 Unity Profiler 逐帧 GC 分配计数器；发布闭环
使用 Windows Release Player。两个 EXE 均由各自 Manifest SHA-256 锁定，不能互换或用高配机冒充。

本任务没有伪造实际最低规格结果。当前机器为 i7-12700F / 32 GB / RTX 3060 Ti，不属于冻结的 4 核 / 8 GB /
2 GB VRAM 包络，因此真实最低规格认证仍为 `NOT RUN`。

## 2. 新增和修改文件

| 文件 | 变更摘要 |
|---|---|
| `Templates/QINGLAN_G36_MINIMUM_SPEC_HARDWARE_TEMPLATE.json` | Schema 2 双 EXE 硬件采集模板 |
| `Scripts/capture-qinglan-g36-minimum-spec-hardware.ps1` | CIM/WMI 本机硬件、见证人和双 EXE Hash 采集 |
| `Scripts/certify-qinglan-g36-minimum-spec.ps1` | 包络预检、30 分钟 Development Target、Release Smoke 与证据安装 |
| `Scripts/validate-qinglan-g36-minimum-spec.ps1` | 候选、硬件、Manifest、性能、生命周期和证据 Hash 校验 |
| `Scripts/test-qinglan-g36-external-gates.ps1` | 合格/高配/篡改/候选汇总回归 |
| `Scripts/run-qinglan-g35-performance.ps1` | 支持指定候选来源、Pack 来源校验和 Windows PowerShell 5.1 启动参数 |
| `Scripts/verify-qinglan-g36-release-candidate.ps1` | 只接受 Hash 配对的最低规格 Review/Validation |
| `Docs/DemoDevelopment/G3_6_MINIMUM_SPEC_CERTIFICATION_GUIDE.md` | 实机操作、包络、证据与真实性边界 |
| `Docs/DemoDevelopment/29_G3_6_RELEASE_CANDIDATE.md` | 加入最低规格 Schema 2 强制门禁 |

## 3. 关键架构决定

- 不降低 `zeroProfilerGcAllocation` 门禁。Release Player 不暴露 `GC Allocated In Frame` 计数器，因此性能
  使用 Manifest 锁定的 Development Player；Release Player 继续承担最终内容与生命周期验证。
- 硬件包络固定为 Windows 10 x64、4 个物理核心、4—8 逻辑处理器、7.5—10 GiB RAM、1.75—3 GiB VRAM；
  高于或低于包络都不能写成最低规格 `PASS`。
- Review 与 Validation 通过 SHA-256 配对；候选汇总同时检查 Schema、Commit、验证状态和零问题数。
- 真实证据必须有具名人类现场见证；自动化合成 Fixture 只进入忽略的 `TestResults`，禁止安装为发布证据。
- 未修改模拟 Tick、Content Schema、存档格式、程序集依赖或第三方包，无需 ADR。

## 4. 实际执行的命令

```text
PowerShell Parser.ParseFile（6 个相关脚本）
powershell.exe -File Scripts/test-qinglan-g36-external-gates.ps1
powershell.exe -File Scripts/validate-qinglan-g36-minimum-spec.ps1（缺证据用例）
powershell.exe -File Scripts/run-qinglan-g35-performance.ps1 -Mode Quick -Executable Builds/WindowsRelease/AzureSword.exe -CandidateCommit 9984bcc...（诊断）
powershell.exe -File Scripts/run-qinglan-g35-performance.ps1 -Mode Quick -Executable Builds/WindowsDevelopment/AzureSword.exe -CandidateCommit 9984bcc...
Scripts/verify-qinglan-g36-release-candidate.ps1（合格证据与篡改证据两次候选汇总）
```

## 5. 测试结果

| 检查 | 结果 | 证据 |
|---|---|---|
| PowerShell 语法 | PASS | 6/6 相关脚本无 Parser 错误 |
| 合格合成最低规格证据 | PASS | Validator `PASS`、0 问题 |
| 高配伪证据拒绝 | PASS | 12 核 Fixture 返回 `FAIL` |
| Review 篡改拒绝 | PASS | 修改后 Hash 与 Validation 不匹配，候选门禁回到 `NOT_RUN` |
| 候选汇总集成 | PASS | 合格 Validation 被接受；人工门禁仍为 `NOT_RUN` |
| Development Player Quick | PASS | 300 Tick；59.968 FPS；1% Low 59.057；GPU p99 1.389 ms；Tick p99 1.282 ms；Profiler 600 样本/0 B；RC2 SHA 匹配 |
| Release Player 性能诊断 | FAIL | Profiler GC 样本为 0，证明非 Development Player 不适用 G3.5 性能门禁；未降低门禁，已改为双 Player 流程 |
| 编译 | NOT RUN | 本次只修改 PowerShell/JSON/Markdown，不修改 C# 或 Unity 内容 |
| EditMode | NOT RUN | 同上；RC2 已有 460/460 基线 |
| PlayMode | NOT RUN | 同上；RC2 已有 20/20 基线 |
| 构建 | NOT RUN | 复用 Hash 锁定的 RC2 Development/Release 构建 |
| 最低规格 30 分钟实体机 | NOT RUN | 当前机器不属于最低规格包络 |

## 6. 构建产物

- 配置：复用 RC2 Windows Development 与 Windows Release Candidate
- Development EXE SHA-256：`5d7eeb5359c2e35e4eb1f6a5844b25c3d7556795bd2f15ec234a2011406bc9c6`
- Release EXE SHA-256：`34c4e304e53e56499267dfd9c975c63dc279ed3011a69a8ca16eb207f1856a8f`
- 新构建：`NOT RUN`

## 7. 未执行项目

- Windows 10 x64 / 4 核 / 8 GB / 2 GB VRAM 物理机 30 分钟 Target：`NOT RUN`。
- 真实人类现场见证：`NOT RUN`。
- 人工/法律 Review 本身：`NOT RUN`；其 Schema 2 工具链已在前一独立提交完成。

## 8. 已知限制和风险

- CIM `Win32_VideoController.AdapterRAM` 在大显存 GPU 上可能截断，但本门禁只认证 2 GB 档；CPU 与 RAM
  包络仍会阻止当前高配机进入测试。
- 认证需要同时保留 Development 与 Release 两套 RC2 Artifact；缺任一 Manifest 或 EXE 即失败。
- 合成 Fixture 只能证明校验器逻辑，不能证明真实最低规格性能。

## 9. 未完成项

- 在符合包络的实体机执行一键认证并提交七份最低规格证据。
- 取得具名人类/法律 Review `PASS`。
- 两项均通过后重新运行 RC2 候选汇总。

## 10. 下一步前置条件

- 下载 GitHub Actions Run `31370035860` 的完整 RC2 Artifact。
- 准备符合冻结包络的 Windows 10 物理机和现场见证人。
- 按 `G3_6_MINIMUM_SPEC_CERTIFICATION_GUIDE.md` 执行命令。

## 11. 结论

`INCOMPLETE`

认证工具链与防伪回归已完成，但强制外部实体机和人类签字仍为 `NOT RUN`；发布决定保持 `NO-GO`。
