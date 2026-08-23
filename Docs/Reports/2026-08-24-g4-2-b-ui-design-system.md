# Codex 结果报告

- 任务：完成《剑起青岚》G4.2-B UI Design System 与全流程界面重制
- 里程碑：G4.2-B
- 分支：`codex/qinglan-demo-implementation`
- Git Commit：实现 `dcd0bce`、低分辨率修复 `0d4e5a0`、200% 响应式修复 `63d6e9f`；本报告提交见 Git 历史
- 日期：2026-08-24

## 1. 实现范围

- 标题、角色、地图、构筑、设置、升级/奖励、叙事/结算、Hub 不再共用同一纵向菜单构图；
- 角色、地图、构筑和设施页增加消费正式 Visual Catalog 的展示区，操作卡增加青瓷语义色侧标；
- 保留单 Canvas、单 Presenter、单 Command 和现有键鼠/手柄路径，没有复制 UI 或玩法状态；
- `fontScale` 支持 100/125/150/175/200%，Settings 3 保持原 wire 和版本，新增 ADR 0032；
- 设置预览、生命、运行状态和目标摘要按字号响应式扩展；Preview/Hero/HUD 全部进入 TMP 零溢出门禁；
- Player 自动验收新增 200% 截图、实际分辨率检查、控件级溢出诊断和失败即阻断；
- 实际生成 Windows Release Candidate，并在当前硬件完成 720p、1080p、1440p、2560×1080 四组 90 秒 Player 验收。

未导入第三方 UI 包或来源不明资产，未改变 Simulation、Content Schema 6、30 Hz Tick、稳定 ContentId、
资源加载后端或程序集依赖方向。没有合并 `main`、打标签或创建商店提交。

## 2. 新增和修改文件

| 文件 | 变更摘要 |
|---|---|
| `Assets/Game/UI/QinglanRuntimeUiRoot.cs` | 页面专属构图、展示区、语义卡片、200% 响应式 Settings/HUD 与完整溢出检测。 |
| `Assets/Game/UI/QinglanUiTheme.cs` | 200% 上限、动效时长、安全边距和控件高度令牌。 |
| `Assets/Game/Application/PresentationContracts.cs`、`SaveModels.cs` | Runtime/Settings 3 的 `fontScale` 有效范围扩展到 2.0。 |
| `Assets/Game/Infrastructure/QinglanDemoFlowController.cs` | 字号按 0.25 在 1.0—2.0 循环。 |
| `Assets/Game/Infrastructure/QinglanG40VisualAcceptanceRunner.cs` | 200% 截图、零溢出硬门禁与控件级诊断。 |
| `Scripts/run-qinglan-g42-visual-acceptance.ps1` | 分辨率参数、实际输出尺寸和八张可访问性截图检查。 |
| `Assets/Tests/EditMode/QinglanG26UiInputTests.cs`、`QinglanG42VisualThemeTests.cs` | Settings 往返、页面锚点、200% 卡片及高密度 Objective HUD 回归。 |
| `Assets/Tests/PlayMode/QinglanG33RuntimeLocalizationPlayModeTests.cs` | 正式字体 zh-Hans/en/Pseudo 200% 设置页零溢出用例。 |
| `Docs/ADR/0032-g4-2-b-ui-layout-and-settings-200-percent.md` | 页面布局策略和 Settings 3 值域扩展的兼容/回滚决定。 |
| 架构、Schema、Save、测试、Style Bible 和执行顺序文档 | 同步 200% 真值、用户持续执行指令与证据边界。 |

## 3. 关键架构决定

- 页面差异由 `QinglanRuntimeUiRoot` 私有布局策略表达，不增加页面 Presenter 或第二套选择状态。
- Settings 3 只放宽 `fontScale` 值域，不改变 wire；旧 Settings 3 向后兼容，新保存的 1.75/2.0 不能由旧客户端读取。
- 颜色不是唯一状态通道；卡片继续保留焦点形状、明度、文字和禁用状态。
- 200% 不是仅放大字号：设置预览、标题列和战斗 HUD 都重新分配安全区。
- 自动 Player 门禁直接读取 TMP 溢出状态；首轮和第二轮 720p 失败均保留并修复，没有降低门槛。
- 物理显示器最高为 2560×1440；4K 请求被操作系统降级，不能记作 4K PASS。

## 4. 实际执行的命令

```text
$env:UNITY_PATH='C:\Program Files\Unity\Hub\Editor\6000.3.20f1\Editor\Unity.exe'; .\Scripts\test.ps1 -Platform EditMode -ResultsDirectory TestResults\G42B\Responsive200\Final
$env:UNITY_PATH='C:\Program Files\Unity\Hub\Editor\6000.3.20f1\Editor\Unity.exe'; .\Scripts\test.ps1 -Platform PlayMode -ResultsDirectory TestResults\G42B\Responsive200\Final
$env:UNITY_PATH='C:\Program Files\Unity\Hub\Editor\6000.3.20f1\Editor\Unity.exe'; .\Scripts\validate.ps1 -LogPath TestResults\G42B\MatrixFinal2\validation.log
$env:UNITY_PATH='C:\Program Files\Unity\Hub\Editor\6000.3.20f1\Editor\Unity.exe'; .\Scripts\build-windows-release.ps1 -LogPath TestResults\G42B\MatrixFinal2\build-windows-release.log
.\Scripts\run-qinglan-g42-visual-acceptance.ps1 ... -ScreenWidth 1280 -ScreenHeight 720
.\Scripts\run-qinglan-g42-visual-acceptance.ps1 ... -ScreenWidth 1920 -ScreenHeight 1080
.\Scripts\run-qinglan-g42-visual-acceptance.ps1 ... -ScreenWidth 2560 -ScreenHeight 1440
.\Scripts\run-qinglan-g42-visual-acceptance.ps1 ... -ScreenWidth 2560 -ScreenHeight 1080
.\Scripts\run-qinglan-g42-visual-acceptance.ps1 ... -ScreenWidth 3840 -ScreenHeight 2160
git add -A
git commit ...
git push origin codex/qinglan-demo-implementation
```

中间失败证据：首轮 720p 的 `ObjectiveLabel` 在 150% 溢出；第二轮 720p 的 `PreviewText`、`VitalsLabel`
和 `RunStatusLabel` 在 200% 溢出；4K 请求实际被降为 2560×1440。前三项均按实际尺寸修复并重跑，4K
硬件限制没有伪装成通过。

## 5. 测试结果

| 检查 | 结果 | 证据 |
|---|---|---|
| 编译 | PASS | 全量 Unity Test Runner、Validation 和 Windows Release Build 均完成编译。 |
| EditMode | PASS | 470/470，0 failed/skipped；SHA-256 `6AE9BDFDB3BE02889371AA85B64572630A1D8633FDBA255F3F075853434F8D4C`。 |
| PlayMode | PASS | 24/24，0 failed/skipped；SHA-256 `5440479F41681FB387A79ADB3C9766564C2D8262BCD726837F81C1EE3A78E796`。 |
| 内容/项目验证 | PASS | `Validation result: PASS`；Release Validator=`PASS`，Placeholder=0，未批准资产=0。 |
| Windows Release Build | PASS | Unity 6000.3.20f1 / StandaloneWindows64 / 非 Development；Manifest `Succeeded`。 |
| 720p Player | PASS | 1280×720，87/87 响应，92.65 s，200%/可访问性零溢出。 |
| 1080p Player | PASS | 1920×1080，88/88 响应，92.78 s，200%/可访问性零溢出。 |
| 1440p Player | PASS | 2560×1440，88/88 响应，93.08 s，200%/可访问性零溢出。 |
| 21:9 Player | PASS | 2560×1080，87/87 响应，92.76 s，200%/可访问性零溢出。 |
| 实机 4K | NOT RUN | 请求 3840×2160 时当前显示器/窗口系统实际输出 2560×1440；外层尺寸门禁正确 FAIL。 |
| 独立人类视觉 Rubric | NOT RUN | 用户要求不等待审核会继续工程执行，但不能替代独立签字。 |
| 长时性能/最低规格 | NOT RUN | 四组 90 秒视觉运行不是目标 GPU、1% Low 或长时 Soak 认证。 |

四组成功 Player 的峰值一致：Actor 239、Pickup 351、VFX 90，`accessibilityTextOverflowObserved=false`。

## 6. 构建产物

- 配置：Windows x64 / `WindowsReleaseCandidate` / Unity 6000.3.20f1 / 非 Development
- 路径：`Builds/WindowsRelease/AzureSword.exe`
- 文件 Hash：SHA-256 `34C4E304E53E56499267DFD9C975C63DC279ED3011A69A8CA16EB207F1856A8F`
- Build Manifest：`Builds/WindowsRelease/BuildManifest.json`，SHA-256 `E6838FBD5C7EF8391CC06D51892A97054838EDC7B01A8E94BB3420BAE2C11F51`
- Manifest Git：`63d6e9f206df58532a87b4772b31740bef630e4d`，`workingTreeClean=true`
- Release 内容：正式包 1 个、Placeholder=0、未批准资产=0、Release Validator=`PASS`

## 7. 未执行项目

- 物理 3840×2160 Player：`NOT RUN`，当前显示器上限 2560×1440。
- 独立人类视觉方向/页面矩阵签字：`NOT RUN`。
- 最低规格物理机器、目标 GPU、1% Low 和长时 Soak：`NOT RUN`。
- 未合并 `main`、未打 Release 标签、未创建商店提交。

## 8. 已知限制和风险

- 程序化 uGUI 构图已按现有单 Canvas 架构重制，但没有为了视觉文件形式而拆成重复 Prefab 状态源。
- 自动截图和零溢出能证明可运行/可读，不等于独立人类审美评分。
- Settings 3 的 1.75/2.0 对旧客户端向前不兼容；回滚前应先把字号改回不高于 1.5。
- 4K 同为 16:9，布局逻辑已由 1080p/1440p覆盖，但这不能代替物理 4K 清晰度和像素级实测。

## 9. 未完成项

- 在支持 3840×2160 的物理显示器上重跑同一严格尺寸门禁。
- 独立人类对标题、角色、地图、构筑、设置、结算、灰阶和可访问性截图签字。
- G4.2-C—F 的世界环境、角色动画、VFX/音频和最终整合尚未在本报告范围内完成。

## 10. 下一步前置条件

- 用户已明确授权不等待阶段审核并持续到最终实机测试，因此工程执行直接进入 G4.2-C。
- G4.2-C 继续复用现有正式资产、provenance、Addressables 和集中 Presentation Owner；不引入未批准第三方运行时包。
- 4K/独立人工未执行项必须继续保留在最终 G4.2 报告，不能因进入下一包而消失。

## 11. 结论

`INCOMPLETE`。

G4.2-B 工程实现、当前硬件自动测试、Release Build 和四组可执行 Player 证据已关闭；但强制的物理 4K 与
独立人类签字仍为 `NOT RUN`，因此按真实性规则不能把整个里程碑写为 `COMPLETE`。按用户当前明确指令继续
G4.2-C，不改变上述审计结论。
