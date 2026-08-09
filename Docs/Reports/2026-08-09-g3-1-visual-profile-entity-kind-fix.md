# Codex 结果报告

- 任务：G3.1 正式 VisualProfile EntityKind 序列化偏移修复
- 里程碑：G3.1 缺陷修复（Manifest 计数保持 7 / 27）
- 分支：`codex/qinglan-demo-implementation`
- Git Commit：本报告所在提交
- 日期：2026-08-09

## 1. 实现范围

修复 `QinglanVisualAssetImporter.CreateVisualProfileAsset` 把从 1 开始的 `EntityKind` 底层值误作零基
`enumValueIndex` 的问题；重写陆青野、六普通敌人和两 Boss 共 9 个已批准正式 Profile 为 Actor=1；
同步三个 Schema 2 provenance 的实际输出 Hash，并增加 EntityKind 回归断言。未提交或注册
ART-SKILL-001 的新内容。

## 2. 新增和修改文件

| 文件 | 变更摘要 |
|---|---|
| `Assets/Game/Editor/QinglanVisualAssetImporter.cs` | 使用枚举底层 `intValue` 写入 EntityKind；保留并扩展可选 Pivot、EntityKind、Profile 尺寸参数 |
| `Assets/GameContent/QinglanDemo/Profiles/Visual/ART-CHAR-001/` | 陆青野 Profile 修正为 Actor=1，provenance Hash 同步 |
| `Assets/GameContent/QinglanDemo/Profiles/Visual/ART-ENEMY-001/` | 六敌人 Profile 修正为 Actor=1，provenance Hash 同步 |
| `Assets/GameContent/QinglanDemo/Profiles/Visual/ART-BOSS-001/` | 两 Boss Profile 修正为 Actor=1，provenance Hash 同步 |
| `Assets/Tests/EditMode/QinglanG31Art*001Tests.cs` | 在角色、敌人和 Boss 正式 Profile 测试中加入 Actor EntityKind 断言 |
| `Docs/DemoDevelopment/24_G3_1_FORMAL_VISUAL_ASSETS.md` | 记录缺陷、影响范围、修复与验证证据 |

## 3. 关键架构决定

- `EntityKind` 序列化使用 `SerializedProperty.intValue` 写底层枚举值，不依赖枚举声明顺序或零基索引。
- 旧的无参数调用仍默认 Actor、脚底 Pivot 和单位缩放；新增参数仅为技能 Projectile/Area VFX 提供中心
  Pivot、正确实体类型和缩放，不改变既有调用契约。
- 发现正式资产身份错误后立即修复并独立提交，不把修复静默混入 ART-SKILL-001 内容提交。
- GUID、StableId、Sprite 引用、Addressables 地址与标签不变；只修正类型字段并更新由此变化的 Hash。

## 4. 实际执行的命令

```text
Unity -executeMethod Game.Editor.QinglanVisualProfileCreateCommand.Run（陆青野一次）
Unity -executeMethod Game.Editor.QinglanVisualProfileCreateCommand.Run（首次草灵清单路径错误，FAIL，无 Profile 写入）
Unity -executeMethod Game.Editor.QinglanVisualProfileCreateCommand.Run（六敌人＋两 Boss，各一次）
.\Scripts\test.ps1 -Platform EditMode -ResultsDirectory TestResults/G31ProfileEntityKindFix
.\Scripts\test.ps1 -Platform PlayMode -ResultsDirectory TestResults/G31ProfileEntityKindFix
.\Scripts\validate.ps1 -LogPath TestResults/G31ProfileEntityKindFix/validation.log
```

首次草灵修复命令误用不存在的 `grass-spirit-animation-atlas.png`，Unity 明确返回 FAIL，未找到 Sprite、
未保存 Profile。清单改为仓库真实 `grass-spirit-directional-animation-atlas.png` 后继续；陆青野未重复执行。

## 5. 测试结果

| 检查 | 结果 | 证据 |
|---|---|---|
| 编译 | PASS | Unity 6000.3.20f1 完成修复脚本编译与 9 个 Profile 写入 |
| Profile 类型检查 | PASS | 9 个 YAML 均为 `entityKind: 1`；测试读取均为 `EntityKind.Actor` |
| EditMode | PASS | `TestResults/G31ProfileEntityKindFix/editmode.xml`，317/317 |
| PlayMode | PASS | `TestResults/G31ProfileEntityKindFix/playmode.xml`，17/17 |
| 内容验证/API Freeze | PASS | `TestResults/G31ProfileEntityKindFix/validation.log`；Profile Hash、provenance、Addressables 通过 |
| Addressables Build | NOT RUN | G3.1 最终集成统一执行 |
| Windows Development Build | NOT RUN | G3.1 最终集成统一执行 |
| 性能/Soak | NOT RUN | 本修复不改变渲染数量或模拟热路径；G3.5 统一执行 |

## 6. 构建产物

- 配置：9 个纠正后的正式 VisualProfile；无 Player Build
- 路径：`Assets/GameContent/QinglanDemo/Profiles/Visual/ART-{CHAR,ENEMY,BOSS}-001/`
- 文件 Hash：已逐项写入对应 Schema 2 provenance
- Build Manifest：NOT RUN

## 7. 未执行项目

Addressables Build、Windows x64 Development Build 和目标硬件性能未执行；这些属于 27 个 ART 批次
完成后的 G3.1 最终集成或 G3.5，不属于本缺陷修复门禁。

## 8. 已知限制和风险

- 当前正式 Profile 仍未由 Release Player 批量装载；本修复保证类型契约正确，不宣称运行时接入完成。
- 后续任何 Profile 作者工具都必须通过底层枚举值写入，并由测试断言实际 `EntityKind`。

## 9. 未完成项

- ART-SKILL-001 至 ART-UI-005 共 20 个视觉批次。
- G3.1 正式 Profile 运行时装载、Addressables Build 和 Windows Development Player。

## 10. 下一步前置条件

- 本修复独立提交并 Push 后，继续完成已开始但尚未提交的 ART-SKILL-001；不跳序。

## 11. 结论

`COMPLETE`（仅指 VisualProfile EntityKind 缺陷修复；G3.1 总里程碑仍为 `IN PROGRESS`）。
