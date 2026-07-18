# Localization Schema v1

## Purpose and storage

Schema v1 describes how arbitrary project `ScriptableObject` assets map to Unity Localization String Table entries. The canonical persisted form is a `LocalizationSchemaAsset`, created from **Assets > Create > Localization Assistant > Schema**.

The schema is Editor-only. It configures scanning and asset generation and is not a replacement for Unity Localization runtime types. Version 1 does not define a standalone hand-authored JSON format; automation should read the schema asset through `LocalizationSchemaReader` so Unity serialization and validation use one contract.

## Root fields

| Field | Required | Meaning |
| --- | --- | --- |
| `schemaVersion` | Yes | Must be `1`. Missing or unsupported versions are errors and are never silently upgraded. |
| `sourceType` | Yes | Assembly-qualified `ScriptableObject` type, for example `Example.CatalogItem, Example.Catalog`. |
| `sourceFolders` | Yes | One or more `Assets`-relative folders. Backslashes, glob patterns, and fixed external paths are not supported. |
| `identityPath` | Yes | Serialized property path containing the stable identity of each source asset. |
| `tableCollection` | Yes | Unity Localization `TableReference` for the target String Table Collection. Prefer a GUID-backed Inspector selection. |
| `requiredLocales` | Yes | Locale identifiers whose tables and values will be required by validation. |
| `keyTemplate` | Yes | Default key template. The safe default is `{sourceId}.{targetId}`. |
| `defaultUpdatePolicy` | Yes | Default non-destructive write policy for new configuration. |
| `targets` | Yes | One or more field mappings. |
| `validationRules` | Yes | Stable identity, locale-value, and placeholder-parity switches. |
| `terminologyRules` | No | Simple source term, target locale, and expected term records. No expression DSL is supported. |

The reader trims values, removes duplicate normalized folder/locale values, and sorts normalized root collections deterministically. Duplicate required locales and target IDs produce diagnostics before normalization.

## Targets

Each `LocalizationSchemaTarget` contains:

| Field | Required | Meaning |
| --- | --- | --- |
| `targetId` | Yes | Stable semantic identity using letters, digits, `.`, `_`, or `-`. It must be unique within the schema. |
| `propertyPath` | Yes | Serialized field path, such as `displayName` or `bonuses[].description`. Use `[]` for a collection; fixed indices are invalid. |
| `elementIdPath` | Collection targets | Stable identity field relative to each collection element, such as `stableId`. Missing identity produces a warning for later strict-CI escalation. |
| `keyTemplate` | No | Target-specific template. Empty values inherit the root template. |
| `required` | Yes | Whether the source field and translations are required. |
| `placeholderContract` | No | Allowed placeholder names. Empty or duplicate names are invalid. |
| `updatePolicy` | Yes | `PreserveExisting`, `FillMissing`, or `Overwrite`. |

## Identity contract

Three identities are intentionally separate:

- Source identity comes from `identityPath` and must be non-empty and unique across scanned source assets.
- Target identity comes from `targetId` and must be unique within a schema.
- Element identity comes from `elementIdPath` and must be non-empty and unique within a collection.

Schema v1 records these rules. Asset-level uniqueness checks are implemented by later scanning and validation milestones. Asset paths and array indices are not stable business identities.

## Key template grammar

Version 1 supports exactly three case-sensitive tokens:

- `{sourceId}`
- `{targetId}`
- `{elementId}`

Every template must contain `{sourceId}` and `{targetId}`. Collection targets must also contain `{elementId}`. Non-collection targets must not contain `{elementId}`. Unknown tokens, empty tokens, and unbalanced braces are errors.

Examples:

```text
{sourceId}.{targetId}
{sourceId}.{targetId}.{elementId}
```

Token replacement, character normalization, aliases, rename previews, and collision policy belong to the key-service milestone and are not implemented by the schema reader.

## Update policies

- `PreserveExisting`: do not replace an existing non-empty translation.
- `FillMissing`: write only missing locale values.
- `Overwrite`: replace existing values after the change is shown in a reviewable diff.

Schema v1 only records the policy. Apply behavior is implemented in a later milestone. `Overwrite` must never bypass preview or validation.

## Diagnostics

`LocalizationSchemaReader.Read` never uses invalid user configuration as an unhandled control-flow exception. It returns a `SchemaReadResult` containing a normalized definition and ordered diagnostics. `IsValid` is false when any diagnostic has `Error` severity.

Diagnostic codes are stable machine-readable strings declared by `SchemaDiagnosticCodes`, including:

```text
ULA_SCHEMA_VERSION_UNSUPPORTED
ULA_SCHEMA_SOURCE_TYPE_INVALID
ULA_SCHEMA_TARGET_ID_DUPLICATE
ULA_SCHEMA_KEY_TEMPLATE_UNKNOWN_TOKEN
ULA_SCHEMA_KEY_TEMPLATE_ELEMENT_TOKEN_REQUIRED
ULA_SCHEMA_ELEMENT_ID_PATH_REQUIRED
```

Agents and CI should branch on `code` and `severity`, not the English message text.

## Generic Item Catalog example

A schema for the fictional Sample can use:

```text
sourceType: Siyan.UnityLocalizationAssistant.Samples.GenericItemCatalog.GenericCatalogItem,
            Siyan.UnityLocalizationAssistant.Samples.GenericItemCatalog
sourceFolders: Assets/Samples/Unity Localization Assistant/<package-version>/Generic Item Catalog/Data
identityPath: stableId
requiredLocales: en, zh-Hans
target name: propertyPath=displayName, keyTemplate={sourceId}.{targetId}
target description: propertyPath=description, keyTemplate={sourceId}.{targetId}
```

The user selects or creates the target String Table Collection locally. The package does not ship a production table GUID, locale asset, translation, or project-specific identifier.
