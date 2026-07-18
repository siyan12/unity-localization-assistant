# Contributing

Thanks for helping improve Unity Localization Assistant. By participating, you
agree to follow the [Code of Conduct](CODE_OF_CONDUCT.md).

## Before opening a change

- Search existing issues first. For substantial behavior or public contract
  changes, open an issue before implementation.
- Security vulnerabilities must follow [SECURITY.md](SECURITY.md), not a public
  issue.
- Keep package core independent of Mimic and other project-specific data. Put
  integrations and fictional examples under `Samples~`.
- Do not contribute paid Asset Store sources, production content, private IDs,
  fixed production table GUIDs, credentials, or other material you cannot
  redistribute.

## Development

The minimum supported Unity line is 2022.3. Install PowerShell 7 and a Unity
2022.3 editor, then run:

```powershell
.\scripts\Test-UnityPackage.ps1 -UnityEditorPath '<path-to-Unity.exe>'
```

Prefer Unity Editor and Localization APIs over serialized YAML edits. Keep
editor-only code in an Editor assembly and add or update EditMode tests for
schema parsing, scanning, key generation, validation, and Apply behavior.

## Pull requests

- Keep changes focused and link the relevant issue.
- Describe behavior, compatibility impact, and validation evidence.
- Update documentation and `Packages/com.siyan.unity-localization-assistant/CHANGELOG.md`
  when public behavior changes.
- Keep package and Codex plugin versions aligned. During development, record
  changes under `Unreleased`; only release preparation creates a dated version.
- Confirm the diff contains no secrets, production data, paid assets, or
  unrelated Unity-generated changes.
