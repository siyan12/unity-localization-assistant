# Repository Guidelines

## Scope

- Build a reusable Unity Package Manager package and companion open Agent Skill, with Codex support provided through a platform-specific plugin adapter.
- Keep the core schema independent of Mimic or any other game-specific data model.
- Place integrations and example schemas under Samples, not package core.

## Source boundaries

- Treat source game repositories as read-only unless a task explicitly says otherwise.
- Never copy paid Asset Store code, art, audio, fonts, private identifiers, table GUIDs, or production content.
- Add third-party dependencies through package metadata and document their licenses.

## Unity changes

- Prefer Unity Editor and Localization APIs over direct serialized YAML edits.
- Keep editor-only code in an Editor assembly.
- Add or update editor tests for schema parsing, key generation, and validation behavior.

## Validation

- Validate the Agent Skill against the open Agent Skills format and validate each platform-specific manifest, including the Codex plugin manifest.
- Compile against the minimum supported Unity version.
- Run editor tests and import the package in a clean sample project before release.
