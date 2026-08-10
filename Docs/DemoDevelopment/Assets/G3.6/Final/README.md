# G3.6 RC2 最终证据

- 候选 Commit：`9984bcc5582bd827372768993953645d754cc463`
- 候选 Tag：`qinglan-demo-g3.6-rc2`
- 自动化结果：`PASS`
- 发布决定：`NO-GO / INCOMPLETE`
- GitHub Actions：<https://github.com/free-world-team/free-world/actions/runs/31370035860>（`success`）

## 自动化门禁

EditMode 460/460、PlayMode 20/20、Project Validation、G2.8 Vertical Slice、G3.4 Balance、
54,000 Tick CPU、G3.6 Compliance、Windows Development、Windows Release、Release Player、
干净 Checkout、源树清洁和 Actions 产物上传均为 `PASS`。Release Player 为非 Development、
Null Platform，只装载一个正式 `qinglan.pack.demo` Pack（193 Definitions），无 Placeholder 或未批准资产。

## 未关闭门禁

- 独立人工视听/构筑差异评审、商业权利与 Steam AI 披露法律签字：`NOT_RUN`。
- 4 核、8 GB、DX11、2 GB VRAM 最低规格物理机器认证：`NOT_RUN`。

因此 DOD-01—08 为 `PASS`，DOD-09—10 为 `NOT_RUN`。不得把 Codex 客观证据准备、用户概括授权，
或 i7-12700F / RTX 3060 Ti 参考目标机结果改写成人工签字或最低规格实机结论。

## 文件

- `release-candidate-summary.json`：最终 DOD 与 `NO-GO` 决定。
- `ci-summary.json`：成功 Run 与首次网络 Checkout 失败历史。
- `release-build-manifest.json`、`release-player.json`：同一 RC2 Commit 的 Release/Player 证据。
- `clean-clone-summary.json`、`compliance.json`：干净 Checkout 和合规结果。
- `cpu-target.json`、`target-player.json`：CPU 与 1080p 30 分钟目标机证据。
- `vertical-slice.json`、`balance-freeze.json`：完整切片与 15 局平衡矩阵。
- `audio-masking-review.json`：客观遮蔽压力分析，不等同于人类目标声卡试听。
- `manual-review.json`、`manual-review-validation.json`：Schema 2 人工/法律评审表与当前 `NOT_RUN` Hash 配对。
- `minimum-spec-review.json`、`minimum-spec-validation.json`：Schema 2 实体机认证表与当前 `NOT_RUN` Hash 配对。

两组 Validation 的存在不代表评审已运行；其作用是锁定当前候选、Schema 和 Review Hash，阻止仅修改顶层
状态绕过门禁。真实结果必须按对应指南重新生成并以 `PASS` 替换。

2,000 Enemy 扩展压力仍为已知非阻断 `FAIL`；最低规格 `NOT_RUN` 是本次 Release 阻断项。
