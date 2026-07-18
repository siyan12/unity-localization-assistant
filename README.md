# Unity Localization Assistant

English | [简体中文](README.zh-CN.md)

Unity Localization Assistant is a schema-driven Unity Editor package for projects that store localizable fields in `ScriptableObject` assets. You describe where the source assets live, which fields are localizable, how keys are generated, and which locales are required. The package then scans the project without modifying it and returns deterministic draft entries and validation diagnostics.

It is useful when a project has many data assets—items, characters, quests, abilities, dialogue records, and similar content—and needs one repeatable contract between those assets and Unity Localization String Tables.

> **Current release:** `0.1.0-alpha.1`. Scanning and validation are available as Editor C# APIs. A review window, translation generation, and applying changes to assets or String Tables are not implemented yet.

## What it can do

- Define a reusable mapping from any `ScriptableObject` type to a Unity Localization String Table Collection.
- Scan public or private serialized fields, including nested collection fields such as `bonuses[].description`.
- Generate normalized, deterministic keys from stable source, target, and collection-element identities.
- Inspect existing `LocalizedString` references and locale values without changing project assets or tables.
- Detect duplicate source identities, element identities, and generated keys.
- Report missing or unresolved tables, entries, required locales, and required locale values.
- Validate Smart String syntax and exact placeholder parity.
- Return stable diagnostic codes and ordered results suitable for custom Editor tools, tests, agents, and CI.

The repository also includes a portable [Agent Skill](skills/unity-localization-assistant/SKILL.md) and a Codex plugin adapter. They describe a safe agent workflow around the same schema; they do not replace the Unity package or grant an agent access to Unity by themselves.

## Requirements

- Unity `2022.3` (minimum supported and currently tested Unity line; newer versions are not yet verified)
- Unity Localization `1.5.9` (installed automatically as a package dependency)
- Git installed when adding the package directly from this repository

## Quick start

### 1. Install the Unity package

In Unity, open **Window > Package Manager**, select **+ > Add package from git URL**, and enter:

```text
https://github.com/siyan12/unity-localization-assistant.git?path=/Packages/com.siyan.unity-localization-assistant
```

Alternatively, add the dependency directly to your project's `Packages/manifest.json`:

```json
{
  "dependencies": {
    "com.siyan.unity-localization-assistant": "https://github.com/siyan12/unity-localization-assistant.git?path=/Packages/com.siyan.unity-localization-assistant"
  }
}
```

This installs the current development version from the default branch. The project has not published a stable release or formal prerelease tag yet, so pinning to a release tag is not currently available.

### 2. Prepare Unity Localization

Use Unity's Localization package to create the locales and target String Table Collection for your project. Your source `ScriptableObject` fields that participate in scanning must be serialized `LocalizedString` fields.

For a small fictional data type to experiment with, open the package in Package Manager and import **Samples > Generic Item Catalog**. The sample provides a `ScriptableObject` type only; you still create the sample assets, schema, locales, and String Table Collection in your project.

### 3. Create a schema

In the Project window, choose **Assets > Create > Localization Assistant > Schema**, then configure the asset in the Inspector:

| Field | Example | Purpose |
| --- | --- | --- |
| Source Type | `MyGame.ItemData, MyGame.Runtime` | Assembly-qualified `ScriptableObject` type to scan |
| Source Folders | `Assets/GameData/Items` | Folders searched for source assets |
| Identity Path | `stableId` | Serialized field containing a unique, stable source ID |
| Table Collection | `Items` | Target Unity String Table Collection |
| Required Locales | `en`, `zh-Hans` | Locales that validation requires |
| Key Template | `{sourceId}.{targetId}` | Deterministic key pattern |
| Targets | `name` → `displayName` | Semantic target IDs mapped to serialized property paths |

For collection targets, use `[]` in the property path—for example `bonuses[].description`—and set an element identity path such as `stableId`. See [Schema v1](docs/schema-v1.md) for every field, supported token, and validation rule.

### 4. Scan and validate

The alpha currently exposes APIs rather than a ready-made Editor window. Put the following script in an Editor folder such as `Assets/Editor/ValidateLocalizationSchema.cs`. Select a schema asset and run **Tools > Localization > Validate Selected Schema**.

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

If the script belongs to a custom assembly definition, add a reference to `Siyan.UnityLocalizationAssistant.Editor`. All package APIs are Editor-only and must not be called by runtime/player code.

The scan is a dry run: it does not create keys, update `LocalizedString` references, write translations, or modify String Tables. Use the returned `SchemaScanResult.Entries` to inspect suggested keys and change kinds, and `LocalizationValidationReport.Diagnostics` to decide whether a workflow may proceed.

## Current limitations

- No built-in scan/review UI or one-click menu command.
- No translation provider or AI translation generation.
- No transactional Apply step; source assets and String Tables remain unchanged.
- No standalone JSON schema format; Schema v1 is stored as a Unity `LocalizationSchemaAsset`.
- Agent-client discovery, permissions, and Unity automation differ by platform and must be configured separately.

These boundaries are intentional for the current alpha. The planned Apply workflow is described in [Milestone E: transactional apply](docs/milestone-e-transactional-apply.md).

## Documentation

- [Schema v1](docs/schema-v1.md) — configuration fields, identity rules, and key-template grammar
- [Read-only scanning](docs/scanning.md) — scanner behavior and dry-run guarantees
- [Key generation and validation](docs/key-and-validation.md) — normalization, ownership, locale, and placeholder checks
- [Testing](docs/testing.md) — local Unity and sample verification
- [Architecture and roadmap](docs/architecture-and-roadmap.md) — design direction and planned milestones
- [Versioning](docs/versioning.md) — prerelease progression

## Developing and testing the package

The committed `Tests/UnityProject` fixture imports the package through a local UPM dependency. With Unity 2022.3 and PowerShell 7 or newer installed, run from the repository root:

```powershell
.\scripts\Test-UnityPackage.ps1 -UnityEditorPath 'C:\Program Files\Unity\Hub\Editor\2022.3.62f3\Editor\Unity.exe'
```

You can omit `-UnityEditorPath` when `UNITY_EDITOR_PATH` is set or Unity Hub has a 2022.3 Editor in its default Windows location. The script performs a clean import/compilation pass and then runs all EditMode tests.

## Repository layout

- `Packages/com.siyan.unity-localization-assistant/` — Unity Package Manager package
- `skills/unity-localization-assistant/` — canonical, platform-neutral Agent Skill
- `.codex-plugin/plugin.json` — Codex plugin adapter that distributes the shared skill
- `Tests/UnityProject/` — clean Unity package fixture
- `docs/` — design, behavior, testing, and release documentation

## License

MIT. See [LICENSE](LICENSE).
