# Read-only scanning

`SchemaScanner` converts a normalized `LocalizationSchemaDefinition` into a deterministic `SchemaScanResult`. The result contains `LocalizationDraftEntry` items and stable diagnostics; it does not apply changes.

## Behavior

- Resolves `sourceType` from an assembly-qualified or full type name and requires a concrete `ScriptableObject` type.
- Searches only configured `sourceFolders` through `AssetDatabase` and sorts source asset paths with ordinal comparison.
- Reads source identities and target fields through `SerializedObject` / `SerializedProperty`, so private `[SerializeField]` fields are supported.
- Expands collection markers such as `bonuses[].description` into concrete Unity property paths such as `bonuses.Array.data[0].description`.
- Reads nested element identities relative to each collection element.
- Preserves existing `LocalizedString` table and entry references on each draft. Name references are preserved directly; ID references are resolved through the referenced string table collection, with stable diagnostics for unresolved tables or entries.
- Reads existing locale values from the referenced collection and includes configured required locales even when their value is missing.
- Expands and normalizes the schema key template through `LocalizationKeyService`, then assigns a draft `ChangeKind` without writing it back. A reference that points at a different collection from the schema always produces `AssignReference`, even when its key text already matches.
- Sorts drafts by source asset path, concrete serialized property path, and target ID.

## Dry-run guarantee

Scanning does not call `ApplyModifiedProperties`, `SetDirty`, `SaveAssets`, or any Localization table mutation API. EditMode coverage compares source asset bytes and dirty state before and after a scan.

## Current limits

Scanning itself does not validate cross-entry ownership, propose translated values, or apply changes. Pass its result to `LocalizationValidationService`; key and validation behavior is documented in `docs/key-and-validation.md`.
