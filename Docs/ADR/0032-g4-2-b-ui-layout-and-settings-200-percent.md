# ADR 0032：G4.2-B 页面专属构图与 Settings 3 的 200% 字号兼容扩展

- 状态：Accepted
- 日期：2026-08-23
- 决策人：依据用户要求自主完成 G4.2 全里程碑，不等待阶段性人工审核
- 关联里程碑：G4.2-B
- 承接：ADR 0024、0030、0031

## 背景

G4.2-A 已建立青瓷剑境令牌、紧凑 HUD、三卡选择和中央战场样板，但标题、角色、地图、构筑、设置、
叙事和 Hub 仍主要复用同一纵向选项列表。G4.2 主计划要求这些页面形成各自的信息层级，并要求
150%/200% 字号在 720p、1080p、4K 和 21:9 下不裁切。

Settings Save Schema 3 原先只接受 1.0、1.25、1.5 范围。把上限扩展到 2.0 会改变存档字段的有效值域，
因此必须记录兼容、迁移和回滚决定；不能只修改运行时夹取范围。

## 决策

### 1. 保持单一 UI 真值与命令入口

- 继续使用一个 `QinglanRuntimeUiRoot`、一个 `QinglanDemoPresenter` 和 `QinglanUiCommand` 路径。
- 页面差异只存在于 UI 私有布局策略，不增加第二套页面状态、选择索引或玩法数据。
- 鼠标、键盘和手柄继续调用同一 Option/Presenter 命令，不为视觉改造复制业务处理。

### 2. 页面专属构图

- 标题使用窄主行动区和独立长文本安全区。
- 角色、构筑、设施和收藏使用“左列表、右展示”；地图使用“左展示、右列表”。
- 设置使用列表与实时预览分栏；升级/奖励保持三卡；叙事/结算使用居中阅读区；Hub 使用设施展示构图。
- 角色、地图和构筑展示区只消费现有正式图标目录与本地化 Key，不引入 Unity Object 到 Application。
- 操作卡片增加语义色侧标，但选中、禁用和危险仍保留焦点形状、文字和明度通道，颜色不是唯一信息源。

### 3. Settings 3 扩展到 200%

- `fontScale` 的有效范围从 `[1.0, 1.5]` 放宽到 `[1.0, 2.0]`，步长仍为 0.25。
- Settings Schema 版本保持 3：旧 Settings 3 数据全部仍有效，wire 字段、字段顺序和默认值不变。
- 新客户端可以保存 1.75 或 2.0；旧客户端读取这类新值会拒绝，因此这是向前不兼容、向后兼容的值域扩展。
- 不静默把 2.0 降为 1.5；读取超出 `[1.0, 2.0]` 的值仍失败，避免掩盖损坏或未来版本数据。

### 4. 自动证据

- EditMode 检查各页面锚点、展示区、200% 字号和卡片高度。
- PlayMode 继续覆盖 zh-Hans、en、Pseudo 与 150% 无溢出。
- G4.2 Player 验收新增 200% 截图、实际分辨率校验和 `HasAnyTextOverflow` 硬门禁。
- 分辨率矩阵使用独立结果和截图目录，禁止用单张 1080p 截图外推所有比例。

## 依赖方向

```text
Application settings/page snapshot
              |
              v
UI private layout strategy + formal visual catalog
              |
              v
single Canvas / shared Presenter command path
```

不改变 `Game.Core`、`Game.Content.Runtime`、`Game.Simulation` 的依赖方向，不引入第三方运行时包。

## 兼容、迁移与回滚

- 旧 Settings 1/2 继续按既有迁移链进入 Settings 3，默认字号仍为 1.0。
- 旧 Settings 3 的 1.0/1.25/1.5 不需要重写。
- 若回滚到只支持 150% 的旧客户端，应先在新客户端把字号改回不高于 1.5；不得依赖旧客户端夹取。
- 如未来需要新增断点式布局数据或改变 wire，必须提升 Settings Schema 并新增迁移，而不是继续扩大隐式解释。

## 被拒绝的方案

- 每个页面建立独立 Presenter：会复制状态和输入路径。
- 只把统一菜单放大到 200%：会保留信息架构问题并产生裁切。
- 升级到 Settings 4 但不改变 wire：会制造无必要迁移和兼容成本。
- 导入来源不明 UI 包：违反 provenance、许可证和第三方审批规则。

## 测试

- 全量 EditMode 与 PlayMode。
- Windows x64 Release Candidate Build。
- 720p、1080p、4K、21:9 Player 矩阵；每次覆盖 150%/200%、色觉、高对比、Reduce Motion 和零文本溢出。
- 独立人类视觉签字仍单独记录为 `NOT RUN`，不得由自动门禁替代。
