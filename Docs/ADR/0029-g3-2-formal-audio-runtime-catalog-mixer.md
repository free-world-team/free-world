# ADR 0029：G3.2 正式音频目录、Mixer、优先级路由与句柄边界

- 状态：Accepted
- 日期：2026-08-10
- 决策人：依据用户连续完成 Demo、全权自行决策与逐步骤提交 Push 授权
- 关联里程碑：G3.2、M13
- 承接：ADR 0004、0025、0026、0028

## 背景

九个正式音频批次已形成 104 个通过 provenance、格式与 Addressables 门禁的 Runtime Clip，但 G2.7
仍在运行时生成测试音，并只拥有两个循环 Source。若各个 UI、View 或 Boss 自行加载和播放 Clip，将造成
Addressables Handle 无唯一 Owner、总并发失控、P0 提示被普通命中挤占，以及 Simulation 引用 Unity 对象。

## 决策

- 新增 Addressable `FormalAudioCatalog`，以稳定 `qinglan/audio/...` Key 映射全部 104 个正式 Clip，
  同时提供 Presentation 语义 Cue 和折枝/听风三阶段 Boss Stem 查询。
- `QinglanFormalAudioLoader` 位于 Infrastructure，是 Catalog Addressables Handle 的唯一 Owner。Host 在
  启动低频装配点加载，先关闭 `PresentationCoordinator` 的 Router/Source，再释放 Handle。
- Production Router 固定总容量 32：8 个循环 Stem Source 与 24 个瞬态 Source；瞬态中 8 个容量只留给
  `CriticalDanger`。低优先级满池时丢弃或被 P0 驱逐，P0 满池时合并同类提示。
- 八个 Stem 通道固定为环境 1、探索/弦乐/战斗/高压 4、当前 Boss 三阶段 3。同批 Stem 使用同一个
  `AudioSettings.dspTime` 起点；120 秒开启战斗层，630 秒开启高压层，BossId/Phase 只选择正式地址。
- 同类 Cue 冷却为 40—120 ms；P0 对非 P0 效果应用 0.5011872（-6 dB），Story 对环境应用
  0.6309574（-4 dB）。这些系数与四路用户音量由集中 Router 统一应用。
- `qinglan-demo.mixer` 提供 Gameplay、Paused、Story、Boss 四个实际 `AudioMixerSnapshot`。状态切换使用
  Snapshot，Master/Music/Ambience/Effects 用户音量保留独立乘区，避免状态覆盖设置。
- UI 输入只向 `PresentationCoordinator` 提交语义 Cue，不持有 AudioClip。Simulation/Application 继续
  只输出 ContentId、BossId、Phase 和只读快照，不引用 UnityEngine 音频类型。
- Development 缺正式 Catalog 使用 G2.7 程序化测试音并告警；Project Validation 对缺 Catalog、
  104 地址、语义 Cue、双 Boss Stem、Master 路由或四 Snapshot 的工程执行构建阻断。

## 依赖方向

```text
Simulation/Core --stable IDs/snapshots--> Application
                                      --> Infrastructure --Addressables handle--> FormalAudioCatalog
                                                              |
                                                              +--> Presentation AudioRequestRouter
                                                                      |
                                                                      +--> AudioMixer / pooled AudioSource
```

`Game.Core`、`Game.Simulation` 与 `Game.Application` 的依赖、Content Schema 6、存档和 30 Hz Tick 均不变。

## 兼容、迁移与回滚

旧 `AudioRequestRouter` 构造函数继续可创建测试音回退，既有 G2.7 容量/驱逐测试保持兼容。回滚时可移除
Loader/Catalog 注入并恢复 Development 测试音，但 Release 不得绕过正式 Catalog 门禁，也不得把 Clip
引用写入 Simulation、Content Save 或具体角色分支。

## 测试

EditMode 覆盖 104 地址、17 个语义 Cue、双 Boss 六 Stem、四 Snapshot、32/8/8、Cooldown、-6 dB/-4 dB；
PlayMode 覆盖 Addressables 实际加载、DSP 批量调度、P0 播放和 Handle 释放。最终执行全量 EditMode/
PlayMode、Project Validation、Addressables Build、Windows Development Build 与构建后 Player Smoke。
