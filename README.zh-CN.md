# Unity Localization Assistant

[English](README.md) | 简体中文

一个由 Schema 驱动的 Unity Localization 编辑器扩展包，同时提供开放的 Agent Skill 和 Codex 插件适配器。

该扩展包旨在帮助项目将 ScriptableObject 字段映射到本地化键和语言表，并验证重复键、缺失语言、术语以及占位符是否一致。

## 仓库结构

- `Packages/com.siyan.unity-localization-assistant/` — Unity Package Manager 扩展包。
- `skills/unity-localization-assistant/` — 规范的、平台无关的 Agent Skill。
- `skills/unity-localization-assistant/agents/openai.yaml` — 可选的 OpenAI/Codex 展示及调用元数据。
- `.codex-plugin/plugin.json` — 用于分发共享 Skill、可安装的 Codex 插件清单。

## Agent 兼容性

配套工作流遵循开放的 Agent Skills `SKILL.md` 格式。其说明和参考资料可供任何兼容 Agent Skills 的客户端复用。自动发现机制和安装位置仍由各客户端决定，因此本仓库将 Codex、Claude、Gemini、Copilot 及其他客户端视为同一份规范 Skill 的轻量适配层，而不是分别维护多份工作流副本。

Skill 本身并不会授予任何能力。Agent 仍需能够访问 Unity 项目，并在相关功能实现后，通过扩展包提供的 Editor、batchmode、CLI 或 MCP 入口执行操作。确定性的扫描、验证、草稿生成和资源应用应由 Unity 扩展包负责；Agent 则负责协调这些服务并解释其结构化结果。

| 使用场景 | 当前状态 | 发现或集成方式 |
| --- | --- | --- |
| 开放 Agent Skills 格式 | 脚手架已通过验证 | 规范文件：`skills/unity-localization-assistant/SKILL.md` |
| Codex | 已包含适配器脚手架 | `.codex-plugin/plugin.json` 用于分发规范 Skill |
| 其他兼容 Agent Skills 的客户端 | 内容可移植，尚未逐一验证客户端 | 按客户端支持的 Skill 位置安装或链接规范 Skill |
| Unity 自动化 | 已支持只读扫描和验证 | `SchemaScanner`、`LocalizationValidationService`、`Tests/UnityProject`、本地 batchmode 脚本及 GitHub Actions EditMode 工作流 |

这里所说的兼容性，是指工作流约定可以移植；并不保证所有 Agent 客户端都能自动发现 Skill，也不保证工具名称或权限完全相同。

## 当前状态

里程碑 A–D 已实现：扩展包现已具备可测试的 Unity 2022.3 脚手架、公开的 Schema v1 约定、确定性的 ScriptableObject 只读扫描、规范化键所有权以及结构化验证报告。资源应用功能仍属于后续里程碑。

扩展包与 Codex 插件清单当前统一使用 `0.1.0-alpha.1`。项目尚未发布稳定版
`v0.1.0`，也尚未创建正式的预发布 Release。预发布推进规则见
[`docs/versioning.md`](docs/versioning.md)，下一阶段实施计划见
[`docs/milestone-e-transactional-apply.md`](docs/milestone-e-transactional-apply.md)。

## 验证扩展包

仓库中提交的测试项目通过本地 UPM `file:` 依赖导入扩展包、进行编译，并通过测试项目清单中的 `testables` 列表公开扩展包测试。

安装 Unity 2022.3 和 PowerShell 7 或更高版本后，在 `pwsh` 中从仓库根目录运行：

```powershell
.\scripts\Test-UnityPackage.ps1 -UnityEditorPath 'C:\Program Files\Unity\Hub\Editor\2022.3.62f3\Editor\Unity.exe'
```

如果已经设置 `UNITY_EDITOR_PATH`，或 Unity Hub 的默认 Windows 目录中存在 `2022.3.x` 版本的 Editor，可以省略 `-UnityEditorPath`。该命令会先执行一次干净的扩展包导入和编译，再运行 EditMode 测试。缺少结果文件、未发现测试、测试失败、编译错误或 Unity 进程返回非零退出码，都会导致脚本失败。

GitHub Actions 使用相同的测试项目和 Unity 2022.3.62f3 运行验证；该版本已完成首次手动导入及 EditMode 验证。在启用工作流前，需要配置仓库的 `UNITY_LICENSE` secret；根据所选 Unity 许可证激活方式，可能还需配置 `UNITY_EMAIL` 和 `UNITY_PASSWORD`。如果缺少 `UNITY_LICENSE`，预检步骤会给出明确的配置错误。测试运行步骤不允许忽略错误，因此编译或测试失败会阻止作业；失败前生成的日志和结果仍会上传到固定的 `artifacts/unity-editmode` 路径。

只读扫描约定及当前限制请参阅 [`docs/scanning.md`](docs/scanning.md)。
键规范化、所有权和验证行为请参阅 [`docs/key-and-validation.md`](docs/key-and-validation.md)。
Unity Editor 和示例导入验证步骤请参阅 [`docs/testing.md`](docs/testing.md)。

## 规划与调研

- [`docs/architecture-and-roadmap.md`](docs/architecture-and-roadmap.md) — 目标架构、Schema 方向、阶段路线图、具体后续开发计划、验收标准及初始 GitHub Issue 划分。
- [`docs/github-landscape.md`](docs/github-landscape.md) — 与相关 Unity 本地化和 AI 翻译项目的对比，以及由此确定的产品定位。
- [`docs/release-readiness.md`](docs/release-readiness.md) — 发布阻塞项、已完成检查、优先级，以及脚手架版本或功能版本的发布门槛。

## 源代码边界

本仓库只能包含原创代码和可再分发的依赖项。可以将 Mimic 作为只读的设计参考，但不得将游戏数据、固定 GUID、专有资源或 Asset Store 源代码复制到本仓库中。

## 许可证

本项目采用 MIT 许可证，详情请参阅 [`LICENSE`](LICENSE)。
