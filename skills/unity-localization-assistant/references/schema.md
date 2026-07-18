# Schema contract

Schema v1 is persisted as a Unity `LocalizationSchemaAsset` and normalized through `LocalizationSchemaReader`. Do not treat Unity YAML or an ad-hoc JSON representation as a separate source of truth.

The root contract defines:

- `schemaVersion` (`1` only).
- Assembly-qualified `ScriptableObject` source type.
- `Assets`-relative source folders.
- Stable source `identityPath`.
- Configurable Unity Localization `TableReference`.
- Required locale identifiers.
- A default key template and safe update policy.
- Field targets, validation switches, and optional simple terminology rules.

Each target defines a stable `targetId`, serialized `propertyPath`, optional collection `elementIdPath`, optional target key template, required flag, placeholder contract, and `PreserveExisting`, `FillMissing`, or `Overwrite` policy.

Key template v1 supports only `{sourceId}`, `{targetId}`, and `{elementId}`. Source and target tokens are always required. Collection targets use `[]` in the property path, require `{elementId}` in their effective template, and should provide a stable element identity path. Unknown tokens and malformed braces are errors.

Invalid assets return ordered `LocalizationDiagnostic` records. Agents should use diagnostic `code` and `severity`, not match the English message. Missing collection element identity is a warning that strict CI may later promote to an error.

The full field reference and examples are in the repository's `docs/schema-v1.md`.

Do not encode production table GUIDs, project-specific enum values, proprietary terminology, absolute machine paths, or unstable array indices in the core schema.
