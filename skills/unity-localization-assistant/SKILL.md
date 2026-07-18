---
name: unity-localization-assistant
description: Configure, implement, or review schema-driven localization workflows for Unity projects using Unity Localization tables and ScriptableObject data. Use when an agent needs to map asset fields to localization keys, create an editor workflow, validate placeholders and locales, or maintain the Unity package in this repository.
---

# Unity Localization Assistant

Use the package schema as the source of truth when connecting project data to Unity Localization tables.

## Workflow

1. Read the project schema and the target ScriptableObject type before proposing changes.
2. Inspect the relevant `StringTableCollection`, shared table data, locale tables, and existing localized references.
3. Present proposed key rules, field mappings, locales, and placeholder behavior before writing project assets.
4. Prefer Unity Editor and Localization APIs over direct YAML edits.
5. Keep game-specific types, terminology, GUIDs, and content in Samples or project configuration, never in package core.
6. Validate package compilation, editor tests, duplicate keys, missing locales, and placeholder parity.
7. Report changed files and any Unity Inspector work that remains.

## Execution Model

- Treat this `SKILL.md` and its references as the platform-neutral workflow contract.
- Detect the available Unity Editor, batchmode, CLI, or MCP capabilities before attempting project operations.
- Use structured package services for scan, diagnostics, draft, diff, and apply when they are available.
- Stop after analysis or produce an explicit manual handoff when the active agent client cannot access a required capability.
- Keep client-specific discovery, presentation, invocation, and tool metadata outside the shared workflow. For OpenAI and Codex, use `agents/openai.yaml` and the repository's Codex plugin manifest.

## Safety Boundaries

- Do not copy Asset Store packages or proprietary project assets into this repository.
- Do not assume a table GUID, locale list, asset path, or field name.
- Do not overwrite existing translations without showing the proposed change.
- Do not modify a source game repository when it is mounted only as reference material.

## Schema

Read `references/schema.md` when defining or changing the configuration contract.
