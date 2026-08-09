# 25 G3.2 正式音频、混音与 Addressables

- 状态：`IN PROGRESS — GOVERNANCE PASS；1 / 9 AUDIO 批次`
- 日期：2026-08-10
- 输入：G3.1 正式视觉、G0.4 Manifest、M13、M15、M16、ADR 0004/0025/0026
- 非范围：G3.3 字体/正文、G3.4 平衡、G3.5 目标硬件性能、G3.6 Release

## 1. 目标与顺序

按 Manifest 的 9 个 AUDIO 行依次完成：AUDIO-AMB-001 → AUDIO-MUSIC-001 → AUDIO-BOSS-001 →
AUDIO-PLAYER-001 → AUDIO-WEAPON-001 → AUDIO-ENEMY-001 → AUDIO-AFFIX-001 → AUDIO-MAP-001 →
AUDIO-UI-001。每行是独立生产、QA、提交和 Push 边界。

全部音频采用第一方确定性程序化合成，不使用外部录音、样本包、人声或生成式音频模型。源文件统一为
48 kHz/24-bit WAV；长循环派生 OGG/Vorbis Streaming，普通短音效派生 OGG/Vorbis Clip，UI 极短音效
派生 48 kHz/16-bit WAV/PCM。

## 2. Governance 门禁

- `audio.release` 必须同时带 `release`、`pack.qinglan_demo`，且只进入 `QinglanDemo-Audio`。
- `visual.release` 与 `audio.release` 不得出现在同一 Entry；source/prompt/spec/provenance 不可寻址。
- AudioImporter 固定 Preserve 48 kHz；Ambience/Music/Boss 使用 Streaming，普通 SFX 使用
  Compressed In Memory，UI 使用 Decompress On Load PCM。
- 每批保存脚本/参数/Seed、源/输出 Hash、长度、声道、Peak/RMS、DC、循环接缝与权利审核。
- Development 缺正式音频可诊断使用 G2.7 Test Tone；最终 G3.2 Integration 后 Release 缺目录/地址阻断。

## 3. Git 容量评审

G3.2 开始时 `Assets/GameAssets` 为 477,068,081 Bytes，`.git` 为 476,914,004 Bytes。长音频若使用
48 kHz/24-bit Stereo，120 秒单文件约 34.6 MB，会超过 G0.4 的 25 MiB 单文件建议并显著放大仓库。

本工作包批准以下容量策略，不启用 Git LFS：

- Ambience/Music/Boss Master 使用 Mono 48 kHz/24-bit WAV；空间宽度由多个环境层、AudioSource 路由和
  Stem 组合形成。最长 120 秒 Master 约 17.3 MB，单文件低于 25 MiB。
- 长 Runtime 文件为 OGG/Vorbis；短音效按允许格式使用 OGG 或 PCM WAV。
- AUDIO-MUSIC-001 与 AUDIO-BOSS-001 的单批总量预计超过 50 MiB，但每个文件均低于 GitHub 单文件
  限制；用户已明确授权提交所有 source/working/final/provenance。仓库为专用项目且当前未配置 LFS，
  因此本次直接 Git 审批通过，风险是 clone 体积继续增长。
- 不用压缩归档隐藏源文件；每个 WAV 保持直接可审计。若实测任一文件 ≥25 MiB 或 G3.2 增量超过
  260 MiB，停止该批并重新评审 LFS/时长/声道策略。

## 4. 批次台账

| 顺序 | 批次 | 状态 | 最低数量 |
|---:|---|---|---:|
| 0 | G3.2 Governance | PASS | Validator、Importer、合成/QA 基础与容量评审 |
| 1 | AUDIO-AMB-001 | PASS | 6 条 45 秒旧庭院环境循环；PCM24 Master + OGG Streaming |
| 2 | AUDIO-MUSIC-001 | PENDING | 4 |
| 3 | AUDIO-BOSS-001 | PENDING | 6 |
| 4 | AUDIO-PLAYER-001 | PENDING | 12 |
| 5 | AUDIO-WEAPON-001 | PENDING | 18 |
| 6 | AUDIO-ENEMY-001 | PENDING | 24 |
| 7 | AUDIO-AFFIX-001 | PENDING | 8 |
| 8 | AUDIO-MAP-001 | PENDING | 14 |
| 9 | AUDIO-UI-001 | PENDING | 12 |
| 10 | Final Integration | PENDING | Catalog、Mixer/Router、全量门禁与 Player Smoke |

## 5. 测试

- Governance Focused EditMode：正确 Audio Route、错 Group、混合类别、source 入组、缺 release 负向测试。
- 每批：Python 两次确定性生成、格式/时长/Peak/RMS/DC/接缝 QA、Unity 导入、Focused EditMode、
  Project Validation。
- 最终：完整 EditMode/PlayMode、实际 Addressables Build、32 Source/8 P0、-6 dB Duck、Stem/Snapshot、
  Windows x64 Development Build 与构建后 Player Smoke。
