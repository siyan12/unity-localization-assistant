# GitHub 同类项目调研

查询日期：2026-07-18

本文记录与 Unity Localization 资产生成、编辑器本地化工作流、结构化数据导入以及 AI 辅助翻译相关的公开项目。Star 和最近提交日期只是查询当日快照，不能直接代表项目质量、维护承诺或真实用户规模；未能可靠核对的指标不作推断。

## 结论摘要

现有项目大致分为四类：Unity 官方的运行时与表资产基础设施、替代官方包的完整本地化系统、CSV/Google Sheets 等数据导入工具，以及独立的 AI 翻译工具。最接近本项目的单个项目是 Game Translation Agent，但它主要解决 Unity CSV 的翻译与审校；Easy Localization Tool 则接近“扫描内容、生成稳定 key、往返表格”的体验，但使用自己的 JSON 运行时。

本次没有发现一个成熟开源项目同时覆盖以下组合：

- 以声明式 schema 描述任意 ScriptableObject 的本地化字段，包括嵌套数组或结构体字段；
- 通过 Unity Editor API 直接创建和更新官方 Unity Localization String Table；
- 采用确定性的 key 生成、引用回写、预览和验证流程；
- 将同一能力包装为开放、可移植的 Agent Skill，并通过 Codex plugin 等薄适配层分发，同时保持包核心不依赖具体游戏数据模型或 Agent 平台。

因此，本项目不应定位成新的运行时本地化框架，也不应把 CSV 或 Google Sheets 导入本身作为独有卖点。更清晰的定位是：**面向官方 Unity Localization 的 schema 驱动资产生成与验证层，并提供可审计、跨兼容客户端复用的 Agent Skill 工作流。**

## 查询快照

| 项目 | 查询日公开指标 | 活跃度说明 |
| --- | ---: | --- |
| Game Translation Agent | 1 star | 最近提交 2026-03-11 |
| Easy Localization Tool | 4 stars | 最近提交 2025-08-20；仓库规模很小 |
| UniGSC | 7 stars | 最近提交 2023-03-29 |
| i18n-ai-translate | 97 stars | 最近提交 2026-06-28 |
| PolyglotUnity | 332 stars | 最近提交 2021-10-19；不宜视为活跃项目 |
| KNOT Localization | 29 stars | GitHub 页面显示 119 commits、15 tags；本次未记录可靠的最近提交日期 |
| XUnity.AutoTranslator | 3,274 stars | 最近提交 2026-06-20 |

上述 star 来自仓库首页，最近提交日期通过公开 commit feed 核对。数字会持续变化。

## 官方基线：Unity Localization

链接：[Unity Localization 官方手册](https://docs.unity3d.com/6000.0/Documentation/Manual/com.unity.localization.html)

官方包已经提供 String 和 Asset 本地化、Smart Strings、伪本地化，以及 XLIFF、CSV 和 Google Sheets 的导入导出。因此，本项目的价值不在重新实现这些运行时能力或通用表格传输能力。

重叠：

- 本项目最终仍创建和维护官方 String Table、Shared Table Data、Locale 和 LocalizedString 引用。
- 两者都需要处理 Smart String、表条目和多语言内容的一致性。

差异：

- 官方包不知道项目自身的 ScriptableObject 数据模型，也不会根据声明式 schema 自动发现字段、生成 key 并回写引用。
- 官方导入导出面向已有表数据；本项目面向“游戏数据模型到官方本地化资产”的上游生成过程。

可借鉴点：

- 始终使用官方 Editor API 和资产模型，不自行发明平行的运行时格式。
- 将 CSV、XLIFF 和 Google Sheets 视为可组合的下游能力，而不是包核心的必要重新实现。

## Game Translation Agent

链接：[flashwade03/Translate-Agent-For-Unity-Game-Development-With-Gemini](https://github.com/flashwade03/Translate-Agent-For-Unity-Game-Development-With-Gemini#game-translation-agent)

该项目使用 Google ADK 和 Gemini 组织翻译、审校与编排代理，支持 Unity Localization 风格 CSV、术语表、风格指南、占位符保持、Google Sheets、Web 控制台和 CLI。

重叠：

- AI 辅助游戏文本翻译和质量检查。
- 处理 Unity CSV、术语约束和占位符。
- 支持批量工作流与人工审阅。

差异：

- 它以 CSV/Google Sheets 和独立 Web 服务为中心，没有直接解析任意 Unity ScriptableObject schema，也不负责创建官方 Unity Localization 资产和回写 LocalizedString。
- 技术栈绑定 Google ADK/Gemini；本项目的核心生成与验证不应绑定单一模型供应商。

可借鉴点：

- 将翻译与审校分成明确阶段。
- 支持术语表、风格指南、占位符保护、异步进度和变更确认。
- AI 写入前保留可审阅的中间结果。

## Easy Localization Tool

链接：[FoxByte-SRL/Easy-Localization-Tool](https://github.com/FoxByte-SRL/Easy-Localization-Tool#unity-localization-tool)

该工具会扫描 Scene 和 Prefab 中的 TMP 文本、附加带稳定 key 的组件、导出和导入 CSV，并为每种语言生成运行时 JSON。

重叠：

- 自动发现待本地化内容。
- 生成稳定 key，并提供编辑器中的 CSV 往返流程。
- 关注将接近完成的 Unity 项目快速接入本地化。

差异：

- 它使用自己的 LocalizationManager、LocalizedText 和 JSON 数据格式，不以官方 Unity Localization 表为目标。
- 扫描对象主要是 Scene/Prefab 文本；本项目面向由 schema 描述的 ScriptableObject 和复杂嵌套字段。

可借鉴点：

- 扫描、预览、应用的低门槛编辑器流程。
- 稳定 key 和对 multiline、引号、富文本等边界情况的处理。

## UniGSC

链接：[dkoleev/UniGSC](https://github.com/dkoleev/UniGSC#google-sheets-configs-for-unity-game-engine)

UniGSC 从 Google Sheets 读取任意结构的数据，并通过默认或自定义 parser 生成任意结构的 JSON；授权和拉取操作位于 Unity Editor。

重叠：

- 使用声明式配置和可扩展 parser 将外部结构化数据带入 Unity。
- 支持 UPM 安装和编辑器内的数据工作流。

差异：

- 输出是通用 JSON 配置，而不是官方 Localization String/Asset Table。
- 不处理 LocalizedString 引用回写、key 规则、Smart String 合同或本地化完整性验证。

可借鉴点：

- 数据源结构与输出结构解耦。
- 提供默认行为，同时允许项目实现自己的 parser。
- 将凭据和远端访问限制在集成层，避免污染核心 schema。

## i18n-ai-translate

链接：[taahamahdi/i18n-ai-translate](https://github.com/taahamahdi/i18n-ai-translate#why-use-it)

这是面向 i18next 风格 JSON 的多模型翻译工具，支持 ChatGPT、Gemini、Claude、DeepSeek 和本地 Ollama，并提供并行处理、diff-aware 更新、检查模式、dry-run 和 GitHub Action。

重叠：

- AI 翻译、格式保持、占位符校验和批量多语言处理。
- 关注只更新发生变化的文本，并保留已有翻译。

差异：

- 它不了解 Unity 的 StringTableCollection、SharedTableData、LocalizedString、Smart Strings 或 Unity 资产生命周期。
- 它是通用 JSON CLI/CI 工具，不承担 Unity Editor 中的数据发现和引用回写。

可借鉴点：

- diff-aware 增量更新、dry-run、check 模式和 CI 失败条件。
- 上下文注入、多模型选择、速率限制以及占位符和结构验证。
- 明确区分新增、修改、删除和已有翻译保留策略。

## PolyglotUnity

链接：[agens-no/PolyglotUnity](https://github.com/agens-no/PolyglotUnity#current-features)

PolyglotUnity 可下载公共或项目自定义 Google Sheet 的 CSV/TSV，支持多文件解析、fallback language、参数化字符串和 TextMesh Pro。README 所列 Unity 版本主要停留在 Unity 2019 及以前，且仓库最近提交较早。

重叠：

- Google Sheets/CSV 数据流、key-value 本地化和参数化文本。
- 编辑器配置与 Unity UI/TMP 集成。

差异：

- 它是独立的运行时本地化系统，不生成官方 Unity Localization 资产。
- 没有 schema 驱动的 ScriptableObject 字段发现、引用回写或可移植 Agent 工作流。

可借鉴点：

- fallback language、多个数据源覆盖顺序和快速迭代体验。
- 参数化字符串与缺 key 情况需要成为一等验证对象。

## KNOT Localization

链接：[V0odo0/KNOT-Localization](https://github.com/V0odo0/KNOT-Localization)

KNOT 是轻量、可扩展的 Unity 文本与资产本地化系统，提供可定制的数据源、运行时加载、编辑器管理和扩展包。其扩展文档还列出 TextMesh Pro、Addressables、实验性 CSV 导入导出和 OpenAI 自动翻译。

重叠：

- Unity 编辑器工具、文本与资产本地化、可扩展 provider，以及可选 AI 翻译。
- 关注设计师可操作的 key 管理和 Undo/Redo。

差异：

- KNOT 自身是官方 Unity Localization 的替代系统；本项目应建立在官方包上，而不是与其竞争运行时所有权。
- KNOT 管理自己的 collection/provider，不以项目数据 schema 到官方表的生成作为主问题。

可借鉴点：

- provider/addon 架构以及核心不强制依赖 TMP、Addressables 或 AI。
- 编辑器操作必须支持 Undo/Redo，并允许项目替换数据源和加载策略。

## XUnity.AutoTranslator

链接：[bbepis/XUnity.AutoTranslator](https://github.com/bbepis/XUnity.AutoTranslator#introduction)

XUnity.AutoTranslator 面向已经构建的 Unity 游戏，通过 mod loader 或补丁框架在运行时捕获文本并调用多种翻译端点；它也支持手工翻译和第三方 LLM/Ollama 翻译插件。

重叠：

- Unity 文本自动翻译、多翻译端点、缓存和格式保护。
- 对大量未知文本进行自动发现和增量处理。

差异：

- 目标用户主要是游戏翻译/mod 社区，处理的是运行时抓取文本，而不是开发期源资产。
- 它不会生成可提交到版本控制的官方 Localization 表，也不理解项目 schema 或稳定业务 key。

可借鉴点：

- 翻译 provider 插件化、缓存、请求节流、失败熔断和费用风险提示。
- 对未知或动态文本应提供清晰诊断，但本项目不应引入运行时抓取范围。

## 最终产品定位

建议对外表述为：

> Unity Localization Assistant 是一个面向官方 Unity Localization 的 schema 驱动编辑器工具和开放 Agent Skill，并通过 Codex plugin 等平台薄适配层分发。它从项目自己的 ScriptableObject 数据模型中发现待本地化字段，确定性地生成或复用 key，创建和更新 String Table，回写 LocalizedString 引用，并在写入前后验证字段路径、重复 key、缺失语言和 Smart String 占位符。

产品边界：

- 包核心只定义通用 schema、发现、key 生成、写入和验证，不依赖 Mimic 或其他游戏的数据类型。
- 游戏专属字段、术语、文案模板和 Smart String 参数语义放在 Samples 或项目集成层。
- Agent 客户端用于理解项目上下文、生成草稿和编排工作流；确定性的资产修改与验证仍由 Unity Editor 代码执行，模型或客户端不是核心依赖。
- 不替代 Unity Localization 的运行时、Locale、Addressables 或表导入导出能力。
- 不把某个模型供应商、在线服务或 Google Sheets 设为必需依赖。

优先借鉴的横向能力：

1. i18n-ai-translate 的 dry-run、diff-aware、check/CI 和占位符校验。
2. Game Translation Agent 的术语表、风格指南、翻译与审校分阶段。
3. Easy Localization Tool 的扫描—预览—应用编辑器体验和稳定 key。
4. KNOT 的 provider/addon 边界与 Undo/Redo。
5. XUnity.AutoTranslator 的 provider、缓存、节流和失败保护，但保持为可选 AI 扩展。
