# Unity Localization Assistant package

This package provides public Schema v1/domain contracts plus deterministic read-only scanning, key generation, ownership indexing, and validation for schema-driven Unity Localization editor workflows.

Create a schema from **Assets > Create > Localization Assistant > Schema**. Invalid configuration is reported through stable diagnostics by `LocalizationSchemaReader`; see the repository's `docs/schema-v1.md` for the complete v1 contract.

After reading a valid schema, call `SchemaScanner.Scan`. The scanner resolves an arbitrary `ScriptableObject` source type, searches configured `sourceFolders` with `AssetDatabase`, traverses top-level and nested serialized properties (including private `[SerializeField] LocalizedString` fields), reads existing references and locale values, and returns `LocalizationDraftEntry` objects plus diagnostics. Scanning is a dry run: it does not dirty or save source assets, tables, or schema assets.

Pass the schema and scan result to `LocalizationValidationService.Validate` to obtain deterministic diagnostics, counts, and a key ownership index. Validation covers normalized-key collisions, identities, collection stability, references, required locales, and Smart String placeholder parity. See the repository's `docs/key-and-validation.md` for the public behavior.

Proposed locale value generation, transactional Apply, and reference writeback are not part of this milestone.

## Requirements

- Unity 2022.3 or newer.
- Unity Localization 1.5.9 (installed automatically as a package dependency).

## Sample

Use Package Manager's **Samples** section to import **Generic Item Catalog**. The sample contains only fictional data types and does not include production game content.

## Tests

Package EditMode tests live in `Tests/Editor`. See the repository root README for local batchmode and CI instructions.
