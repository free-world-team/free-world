# G3.6 最低规格物理机认证指南

## 1. 目的

本流程只认证 `qinglan-demo-g3.6-rc2` 的锁定构建，不允许把虚拟机、远程显示适配器、当前高配参考目标机
或手填结论当作最低规格证据。认证结果同时受本地硬件采集、30 分钟 Development Player Target 性能、
Release Player Smoke、两个 Build Manifest、证据 Hash 和人工见证约束。性能必须使用 Development Player，
因为正式 GC 门禁要求的 Unity Profiler 逐帧分配计数器在非 Development Player 中不可用；Release Player
另行验证最终发布构建的正式内容和生命周期，不能以此为由降低 GC 门禁。

## 2. 锁定认证包络

| 项目 | 必须满足 |
|---|---|
| 候选 | Commit `9984bcc5582bd827372768993953645d754cc463`，Tag `qinglan-demo-g3.6-rc2` |
| 操作系统 | Windows 10 x64 |
| CPU | 恰好 4 个物理核心，4—8 个逻辑处理器 |
| 内存 | 8 GB 档，实际采集值 7.5—10 GiB |
| GPU | 独立或物理显示适配器，2 GB 档，实际采集值 1.75—3 GiB |
| 图形与分辨率 | Direct3D 11，1920×1080，非远程显示 |
| 性能负载 | `target`：54,000 Tick、1,200 敌人、900 投射物、500 拾取物、200 VFX |
| 性能 Player | Windows Development Manifest 锁定的 EXE，保留完整 Profiler 指标 |
| 发布 Player | 非 Development、Null Platform、正式内容与完整生命周期 Smoke `PASS` |

高于包络的机器不是“更严格的最低配置认证”，只能作为参考目标机证据。低于包络的机器也不能替代已冻结
最低规格；如需修改规格，必须先按 Change Request 和 ADR 流程变更项目约束。

## 3. 准备

1. 从 GitHub Actions Run `31370035860` 下载 Artifact
   `qinglan-demo-release-candidate-9984bcc5582bd827372768993953645d754cc463`；
2. 将 Artifact 完整解压到本地，不修改 Development/Release 两套 `AzureSword.exe` 或 Data 目录；
3. 保留 Artifact 内的 `development-build-manifest.json` 与 `release-build-manifest.json`；
4. 本地仓库必须能解析锁定 Commit 和 Tag；
5. 由实际在场见证人提供姓名、角色，并明确执行 `-AttestPhysicalHardware`。

## 4. 一键认证

在项目根目录执行；替换两个路径和见证人信息：

```powershell
& .\Scripts\certify-qinglan-g36-minimum-spec.ps1 `
  -PerformanceExecutable 'D:\QinglanRC2\Builds\WindowsDevelopment\AzureSword.exe' `
  -ReleaseExecutable 'D:\QinglanRC2\Builds\WindowsRelease\AzureSword.exe' `
  -DevelopmentManifest 'D:\QinglanRC2\development-build-manifest.json' `
  -ReleaseManifest 'D:\QinglanRC2\release-build-manifest.json' `
  -OperatorName '真实姓名' `
  -OperatorRole 'QA / Release Manager' `
  -AttestPhysicalHardware `
  -Notes '机器资产编号、测试地点和必要说明'
```

脚本先用本机 CIM/WMI 采集硬件并校验包络。机器不匹配时不会开始 30 分钟运行；匹配时依次执行 Target
性能和 Release Player Smoke，最后生成并校验签名证据。只有全部 `PASS` 才把以下文件安装到
`Docs/DemoDevelopment/Assets/G3.6/Final`：

- `minimum-spec-hardware.json`
- `minimum-spec-target-player.json`
- `minimum-spec-release-player.json`
- `minimum-spec-development-manifest.json`
- `minimum-spec-release-manifest.json`
- `minimum-spec-review.json`
- `minimum-spec-validation.json`

## 5. 独立复验

已有四份源证据时可单独运行：

```powershell
& .\Scripts\validate-qinglan-g36-minimum-spec.ps1 `
  -HardwarePath 'TestResults/QinglanDemo/G3.6/MinimumSpec/minimum-spec-hardware.json' `
  -PerformancePath 'TestResults/QinglanDemo/G3.6/MinimumSpec/target-player.json' `
  -ReleasePlayerPath 'TestResults/QinglanDemo/G3.6/MinimumSpec/release-player.json' `
  -DevelopmentManifestPath 'TestResults/QinglanDemo/G3.6/MinimumSpec/development-build-manifest.json' `
  -ReleaseManifestPath 'TestResults/QinglanDemo/G3.6/MinimumSpec/release-build-manifest.json'
```

退出码 `0/1/2` 分别表示 `PASS/FAIL/NOT_RUN`。候选汇总同时核对 `minimum-spec-review.json` 的 SHA-256
与 `minimum-spec-validation.json.sourceSha256`；缺验证文件、Hash 不匹配、候选不一致或任何包络失败均不会
被接受。

## 6. 真实性边界

- `-AttestPhysicalHardware` 只代表实际见证人确认，不可由概括授权、自动 Agent 或项目所有者的泛化许可代签；
- 采集 JSON 不应手工修改；修改会使校验 Hash 与审计链失效；
- 当前 i7-12700F / RTX 3060 Ti 结果继续保留为 G3.5 参考目标机 `PASS`，不是最低规格 `PASS`；
- 缺物理机器或见证人时结论必须保持 `NOT_RUN / NO-GO`。
