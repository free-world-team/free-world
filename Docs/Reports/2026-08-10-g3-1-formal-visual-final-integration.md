# Codex 结果报告

- 任务：G3.1 正式视觉运行时目录、最终集成与门禁
- 里程碑：G3.1 Final Integration（27 / 27 ART 批次后）
- 分支：`codex/qinglan-demo-implementation`
- Git Commit：本报告所在提交
- 日期：2026-08-10

## 1. 实现范围

完成 G3.1 最终集成：建立覆盖全部正式视觉输入的 Addressable Catalog；把正式 Player、敌人、Boss、
Projectile、Area、Pickup、Affix、Status 和 UI 热集接入现有 Presentation/UI；固化 Handle Owner、
Development Fallback 与 Project Validation 阻断边界；执行全量测试、实际 Addressables Build、1080p
标准/高对比截图审查、Windows x64 Development Build 和构建后 Player Smoke。

27 个 ART 文件生产、导入和 provenance 已由各自独立报告记录。本步骤未制作 G3.2 音频、G3.3 字体/
正文、G3.4 平衡，也未执行 G3.5 目标硬件性能或 G3.6 Release/Steam 合规。

## 2. 新增和修改文件

| 文件 | 变更摘要 |
|---|---|
| `Assets/Game/Presentation/FormalVisualCatalog.cs` | 正式视觉 Catalog、Binding、Usage 与地址/Profile 查询 |
| `Assets/Game/Infrastructure/QinglanFormalVisualLoader.cs` | Addressables 启动加载、唯一 Handle Owner 与释放 |
| `Assets/Game/Infrastructure/QinglanDemoRuntimeHost.cs` | Bootstrap 注入正式 Profile 和 UI Sprite 查询 |
| `Assets/Game/Presentation/EntityViews.cs`、`ViewPools.cs` | 正式 Profile 优先、Affix 正式叠加与高对比轮廓 |
| `Assets/Game/Presentation/PresentationEffects.cs`、`PresentationCoordinator.cs` | 正式 Status/VFX Sprite 解析 |
| `Assets/Game/UI/QinglanRuntimeUiRoot.cs` | 正式标题/页面背景、Panel、焦点和指针消费 |
| `Assets/Game/Editor/QinglanG31FormalVisualIntegration.cs` | 34 Profile、199 Binding、181 地址覆盖的 Catalog Authoring/验证/Build 命令 |
| `Assets/Game/Editor/WindowsDevelopmentBuild.cs` | 显式 Addressables 构建、失败阻断与已验证输出拷贝 |
| `Assets/Game/Editor/QinglanVisualAssetImporter.cs` | Pointer 单独使用 Readable RGBA32；其余 Glyph 保持 BC7 |
| `Assets/GameContent/QinglanDemo/Profiles/Visual/G3-1-INTEGRATION/` | 正式运行时 Catalog Asset |
| `Assets/Tests/EditMode/QinglanG31FormalVisualIntegrationTests.cs` | Catalog 覆盖、Alias、Profile、UI 查询测试 |
| `Assets/Tests/EditMode/AssemblyGovernanceTests.cs` | Infrastructure Addressables 依赖图契约 |
| `Assets/Tests/EditMode/QinglanG31ArtUi005Tests.cs` | Pointer/其余 Glyph 导入差异回归 |
| `Assets/Tests/PlayMode/BootstrapPlayModeTests.cs` | 真实 Bootstrap 正式 Catalog/标题背景加载 |
| `Docs/ADR/0028-g3-1-formal-visual-runtime-catalog.md` | 长期依赖、生命周期、降级与构建决策 |
| `Docs/ARCHITECTURE.md`、Demo 状态文档 | 架构真值、G3.1 完成状态和 G3.2 前置条件 |

## 3. 关键架构决定

- 采用 ADR 0028：Addressable `FormalVisualCatalog` 是正式视觉唯一运行时入口；Simulation/Application
  继续只传稳定 ID/快照，Presentation/UI 不持有 Addressables Handle。
- Catalog 间接覆盖 181/181 个 `visual.release` 地址，只直接持有启动/运行热集，避免无条件实例化全部
  正式 Sprite；新增内容仍以 Profile/Binding 扩展，不修改 Core。
- Development 缺映射时可诊断降级；Project Validation 对 Release 缺 Catalog、地址、Alias 或 Profile
  进行阻断。
- 开发构建显式构建并检查 Addressables，再让 Player Pipeline 只复制已验证输出；消除用户级偏好和
  隐式二次构建对产物完整性的影响。

## 4. 实际执行的命令

```text
Unity.exe -batchmode -nographics -quit -projectPath E:\ai\free-world -executeMethod Game.Editor.QinglanG31FormalVisualIntegration.Run
Unity.exe -batchmode -nographics -projectPath E:\ai\free-world -runTests -testPlatform EditMode -testFilter Game.Tests.EditMode.QinglanG31FormalVisualIntegrationTests
Unity.exe -batchmode -nographics -projectPath E:\ai\free-world -runTests -testPlatform EditMode
Unity.exe -batchmode -nographics -projectPath E:\ai\free-world -runTests -testPlatform PlayMode
Unity.exe -batchmode -nographics -quit -projectPath E:\ai\free-world -executeMethod Game.Editor.ProjectValidationCommand.Run
Unity.exe -batchmode -nographics -quit -projectPath E:\ai\free-world -executeMethod Game.Editor.QinglanG31AddressablesBuildCommand.Run
Unity.exe -batchmode -nographics -quit -projectPath E:\ai\free-world -executeMethod Game.Editor.QinglanG28ReadabilityCommand.Run
Unity.exe -batchmode -quit -projectPath E:\ai\free-world -executeMethod Game.Editor.QinglanG28ReadabilityCommand.Run
Unity.exe -batchmode -nographics -quit -projectPath E:\ai\free-world -executeMethod Game.Editor.WindowsDevelopmentBuild.BuildFromCommandLine
AzureSword.exe -screen-width 1280 -screen-height 720 -popupwindow -qinglanG28Smoke
git diff --check
```

## 5. 测试结果

| 检查 | 结果 | 证据 |
|---|---|---|
| 编译 | PASS | 最终 EditMode、PlayMode 与 Windows Development Build 均完成脚本编译 |
| G3.1 Focused EditMode | PASS | 4/4；181 地址、199 Binding、34 Profile、UI 查询/句柄边界 |
| EditMode | PASS | 最终 384/384，0 Failed，`TestResults/QinglanDemo/G3.1/formal-editmode-final.xml` |
| PlayMode | PASS | 最终 17/17，0 Failed，正式 Catalog 与 UI 背景均加载 |
| 内容验证 | PASS | `TestResults/QinglanDemo/G3.1/project-validation-final.log`；181/181 Release 地址、Provenance/Hash、Runtime Alias/Profile |
| Addressables Build | PASS | Packed Content：568 Locations；Player 包内 13 个文件、42,206,917 Bytes |
| 1920×1080 可读性 | PASS | 600 敌人、915 Active Views、318 P0 View；标准/高对比截图人工复核 |
| Windows Development Build | PASS | Unity 6000.3.20f1，StandaloneWindows64，Development |
| Player Smoke | PASS | 正式 Catalog/UI、标题→Run→结算→据点→重开、暂停、升级、存档全部为 true |
| 性能/Soak | NOT RUN | 目标 GPU Frame Time、显存和 1% Low 属于 G3.5；本次截图不替代性能证据 |

历史失败均已保留且未伪装为通过：首轮全量 EditMode 383/384 `FAIL`（依赖图期望）；首轮全量 PlayMode
16/17 `FAIL`（Pointer BC7 不可读）；无图形设备截图 `FAIL`（RenderTexture）；首轮构建后 Player Smoke
`FAIL`（隐式 Addressables 重建失败且产出缺内容）。对应修复后均重新实际执行并取得上述最终结果。

## 6. 构建产物

- 配置：Windows x64 Development / Unity 6000.3.20f1 / D3D12 Smoke
- 路径：`Builds/WindowsDevelopment/AzureSword.exe`
- 文件 Hash：`5D7EEB5359C2E35E4EB1F6A5844B25C3D7556795BD2F15EC234A2011406BC9C6`
- Build Manifest：`Builds/WindowsDevelopment/BuildManifest.json`
- Addressables Build Hash：`f234d906ff779639eb9d46602306c28f5be63bc54a177238021e25e08b893878`
- 截图：`TestResults/QinglanDemo/G3.1/formal-readability/readability-standard.png`、
  `readability-high-contrast.png`
- Player Smoke：`TestResults/QinglanDemo/G3.1/player-smoke-final.json`

## 7. 未执行项目

- G3.2 正式音乐、环境、机制和危险提示音：`NOT RUN`，必须作为下一独立工作包生产、授权和混音审查。
- G3.3 Noto CJK 固定版本/Hash/OFL Notice、TMP、简中/英文正文和伪本地化裁切：`NOT RUN`。
- G3.4 Seed 矩阵与三构筑数值冻结：`NOT RUN`。
- G3.5 目标硬件 CPU/GPU/GC/显存/1% Low：`NOT RUN`。
- G3.6 Release Build、Steam/平台合规和干净候选签字：`NOT RUN`；Development 包含 Placeholder。

## 8. 已知限制和风险

- Catalog 在启动低频阶段同步加载本地 Addressables；当前包为本地内容，若未来改远端必须改为异步加载页。
- Catalog 完整登记所有地址，但只直接预载热集；地图 Tile/Prop、Objective/Event/Landmark 的状态动画仍需
  由拥有对应状态字段的 Presentation Driver 按地址取用，不能把状态真值放回 View。
- 高密度截图在 RTX 3060 Ti 上验证的是视觉层级与可读性，不是 G3.5 的目标机器性能签字。
- Development Build 仍含测试/Placeholder Pack，禁止作为 Release 候选分发。

## 9. 未完成项

- G3.1 当前里程碑无未完成强制项。

## 10. 下一步前置条件

- 按 G0.4 Manifest 顺序进入 G3.2；先核对音频批次、来源/许可证、响度与 Ducking 预算。
- G3.2 必须独立提交并 Push，不提前混入 G3.3 字体/正文。

## 11. 结论

`COMPLETE`。
