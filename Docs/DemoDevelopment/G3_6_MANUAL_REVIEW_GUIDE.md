# G3.6 人工与法律评审操作指南

本指南只关闭 `29_G3_6_RELEASE_CANDIDATE.md` 第 7 节的外部人工/法律门禁。自动测试、Codex 审阅、
用户概括授权或空白签字不能替代具名人类 Reviewer 的实际结论。

## 1. 生成锁定证据表

在候选仓库运行：

```powershell
./Scripts/prepare-qinglan-g36-manual-review.ps1 `
  -CandidateCommit qinglan-demo-g3.6-rc2 `
  -CandidateTag qinglan-demo-g3.6-rc2
```

输出为 `TestResults/QinglanDemo/G3.6/External/manual-review.json`。脚本锁定 RC2 Commit/Tag，并为视觉、
构筑、音频和权利证据写入 SHA-256。若文件已存在，脚本拒绝覆盖；只有确认尚无人编辑时才可显式使用
`-Force`。

## 2. 人类 Reviewer 实际评审

Reviewer 只填写以下字段，不得修改候选 Commit、Tag、证据路径、证据 Hash 或声明正文：

- 每项 `status`：只能为 `PASS` 或 `FAIL`；
- `reviewer.name`、`reviewer.role`、`reviewedAtUtc` 和非空 `notes`；
- 权利项四个检查：商业权利、Steam AI 披露、Noto OFL 随包义务、商店法律文本；
- 顶层 `status`：任一失败为 `FAIL`，全部通过为 `PASS`；
- `attestation.signed=true`、具名人类 `signerName` 和 `signedAtUtc`。

视觉/叙事、三构筑差异、目标声卡遮蔽可以由体验 Reviewer 执行；权利与商店文本应由有相应责任的法律/
合规 Reviewer 执行。不同项目可填写不同 Reviewer，最终签字人对整份结论负责。

## 3. 校验并安装证据

```powershell
./Scripts/validate-qinglan-g36-manual-review.ps1 `
  -CandidateCommit qinglan-demo-g3.6-rc2 `
  -InstallEvidenceRoot TestResults/QinglanDemo/G3.6/Candidate
```

校验器会重新计算所有证据 Hash，验证 Reviewer 类型、姓名、角色、时间、四项权利检查、顶层状态和声明。
只有 `PASS` 才会把原始评审及其验证记录安装为：

- `manual-review.json`
- `manual-review-validation.json`

退出码：`0=PASS`、`1=FAIL`、`2=NOT_RUN`。不得手工伪造验证文件；候选汇总会检查验证文件中的来源 Hash。

## 4. 失败处理

- 任一评审为 `FAIL`：保留文件和说明，修复实际问题后重新生成候选并完整复测；不得只改状态。
- 任一评审未做：保持 `NOT_RUN`，G3.6 继续 `NO-GO / INCOMPLETE`。
- 证据 Hash 变化：说明候选证据已被修改，必须重新生成锁定证据表并重新评审。
