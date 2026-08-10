# Codex 结果报告

- 任务：正式字体、本地化三表与 Demo 运行时最终集成
- 里程碑：G3.3
- 分支：`codex/qinglan-demo-implementation`
- Git Commit：本提交（提交后以 Git 记录为准）
- 日期：2026-08-10

## 1. 实现范围

完成 FONT-001、FONT-002、LOC-UI-001、LOC-CONTENT-001、LOC-NARRATIVE-001 的最终运行时整合：
三套 Noto CJK TMP 字体通过 Addressables 加载，806 个双语 Key 由 UI/内容/叙事三表按稳定命名空间
解析，Pseudo 自动生成；页面、HUD、危险提示和伤害数字全部迁移到 TMP。完成实际字形预热、
provenance、150% 布局、全量回归、Windows Development Build 和构建后 Player Smoke。

未实施 G3.4 数值冻结、G3.5 目标硬件性能或 G3.6 Release Candidate。

## 2. 新增和修改文件

| 文件 | 变更摘要 |
|---|---|
| `Assets/Game/Editor/QinglanG33FinalIntegration.cs` | 三表 Preload、Pseudo 配置、全量字形收集/预热和 provenance 最终写回 |
| `Assets/Game/Editor/LocalizationProjectValidator.cs` | 正式表、Preload、系统字体禁用和逐字 Glyph 门禁 |
| `Assets/Game/Infrastructure/QinglanFormalFontLoader.cs` | 三套正式 TMP Addressables 唯一 Handle Owner |
| `Assets/Game/Infrastructure/QinglanDemoRuntimeHost.cs` | 启动加载、UI 注入、状态暴露和销毁释放 |
| `Assets/Game/Infrastructure/QinglanG28DevelopmentSmokeRunner.cs` | Player 三表、Locale、字形、150% 布局断言与诊断 |
| `Assets/Game/UI/QinglanRuntimeUiRoot.cs`、`RuntimeUiRoot.cs` | TMP 页面/HUD/危险文本、正式字体分工与响应式安全区 |
| `Assets/Game/UI/UnityLocalizationService.cs` | UI、Content、Narrative 稳定 Key 路由与 Pseudo 解析 |
| `Assets/Game/Presentation/PresentationEffects.cs` | 池化伤害数字从 Legacy Text 迁移到 TMP |
| 三套 TMP Font Asset、Pseudo Locale、Localization Addressables | 1093/563/842 字形、40 Atlas、六表 Preload 与正式地址 |
| `Assets/Tests/EditMode/QinglanG33FinalIntegrationTests.cs` | 路由、Preload、Pseudo、Glyph、默认字体与系统回退门禁 |
| `Assets/Tests/PlayMode/QinglanG33RuntimeLocalizationPlayModeTests.cs` | 实际加载、三 Locale 三表、全 TMP 树、Serif 和 150% 布局 |
| `Docs/ADR/0030-g3-3-formal-tmp-localization-runtime.md` | 正式字体、本地化和程序集依赖边界 |
| `Docs/ARCHITECTURE.md`、程序集 asmdef/治理测试 | 登记 TextMeshPro 直接 Package 依赖并保持产品图无环 |
| `Scripts/run-qinglan-g28-player-smoke.ps1` | 1920×1080 Player 启动参数与 G3.3 结果校验 |

## 3. 关键架构决定

- UI Key、内容 Key、故事/藏录/叙事 Key 分别进入三张正式 String Table；Presenter 与内容定义继续只传稳定 Key。
- Regular 用于正文/HUD，Bold 用于危险提示，Serif 用于标题/故事/结算；任何发布路径不得依赖操作系统或 Legacy 字体。
- Pseudo 使用全角 ASCII 和 `【】`，避免 Accenter 默认字符超出 Noto CJK 覆盖；所有实际 Pseudo 字符同样预热。
- 非运行页面扩展到完整安全区；Run HUD 才激活 HUD/危险层，隐藏文本不参加溢出判定。
- 采用 ADR 0030；不改变产品程序集之间的方向、模拟 Tick、Content Schema 或存档格式。

## 4. 实际执行的命令

```text
Unity.exe -batchmode -nographics -projectPath E:\ai\free-world -executeMethod Game.Editor.QinglanG33FinalIntegration.Run
Unity.exe -batchmode -nographics -projectPath E:\ai\free-world -executeMethod Game.Editor.QinglanG33FinalIntegration.RunFinalizeProvenance
Unity.exe -batchmode -nographics -projectPath E:\ai\free-world -runTests -testPlatform EditMode -testFilter Game.Tests.EditMode.QinglanG33FinalIntegrationTests
Unity.exe -batchmode -nographics -screen-width 1920 -screen-height 1080 -projectPath E:\ai\free-world -runTests -testPlatform PlayMode -testFilter Game.Tests.PlayMode.QinglanG33RuntimeLocalizationPlayModeTests
Scripts/test.ps1 -Platform All -ProjectPath E:\ai\free-world -ResultsDirectory TestResults\QinglanDemo\G3.3\FullRegressionFinal2
Scripts/validate.ps1 -ProjectPath E:\ai\free-world -LogPath TestResults\QinglanDemo\G3.3\validation-post-margin.log
Scripts/build-windows.ps1 -ProjectPath E:\ai\free-world -OutputPath Builds\WindowsDevelopment\AzureSword.exe -LogPath TestResults\QinglanDemo\G3.3\build-windows-post-margin.log -EvidenceRoot TestResults\QinglanDemo\G3.3\BuildEvidencePostMargin
Scripts/run-qinglan-g28-player-smoke.ps1 -ProjectPath E:\ai\free-world -Executable Builds\WindowsDevelopment\AzureSword.exe -LogPath TestResults\QinglanDemo\G3.3\player-smoke-delivery.log -ResultPath TestResults\QinglanDemo\G3.3\player-smoke-delivery.json
git diff --check
```

## 5. 测试结果

| 检查 | 结果 | 证据 |
|---|---|---|
| 编译 | PASS | 最终 Development Build 编译与 Player 启动成功 |
| G3.3 EditMode | PASS（4/4） | `TestResults/G33FinalEditMode.xml` |
| G3.3 PlayMode | PASS（2/2） | `TestResults/QinglanDemo/G3.3/MarginFixPlayMode.xml` |
| 全量 EditMode | PASS（445/445） | `TestResults/QinglanDemo/G3.3/FullRegressionFinal2/editmode.xml` |
| 全量 PlayMode | PASS（20/20） | `TestResults/QinglanDemo/G3.3/FullRegressionFinal2/playmode.xml` |
| 内容/治理验证 | PASS | `TestResults/QinglanDemo/G3.3/validation-post-margin.log` |
| Windows Development Build | PASS | `TestResults/QinglanDemo/G3.3/build-windows-post-margin.log` |
| 构建后 Player Smoke | PASS | `TestResults/QinglanDemo/G3.3/player-smoke-delivery.json` |
| 性能/Soak | NOT RUN | 属于后续 G3.5；本里程碑未宣称性能达标 |

中间失败均保留为真实记录：Pseudo 默认字符和五个装饰符号最初缺字；首次全量 EditMode 因 TMP
依赖白名单未登记为 444/445，布局隐藏层测试修正前也为 444/445；首次 Build 因 Windows 1224 文件映射
失败，干净进程重试通过；Player 初次发现 Pseudo 断言取错字符及 150% 页面溢出，修正后最终 PASS。

## 6. 构建产物

- 配置：Unity 6000.3.20f1，StandaloneWindows64，Development
- 路径：`Builds/WindowsDevelopment/AzureSword.exe`
- 文件 Hash：`5d7eeb5359c2e35e4eb1f6a5844b25c3d7556795bd2f15ec234a2011406bc9c6`
- Build Manifest：`Builds/WindowsDevelopment/BuildManifest.json`
- Addressables Build Hash：`18f62d94604b0fe9ae0d8834abb3f5ea7a3550ba02db8bc945da4db57c19cd20`

## 7. 未执行项目

- 目标硬件 1080p 60 FPS、1% Low、长时间 Soak：NOT RUN，属于 G3.5。
- Release 非 Development Build 与 Steam 合规包：NOT RUN，属于 G3.6。

## 8. 已知限制和风险

- TMP 资产保持 Dynamic/Multi Atlas 以满足内容扩展；Editor PlayMode 可能重新序列化运行时缓存，因此
  交付流程必须在所有 Editor 测试结束后执行 provenance 最终写回，再执行 Validation/Build。
- 当前构建 Manifest 仍包含开发/测试 Pack 与 Placeholder 标记；G3.6 Release Gate 必须剔除或阻断，
  本 G3.3 Development Build 不等同于 Release Candidate。
- Pseudo 的 Expander 会附加压力字符，属于布局压力设计；正式 en/zh-Hans 文案不受影响。

## 9. 未完成项

- G3.3 当前范围无未完成项。
- G3.4、G3.5、G3.6 按执行顺序待实施。

## 10. 下一步前置条件

- 在干净工作树上开始 G3.4，只修改内容数值、Seed 矩阵和对应测试基线。
- 不改变稳定 ID、Schema、随机流协议或已冻结的正式字体/本地化边界。

## 11. 结论

`COMPLETE`
