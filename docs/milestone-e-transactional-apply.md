# Milestone E: Transactional Apply

## Objective

Turn a reviewed, validated localization draft into one atomic, auditable Unity
Editor operation. Apply must never silently overwrite translations, operate on
a stale draft, or leave tables and `LocalizedString` references half-updated.

## Work breakdown

1. **Apply plan and fingerprints** — define immutable plan entries, source/table
   fingerprints, conflict kinds, and structured change reports.
2. **Unity Localization adapter** — create/reuse shared keys and implement
   `PreserveExisting`, `FillMissing`, and explicit `Overwrite` through Editor APIs.
3. **Reference writeback** — update top-level and nested array/list
   `SerializedProperty` paths without direct YAML editing.
4. **Transaction boundary** — record Undo, apply table/reference changes, mark
   dirty objects, save, and return a deterministic report; failure must roll back
   or stop before any mutation.
5. **Safety tests** — cover stale drafts, conflicting ownership, partial failure,
   Undo, idempotency, nested writeback, and all update policies.
6. **Operator documentation** — document preview, overwrite confirmation,
   limitations, failure recovery, and the no-orphan-deletion rule.

## Contract decisions to make first

- Fingerprints must include source identity/property state, shared-key identity,
  locale values relevant to the plan, and referenced collection/entry IDs.
- Planning and validation are pure/read-only. Mutation begins only after every
  enabled entry passes a final stale-state and conflict check.
- `PreserveExisting` is the default. `Overwrite` requires an explicit plan entry
  showing old and new values; warnings cannot be implicitly dismissed by an API
  caller.
- Shared keys are allocated before locale values, but no allocation is committed
  until the whole plan is valid.
- Automatic orphan deletion and key renaming remain out of scope for Milestone E.

## Acceptance criteria

- Applying the Generic Item Catalog draft writes the expected shared key, locale
  entries, and `LocalizedString` references.
- Undo restores every touched object; a failed or stale Apply leaves no partial
  changes.
- Reapplying the same plan is a no-op with a stable report.
- Existing non-empty translations survive the default policy.
- Tests run on Unity 2022.3 and cover nested serialized references and simulated
  mid-transaction failure.

## Sequencing

Issues 1 and 2 establish contracts and the adapter. Issue 3 can follow the plan
contract in parallel with adapter implementation. Transaction orchestration then
combines them, followed by the complete safety matrix and documentation. The
EditorWindow remains Milestone F and consumes these services without owning
mutation logic.
