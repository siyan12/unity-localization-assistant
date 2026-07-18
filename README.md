# Unity Localization Assistant

A schema-driven Unity Localization editor package with a companion open Agent Skill and a Codex plugin adapter.

The package will help projects map ScriptableObject fields to localization keys and locale tables while validating duplicate keys, missing locales, terminology, and placeholder parity.

## Repository layout

- `Packages/com.siyan.unity-localization-assistant/` — Unity Package Manager package.
- `skills/unity-localization-assistant/` — canonical, platform-neutral Agent Skill.
- `skills/unity-localization-assistant/agents/openai.yaml` — optional OpenAI/Codex presentation and invocation metadata.
- `.codex-plugin/plugin.json` — installable Codex plugin manifest that distributes the shared skill.

## Agent compatibility

The companion workflow follows the open Agent Skills `SKILL.md` format. Its instructions and references are intended to be reusable by any skills-compatible agent client. Automatic discovery and installation locations remain client-specific, so the repository treats Codex, Claude, Gemini, Copilot, and other clients as thin adapters around one canonical skill rather than as separate copies of the workflow.

The skill does not grant capabilities by itself. An agent still needs access to the Unity project and, once implemented, the package's Editor, batchmode, CLI, or MCP entry points. Deterministic scanning, validation, draft generation, and asset application belong in the Unity package; agents should orchestrate those services and explain their structured results.

| Surface | Current status | Discovery or integration |
| --- | --- | --- |
| Open Agent Skills format | Scaffold validated | Canonical `skills/unity-localization-assistant/SKILL.md` |
| Codex | Adapter scaffold included | `.codex-plugin/plugin.json` distributes the canonical skill |
| Other skills-compatible clients | Portable content, not yet verified per client | Install or link the canonical skill using the client's supported skill location |
| Unity automation | Milestone A fixture available | `Tests/UnityProject`, local batchmode script, and GitHub Actions EditMode workflow |

Compatibility means that the workflow contract is portable; it does not promise automatic discovery, identical tool names, or equivalent permissions in every agent client.

## Status

Testable package scaffold (`0.1.0`). The public schema and editor implementation are the next milestones.

## Validate the package

The committed fixture imports the package through a local UPM `file:` dependency, compiles it, and exposes package tests through the fixture manifest's `testables` list.

With Unity 2022.3 and PowerShell 7 or newer installed, run from `pwsh`:

```powershell
.\scripts\Test-UnityPackage.ps1 -UnityEditorPath 'C:\Program Files\Unity\Hub\Editor\2022.3.62f3\Editor\Unity.exe'
```

You can omit `-UnityEditorPath` when `UNITY_EDITOR_PATH` is set or Unity Hub has a `2022.3.x` Editor in its default Windows location. The command first performs a clean package import/compilation pass, then runs EditMode tests. A missing result file, zero discovered tests, a failed test, a compile error, or a non-zero Unity process exit code fails the script.

GitHub Actions runs the same fixture with Unity 2022.3.62f3, the version used for the first successful manual import and EditMode validation. Configure the repository's `UNITY_LICENSE` secret (and `UNITY_EMAIL` / `UNITY_PASSWORD` when required by the chosen Unity license activation method) before enabling the workflow. The test runner step is not allowed to continue on error, so compilation or test failures block the job.

See [`docs/testing.md`](docs/testing.md) for Unity Editor and sample-import verification steps.

## Planning and research

- [`docs/architecture-and-roadmap.md`](docs/architecture-and-roadmap.md) — target architecture, schema direction, phased roadmap, concrete next-development plan, acceptance criteria, and initial GitHub issue breakdown.
- [`docs/github-landscape.md`](docs/github-landscape.md) — comparison with related Unity localization and AI translation projects, with the resulting product positioning.
- [`docs/release-readiness.md`](docs/release-readiness.md) — release blockers, completed checks, priorities, and the gates for a scaffold or functional release.

## Source boundary

This repository must contain only original code and redistributable dependencies. Mimic may be used as read-only design evidence, but game data, fixed GUIDs, proprietary assets, and Asset Store sources must not be copied here.

## License

MIT. See `LICENSE`.
