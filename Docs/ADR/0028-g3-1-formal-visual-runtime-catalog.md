# ADR 0028：G3.1 正式视觉目录、Addressables Owner 与降级边界

- 状态：Accepted
- 日期：2026-08-10
- 决策人：依据用户连续完成 Demo、全权自行决策与逐步骤提交 Push 授权
- 关联里程碑：G3.1、M13
- 承接：ADR 0004、0009、0025、0026、0027

## 背景

27 个正式视觉生产批次已完成文件、Provenance、导入和 Addressables 注册，但 G2.7 Runtime 仍只把
`VisualProfileCatalog` 作为可选参数，Bootstrap 固定传入空目录。UI、Pickup、Affix 与 Status 也没有
统一的正式资源映射和句柄 Owner。若让 Simulation、UI 或各个 View 自行调用 Addressables，会破坏稳定
ContentId 边界并造成无法审计的句柄生命周期。

## 决策

- 新增 Addressable `FormalVisualCatalog`，以稳定 Key 间接映射全部 `visual.release` 地址，并只直接预载
  首屏/UI Chrome、状态、Pickup、Affix 与正式实体 Profile 所需的 Sprite。
- `Game.Infrastructure` 新增对 `Unity.Addressables` / `Unity.ResourceManager` 的依赖；
  `QinglanFormalVisualLoader` 是唯一启动加载和释放 Owner。Presentation/UI 不持有 Addressables Handle。
- `QinglanDemoRuntimeHost` 在低频启动阶段同步完成本地 Catalog 加载，再把已解析的
  `VisualProfileCatalog` 与只读 UI Sprite 接口注入现有 Coordinator/Canvas。该同步点不进入 30 Hz Tick。
- Simulation/Application 继续只输出稳定 ContentId、PresentationId 与快照，不引用 Unity Object、地址或
  Catalog。新增角色、技能、敌人和资源仍通过目录/Profile 数据扩展，不修改核心程序集。
- Player、普通敌人、Boss、Projectile、Area、Pickup 和 Affix Overlay 优先使用正式 Profile；Status
  Request 可使用正式 Sprite。正式命中失败在 Development 保留程序化 Fallback 和诊断，Release 由
  Project Validation 阻断缺 Catalog、缺地址、缺必需 Alias/Profile 的工程。
- UI 使用正式标题/页面背景、九宫格 Panel、焦点环和鼠标指针；文字仍由 Localization Key 在 View 边界
  解析。高对比模式保留形状身份并为正式实体增加可见轮廓/色调通道。
- Catalog Handle 在 Host 销毁时释放；View/VFX/Map 的对象池生命周期仍归 `PresentationCoordinator`。
- Windows Development Build 显式执行并检查 Addressables Content Build；Player 预处理只复制该次已验证
  输出。内容构建失败必须直接阻断 Player，不得依赖用户级 Editor Preference 或继续产生缺内容的包。

## 依赖方向

```text
Simulation/Core --stable IDs/snapshots--> Application
                                      --> Infrastructure --Addressables handle--> FormalVisualCatalog
                                                              |                         |
                                                              +--> Presentation profiles+
                                                              +--> UI read-only sprites
```

`Game.Core`、`Game.Simulation`、`Game.Application` 的依赖和冻结 API 不变。新增依赖只位于 Unity 适配层
`Game.Infrastructure`。

## 兼容、迁移与回滚

Content Schema 6、存档、30 Hz Tick 与已有稳定 ID 不变。旧调用者继续可省略正式目录参数并使用程序化
Fallback。回滚可移除 Loader 注入和 Catalog Entry；不得让 Simulation 直接持有 Unity 资产来替代。

## 测试

EditMode 覆盖 181 个正式地址、必需 Alias、34 个 Profile、UI 消费与 Handle 释放。PlayMode 覆盖真实
Bootstrap 正式 Catalog/标题背景加载。最终执行全量 EditMode/PlayMode、Project Validation、实际
Addressables Build、1920×1080 标准/高对比截图和 Windows x64 Development Player。
