# GitHub 发布准备度

## 当前结论

仓库已经公开，里程碑 A–D 已合入 `main`，Unity 2022.3.62f3 的远端
EditMode workflow 已成功运行。当前能力仍是 **alpha 阶段的只读内核**：schema、
扫描、key ownership 和结构化验证可用，但没有 review UI、事务式 Apply 或
`LocalizedString` 写回。

包与 Codex plugin 统一声明 `0.1.0-alpha.1`，但尚未创建 tag 或 GitHub
Release。这是开发版本声明，不是已发布版本。正式 `v0.1.0` 暂不发布。

## 已完成的开源基线

- MIT 根许可证与包内许可证齐全。
- UPM package、Editor/Test/Sample asmdef、开放 Agent Skill 和 Codex adapter
  已建立；package manifest 已声明 Generic Item Catalog Sample。
- Schema v1、确定性只读扫描、key/ownership 和 validation 已实现并有
  EditMode 覆盖。
- Unity 2022.3.62f3 干净 fixture 已完成导入、编译和 47 个 EditMode tests；
  GitHub Actions 的 PR 与 push 检查均成功。
- CI 对缺失 Unity license 提供明确预检，并固定保存失败前的日志与 NUnit
  结果。
- Agent Skill、plugin/package JSON 和 Editor assembly 边界已有本地验证。
- 未发现凭据、私钥、生产游戏数据、固定生产 table GUID、付费 Asset Store
  源码或美术内容。
- 首次开源治理文件、Issue forms、PR template、版本策略和 Milestone E 计划
  已纳入本轮治理工作。

历史验证轮次和复现步骤统一记录在 [`testing.md`](testing.md)，不在本页维护
多个相互冲突的测试计数。

## 当前发布阻断项

1. **事务式 Apply 尚未实现。** 必须完成 stale-state 检测、共享 key/locale
   写入、嵌套引用回写、Undo、dirty/save、回滚或预提交失败保证。
2. **Apply 安全矩阵尚未通过。** 需要覆盖 PreserveExisting、FillMissing、
   显式 Overwrite、幂等性、部分失败和 stale draft。
3. **Editor review workflow 尚未实现。** 用户还不能在通用 UI 中审阅 diff、
   逐条启用并安全 Apply。
4. **Sample 尚未端到端。** Generic Item Catalog 可以导入编译，但尚未展示
   扫描、审阅、写表、引用回写和重开后的持久化闭环。
5. **发布自动化仍需收口。** 正式 prerelease 前仍需重复验证开放 Agent
   Skills 格式、Codex plugin manifest、clean Git URL install、来源/许可证和
   敏感信息检查。

## 预发布策略

详细规则见 [`versioning.md`](versioning.md)：

- Milestones E/F 使用 `0.1.0-alpha.N`；每次真正发布 prerelease 才递增。
- Apply 与 Editor workflow 功能完整后进入 `beta.N`。
- 功能冻结、只修发布阻断项时进入 `rc.N`。
- 只有 Milestone G 的全部门槛通过后才发布稳定 `0.1.0`。

package manifest、Codex plugin manifest、changelog 条目、tag 和 Release 必须
一致；普通合并不创建 tag 或 Release。所有非稳定 GitHub Release 必须标记为
prerelease。

## 下一门槛

### Milestone E — Transactional Apply

按 [`milestone-e-transactional-apply.md`](milestone-e-transactional-apply.md)
先冻结 Apply plan/fingerprint/change report 契约，再实现 Unity Localization
adapter、SerializedProperty 写回、事务边界和安全测试。

### Milestone F — Editor workflow

提供 schema 选择、Scan/Rescan、diff/diagnostic review、逐条启用、错误阻断、
警告确认和诊断定位，并用原创 Sample 完成端到端验证。

### Milestone G — stable v0.1.0

- 在最低支持 Unity 版本的干净项目中完成 Git URL install、Sample、全部
  EditMode tests 和持久化验证。
- README、包文档、限制、故障排查、升级/兼容说明与 changelog 齐全。
- 共享 Agent Skill 只有一个规范源，开放格式与 Codex adapter 校验通过。
- 来源、许可证、安全和敏感信息审查通过。
- 所有公开功能声明均可由 Sample 和自动化测试复现。

在这些条件满足前，项目应明确描述为 alpha/read-only core，而不是完整、
可安全写回资产的 Unity 本地化工具。
