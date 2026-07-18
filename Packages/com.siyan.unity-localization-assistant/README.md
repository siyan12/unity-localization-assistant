# Unity Localization Assistant package

This package provides a testable foundation and the public Schema v1/domain contracts for schema-driven Unity Localization editor workflows. Scanning and asset-generation services will be introduced in later milestones.

Create a schema from **Assets > Create > Localization Assistant > Schema**. Invalid configuration is reported through stable diagnostics by `LocalizationSchemaReader`; see the repository's `docs/schema-v1.md` for the complete v1 contract.

## Requirements

- Unity 2022.3 or newer.
- Unity Localization 1.5.9 (installed automatically as a package dependency).

## Sample

Use Package Manager's **Samples** section to import **Generic Item Catalog**. The sample contains only fictional data types and does not include production game content.

## Tests

Package EditMode tests live in `Tests/Editor`. See the repository root README for local batchmode and CI instructions.
