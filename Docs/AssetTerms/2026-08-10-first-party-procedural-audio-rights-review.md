# FirstParty 程序化音频权利审查（2026-08-10）

- 审查日期：2026-08-10
- 权利依据：`repository://AGENTS.md`
- 适用范围：G3.2 中由本仓库脚本从零确定性合成并指向本记录的
  `Assets/GameAssets/FirstParty/QinglanDemo/AUDIO-*/` 音频
- Owner：Qinglan Demo Audio Owner
- 审核人：Codex（技术、创意、权利）

## 审查结论

G3.2 正式音频只使用数学振荡器、确定性噪声、包络、滤波/混音和仓库内原创节奏/音高参数，从零生成
48 kHz/24-bit WAV Master 与派生 Runtime 文件。流程不读取或采样唱片、影视、游戏、样本包、现场录音、
人声、商标声、参考项目或来源不明音频；不使用生成式音频模型，也不上传任何外部参考输入。

Python、NumPy 与 libsndfile/soundfile 仅作为本地制作工具，不随游戏 Runtime 分发；其生成文件本身是
项目第一方输出。本记录不替代制作工具自身的软件许可证，且不授权未来加入第三方采样或模型输出。

满足以下条件的批次批准用于 Windows x64 / Steam 商业游戏运行时、商店/营销录屏与内部测试：

1. 保存生成脚本、规格、固定 Seed、Master/Runtime Hash 和实际 QA 指标；
2. Master 为 48 kHz/24-bit WAV，Runtime 格式、长度、峰值、RMS、DC、循环接缝和并发预算通过；
3. `source/` 不进入 Addressables，只有 `final/` 使用 `QinglanDemo-Audio` 和三个正式标签；
4. 技术、创意与权利审核均完成，状态为 `approved-for-release`；
5. 文件、脚本或参数变化会触发 Hash 失配并要求重新审核。

若未来加入外部录音、样本、音乐片段、人声、第三方生成模型或参考音频，本记录立即失效，必须另行登记
来源、许可证、输入权利、平台用途和 Release Owner 审查。
