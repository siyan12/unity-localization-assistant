# Unity Localization Assistant

[English](README.md) | 简体中文

Unity Localization Assistant 是一个由 Schema 驱动的 Unity Editor 扩展包，适用于使用 `ScriptableObject` 保存待本地化字段的项目。你可以通过 Schema 描述源资源的位置、需要本地化的字段、键生成规则和必需语言；扩展包会在不修改任何资源的前提下扫描项目，并返回确定性的草稿条目和验证诊断。

当项目中有大量物品、角色、任务、技能、对话等数据资源，需要在这些资源与 Unity Localization String Table 之间建立一套可重复执行的约定时，它尤其有用。

> **当前开发版本：** `0.1.0-alpha.1`。项目尚未创建 tag 或 GitHub Release。扫描和验证能力已作为 Editor C# API 提供；审阅窗口、翻译生成以及将变更写回资源或 String Table 的功能尚未实现。

## 它能做什么

- 将任意 `ScriptableObject` 类型映射到 Unity Localization String Table Collection。
- 扫描公开或私有的序列化字段，包括 `bonuses[].description` 这样的嵌套集合字段。
- 根据稳定的源、目标和集合元素标识生成规范化、确定性的本地化键。
- 读取现有 `LocalizedString` 引用和各语言值，但不修改项目资源或语言表。
- 检测重复的源标识、元素标识和生成键。
- 报告缺失或无法解析的语言表、条目、必需语言及必需语言值。
- 验证 Smart String 语法和占位符是否严格一致。
- 返回稳定的诊断代码和有序结果，便于自定义 Editor 工具、测试、Agent 和 CI 使用。

仓库还包含一个可移植的 [Agent Skill](skills/unity-localization-assistant/SKILL.md) 和 Codex 插件适配器。它们描述了围绕同一 Schema 的安全 Agent 工作流；它们不能替代 Unity 扩展包，也不会自行授予 Agent 访问 Unity 的能力。

## 环境要求

- Unity `2022.3`（最低支持且当前完成验证的 Unity 版本线；更新版本尚未验证）
- Unity Localization `1.5.9`（会作为扩展包依赖自动安装）
- 通过 Git URL 安装时，需要本机已安装 Git

## 快速上手

### 1. 安装 Unity 扩展包

在 Unity 中打开 **Window > Package Manager**，选择 **+ > Add package from git URL**，然后输入：

```text
https://github.com/siyan12/unity-localization-assistant.git?path=/Packages/com.siyan.unity-localization-assistant
```

也可以直接在项目的 `Packages/manifest.json` 中加入依赖：

```json
{
  "dependencies": {
    "com.siyan.unity-localization-assistant": "https://github.com/siyan12/unity-localization-assistant.git?path=/Packages/com.siyan.unity-localization-assistant"
  }
}
```

以上方式会安装默认分支上的当前开发版本。项目尚未发布稳定版或正式的预发布标签，因此目前还不能固定到某个 Release 标签。

### 2. 准备 Unity Localization

使用 Unity Localization 扩展包创建项目需要的 Locale 和目标 String Table Collection。参与扫描的源 `ScriptableObject` 字段必须是已序列化的 `LocalizedString` 字段。

如果想先用一个简单的虚构数据类型试用，可以在 Package Manager 中打开本扩展包，并导入 **Samples > Generic Item Catalog**。该示例只提供一个 `ScriptableObject` 类型；示例资源、Schema、Locale 和 String Table Collection 仍需在自己的项目中创建。

### 3. 创建 Schema

在 Project 窗口中选择 **Assets > Create > Localization Assistant > Schema**，然后在 Inspector 中配置：

| 字段 | 示例 | 用途 |
| --- | --- | --- |
| Source Type | `MyGame.ItemData, MyGame.Runtime` | 要扫描的 `ScriptableObject` 程序集限定类型名 |
| Source Folders | `Assets/GameData/Items` | 搜索源资源的目录 |
| Identity Path | `stableId` | 保存唯一、稳定源标识的序列化字段 |
| Table Collection | `Items` | 目标 Unity String Table Collection |
| Required Locales | `en`、`zh-Hans` | 验证时必须存在的语言 |
| Key Template | `{sourceId}.{targetId}` | 确定性的键格式 |
| Targets | `name` → `displayName` | 语义目标 ID 到序列化属性路径的映射 |

集合目标需要在属性路径中使用 `[]`，例如 `bonuses[].description`，并设置 `stableId` 之类的元素标识路径。所有字段、支持的模板标记和验证规则见 [Schema v1](docs/schema-v1.md)。

### 4. 扫描并验证

当前 Alpha 版提供的是 API，还没有现成的 Editor 窗口。将以下脚本放入 `Assets/Editor/ValidateLocalizationSchema.cs` 等 Editor 目录中，选中一个 Schema 资源，然后执行 **Tools > Localization > Validate Selected Schema**。

```csharp
using Siyan.UnityLocalizationAssistant.Editor;
using UnityEditor;
using UnityEngine;

public static class ValidateLocalizationSchema
{
    [MenuItem("Tools/Localization/Validate Selected Schema")]
    private static void ValidateSelectedSchema()
    {
        var schemaAsset = Selection.activeObject as LocalizationSchemaAsset;
        if (schemaAsset == null)
        {
            Debug.LogError("Select a LocalizationSchemaAsset first.");
            return;
        }

        var readResult = LocalizationSchemaReader.Read(schemaAsset);
        foreach (var diagnostic in readResult.Diagnostics)
            Debug.Log($"{diagnostic.Severity} [{diagnostic.Code}] {diagnostic.Message}", schemaAsset);

        if (!readResult.IsValid)
            return;

        var scanResult = new SchemaScanner().Scan(readResult.Schema);
        var report = new LocalizationValidationService().Validate(readResult.Schema, scanResult);

        foreach (var diagnostic in report.Diagnostics)
            Debug.Log($"{diagnostic.Severity} [{diagnostic.Code}] {diagnostic.Message}", schemaAsset);

        Debug.Log(
            $"Localization scan: {scanResult.Entries.Count} entries, " +
            $"{report.ErrorCount} errors, {report.WarningCount} warnings.",
            schemaAsset);
    }
}
```

如果该脚本位于自定义程序集定义中，请添加对 `Siyan.UnityLocalizationAssistant.Editor` 的引用。扩展包的 API 全部仅限 Editor 使用，不能由运行时或 Player 代码调用。

扫描过程是一次 Dry Run：它不会创建键、更新 `LocalizedString` 引用、写入译文或修改 String Table。可以通过 `SchemaScanResult.Entries` 查看建议键和变更类型，并通过 `LocalizationValidationReport.Diagnostics` 判断工作流能否继续。

## 当前限制

- 没有内置的扫描/审阅界面或一键执行菜单。
- 没有翻译服务或 AI 翻译生成功能。
- 没有事务化 Apply 步骤；源资源和 String Table 不会被修改。
- 没有独立的 JSON Schema 格式；Schema v1 以 Unity `LocalizationSchemaAsset` 保存。
- 不同 Agent 客户端的 Skill 发现方式、权限和 Unity 自动化能力不同，需要分别配置。

这些是当前 Alpha 阶段的明确边界。计划中的写回流程见 [里程碑 E：事务化 Apply](docs/milestone-e-transactional-apply.md)。

## 文档

- [Schema v1](docs/schema-v1.md) — 配置字段、标识规则和键模板语法
- [只读扫描](docs/scanning.md) — 扫描行为及 Dry Run 保证
- [键生成与验证](docs/key-and-validation.md) — 规范化、所有权、语言和占位符检查
- [测试](docs/testing.md) — 本地 Unity 和示例验证方法
- [架构与路线图](docs/architecture-and-roadmap.md) — 设计方向和计划中的里程碑
- [版本规则](docs/versioning.md) — 预发布版本推进规则

## 开发与测试扩展包

仓库中的 `Tests/UnityProject` 测试项目通过本地 UPM 依赖导入扩展包。安装 Unity 2022.3 和 PowerShell 7 或更高版本后，在仓库根目录运行：

```powershell
.\scripts\Test-UnityPackage.ps1 -UnityEditorPath 'C:\Program Files\Unity\Hub\Editor\2022.3.62f3\Editor\Unity.exe'
```

如果已经设置 `UNITY_EDITOR_PATH`，或 Unity Hub 默认 Windows 目录中存在 Unity 2022.3，可以省略 `-UnityEditorPath`。脚本会先执行一次干净的导入和编译，再运行全部 EditMode 测试。

## 仓库结构

- `Packages/com.siyan.unity-localization-assistant/` — Unity Package Manager 扩展包
- `skills/unity-localization-assistant/` — 规范的、平台无关的 Agent Skill
- `.codex-plugin/plugin.json` — 用于分发共享 Skill 的 Codex 插件适配器
- `Tests/UnityProject/` — 干净的 Unity 扩展包测试项目
- `docs/` — 设计、行为、测试和发布文档

## 许可证

本项目采用 MIT 许可证，详情见 [LICENSE](LICENSE)。
