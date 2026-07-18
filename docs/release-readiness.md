# GitHub 发布准备度审查

## 结论

当前仓库适合作为 **pre-alpha / foundation scaffold** 公开，用于展示项目方向、Unity Package Manager 包结构、开放 Agent Skill 和 Codex 适配层的初始设计；它尚不适合作为“可用的本地化工具”发布。

如果本次发布仅公开脚手架，应在 README、GitHub Release 和版本标签中明确说明尚无可运行的编辑器实现、公共 schema 未定稿、示例和验证流程仍待完成。如果发布说明承诺能够扫描项目数据、生成 key、写入 Localization tables 或执行完整校验，则以下阻断项必须先解决。

## 阻断可用 release 的项目

1. **功能闭环尚不存在。** Schema v1、领域模型与稳定诊断已实现，但扫描、key 生成、Localization table 读取、验证和 Apply 服务仍未形成可用闭环。
2. **后续行为测试尚未实现。** Schema 读取、版本、必填字段、身份、模板和领域默认值已有 EditMode 覆盖；仍需覆盖 key 生成、已有 key 复用、重复 key 占用、缺失 locale value、placeholder parity 和写表前 dry-run。
3. **CI 尚未完成首次远端运行。** Unity 2022.3 fixture、失败阻断脚本和 GitHub Actions 工作流已经存在，并已在 2022.3.62f3 中人工和本地 batchmode 通过；仍需配置 Unity license secrets 并取得首次 CI 通过记录。
4. **Sample 仍是骨架示例。** Generic Item Catalog 已可从 Package Manager 导入并编译，Schema v1 文档可以表达该虚构类型，但尚未展示扫描到 String Table 的完整、可复现流程。
5. **尚无可发布的 Git 历史。** 审查时仓库仍没有首个提交，文件均未跟踪，也没有远端配置。发布前需要审阅提交范围、建立干净的初始提交并配置 GitHub 远端。

## 优先改进

### P0：形成最小可用核心

- 定义版本化、与具体游戏数据模型无关的 schema。
- 将扫描、key 生成、已有引用解析和验证实现为可单元测试的纯逻辑层。
- 增加 Unity Localization adapter，通过 Editor API 执行查表、创建 key、写入 locale table、回写 `LocalizedString`、Undo、dirty 和 save。
- Apply 前提供 dry-run 和冲突报告；不得静默覆盖由其他资产占用的 key。
- 校验 required locale 缺表/缺值、空 key、重复 key 归属和 Smart String placeholder parity。
- 增加 Editor tests，并在 CI 中运行。

### P1：完成可复现使用体验

- 提供通用 EditorWindow：选择或扫描资产、预览草稿、逐条启用、人工修订、验证并应用。
- 完成一个只含原创虚构数据的 Sample，覆盖安装、schema 配置、生成 key、写表和验证。
- 扩充根 README 与包 README：支持版本、依赖、UPM Git 安装方式、通用 Agent Skill 使用方式、Codex plugin 安装方式、平台支持矩阵、最小示例、已知限制和故障排查。
- 在包目录加入许可证，并补充 `CONTRIBUTING.md`、`SECURITY.md`、`.gitattributes` 及适当的 issue/PR 模板。
- 为 `package.json` 补充文档、变更日志、许可证和作者/仓库链接；Sample 可用后增加 manifest 中的 samples 声明。

### P2：可选扩展

- 在核心稳定后再设计 CSV import/export。
- 机器翻译应采用可替换 provider 接口；DeepL、OpenAI 等集成需要处理凭证安全、速率限制、重试、成本提示和逐条预览，不应与核心 schema 或写表逻辑耦合。
- 术语规则应来自项目配置，不应把任何生产项目的专用术语硬编码到 package core。

## 版本兼容性判断

`package.json` 声明最低 Unity 版本为 `2022.3`，并依赖 `com.unity.localization` `1.5.9`。只读参考项目的 `ProjectVersion.txt` 显示 Unity `2022.3.62f3`，其 package lock 也解析到 Localization `1.5.9`，因此“该依赖组合明显不兼容”的疑虑已降级，不再单独视为发布阻断。

本仓库的最小 fixture 随后已在 Unity `2022.3.62f3` 中完成自身验证：本地 UPM 包解析和编译无 Console 错误，Generic Item Catalog Sample 导入后编译无错误，EditMode Test Runner 能发现并通过 `PackageSmokeTests.PackageIdentity_IsStable`。这满足里程碑 A 的人工实机验收；正式 release 前仍需取得 CI 运行记录，并在条件允许时补充最低 `2022.3` 初始版本或其他支持版本的矩阵验证。

## 已通过的检查

- 根目录包含完整 MIT License 文本。
- package、plugin 和 Editor assembly definition 的 JSON 语法有效。
- package 与 plugin 的版本号当前一致，均为 `0.1.0`。
- 包内已有 `CHANGELOG.md`，记录了初始 scaffold 版本。
- UPM 包目录包含独立的 `LICENSE.md`。
- Unity 2022.3.62f3 fixture 已成功解析并编译本地包，Console 无错误。
- Generic Item Catalog Sample 已通过 Package Manager 导入并编译，Console 无错误。
- EditMode Test Runner 已发现并通过 package smoke test。
- 已提供本地 batchmode 验证脚本和 GitHub Actions EditMode 工作流；测试失败或零测试会阻断任务。
- Schema v1 Asset、规范化 Definition、领域模型和稳定诊断 code 已实现并记录在 `docs/schema-v1.md`。
- Unity 2022.3.62f3 干净临时 fixture 的 batchmode 导入、编译和 19 个 EditMode tests 已全部通过。
- `.gitignore` 已覆盖主要 Unity 缓存、构建目录和常见 IDE 产物。
- 未发现疑似 API key、访问令牌、私钥或密码。
- 未发现写入文档或源码的本机用户绝对路径。
- 未发现超过 1 MiB 的非 Git 文件。
- 当前仓库未包含参考游戏的生产数据、固定 table GUID、付费 Asset Store 代码或美术资源。

配套 Agent Skill 已通过本地 `quick_validate.py` 校验。Plugin、package 和 asmdef 已通过 JSON 语法解析；正式发布时仍应把开放 Agent Skills 格式校验、Codex plugin manifest 校验和 OpenAI 元数据一致性检查加入可重复执行的发布检查。

## 建议发布门槛

### Scaffold 公开门槛

- README 和 Release Notes 明确标注 pre-alpha / scaffold，以及当前不能完成哪些工作。
- 许可证、manifest、敏感信息和提交范围复核通过。
- 建立首个干净提交，不包含参考项目或第三方商业资产。
- 不创建暗示功能已可用的稳定版 release；可使用源码公开或明确的 pre-release 标签。

### 首个可用 release 门槛

- 完成 P0 最小核心和至少一个端到端 Sample。
- schema、key 生成和验证行为具有自动化测试。
- CI 在最低 Unity 版本上通过编译与 Editor tests。
- 在干净项目中通过 Git URL/本地包安装、导入、扫描、dry-run、写表和重新打开项目后的持久化验证。
- README、包文档、变更日志、包内许可证和升级/兼容说明齐全。
- 共享 Agent Skill 只有一个规范源；至少验证 Codex 适配入口和一个非 Codex 的 skills-compatible 客户端，并记录各目标客户端的发现位置、工具前置条件和不支持能力时的降级行为。
- 完成一次来源与许可证审查，确认核心与 Sample 均不含参考项目的私有标识、生产内容或第三方 Asset Store 材料。

满足 Scaffold 门槛后可以公开仓库；只有满足“首个可用 release 门槛”后，才应把该项目描述为可安装、可执行的 Unity 本地化工具。
