# ADR 0027：G3.1 编辑器图集切片使用 Unity Sprite Data Provider

- 状态：Accepted
- 日期：2026-08-10
- 决策人：依据用户连续完成 Demo、全权自行决策与逐步骤提交 Push 授权
- 关联里程碑：G3.1、ART-UI-002
- 关联 CR：无需新增；仅替换正式资产编辑器导入实现，不新增 Gameplay 机制
- 承接：ADR 0004、0011、0026

## 背景

ART-UI-002 是 4×4、2048×2048 的正式 UI Sprite Atlas，需要 16 个稳定语义切片，并为前 8 个 Frame/
Panel 切片保存 64 px 九宫格边界。Unity 6 中已废弃的 `TextureImporter.spritesheet` 在资源已有 Sprite
Editor 数据时不能可靠替换切片，曾保留自动切出的非语义 Sprite。继续使用该接口会让导入结果依赖资源
历史状态，无法满足确定性生产和内容验证要求。

项目已随 Unity 6 使用官方 `com.unity.2d.sprite` 包，但 `Game.Editor` 尚未显式引用其 Editor 程序集。
受支持的 `ISpriteEditorDataProvider`、`ISpriteNameFileIdDataProvider` 和 `SpriteDataProviderFactories` 位于
`Unity.2D.Sprite.Editor`，只在 Editor 平台可用。

## 决策

- `Game.Editor` 新增对 `Unity.2D.Sprite.Editor` 的单向、Editor-only 直接引用。
- 多 Sprite 正式图集统一通过 Unity Sprite Data Provider 写入 `SpriteRect`、语义名称、Pivot、Border
  与 Name/FileId 映射，不再使用已废弃的 `TextureImporter.spritesheet`。
- 重复导入时按语义名称复用现有 `spriteID`；首次出现的语义切片才生成新 GUID，保证 `.meta` 幂等。
- Single Sprite 和现有纹理压缩、Addressables、Provenance 流程保持不变。
- Assembly 治理测试登记该引用，并继续验证整个产品/编辑器程序集图无环。

## 影响

依赖方向保持从最外层 `Game.Editor` 指向 Unity 官方 Editor 包，不进入 Player，不反向进入
`Game.Core`、`Game.Content.Runtime`、`Game.Simulation`、`Game.Application` 或表现运行时。没有新增
第三方运行时包，不改变 Content Schema 6、Save Schema 3、30 Hz 模拟 Tick、稳定 ContentId 或
Addressables 运行时加载架构。

代价是正式多 Sprite 导入工具需要 Unity 2D Sprite Editor 包存在；若未来 Unity 升级改变 Data Provider
API，必须在 Unity 升级 ADR 中同步迁移，而不能退回已废弃接口。

## 回滚

回滚需同时移除 `Game.Editor` 的程序集引用、Data Provider 调用和本 ADR，并恢复一个在当前 Unity 版本
中同样可验证、可幂等的切片写入方案。不得仅删除程序集引用或改回历史状态相关的旧 API。

## 测试

- ART-UI-002 定向 EditMode 验证 16 个语义 Sprite、512×512 Rect、中心 Pivot、前 8 个 64 px Border、
  后 8 个零 Border、BC7、无 MipMap、Addressables 和 Provenance。
- 连续执行两次导入并比较 Atlas `.meta` SHA-256，结果必须一致。
- 全量 EditMode 验证批准程序集图等价且无环；随后运行 PlayMode 与 Project Validation。
