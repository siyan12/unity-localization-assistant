## Summary

<!-- What changed and why? Link the issue with Closes #123 when appropriate. -->

## Validation

- [ ] Relevant EditMode tests pass.
- [ ] Clean Unity 2022.3 import/compile was run when package behavior changed.
- [ ] Documentation and `CHANGELOG.md` were updated when public behavior changed.
- [ ] Package and Codex plugin prerelease versions remain consistent.

## Repository boundaries

- [ ] Unity Editor/Localization APIs are used instead of direct serialized YAML edits where practical.
- [ ] Editor-only code remains in an Editor assembly.
- [ ] No credentials, production data, paid Asset Store content, private identifiers, or fixed production table GUIDs are included.
- [ ] Project-specific integrations and examples remain outside package core.

## Compatibility and risk

<!-- Describe schema/API compatibility, migration needs, asset-write behavior, Undo/rollback behavior, and known limits. -->
