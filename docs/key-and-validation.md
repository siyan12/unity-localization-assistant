# Key generation and validation

Milestone D adds deterministic key generation and a read-only validation report on top of `SchemaScanner`.

## Key contract

`LocalizationKeyService.Expand` replaces `{sourceId}`, `{targetId}`, and `{elementId}`, then normalizes the complete key with these invariant rules:

1. Apply Unicode Form KC and trim surrounding whitespace.
2. Preserve Unicode letters and digits, including their case, plus `.`, `_`, and `-`.
3. Collapse each run of other characters to `-`.
4. Remove leading and trailing `.`, `_`, and `-`.

The rule is culture-independent and intentionally preserves case to avoid silently renaming existing keys. Ownership comparisons use ordinal equality. Normalization can make distinct raw identities collide, so callers must validate the resulting scan rather than assume generation implies uniqueness.

Collection targets without `elementIdPath` temporarily use the concrete array index as `{elementId}`. The report emits `ULA_VALIDATE_ELEMENT_IDENTITY_MISSING`; it is an error when `requireStableElementIdentity` is enabled and a warning otherwise. Reordering such a collection changes its keys. Schema v1 supports one stable collection identity per target; multiple `[]` markers produce `ULA_VALIDATE_NESTED_COLLECTION_UNSUPPORTED`.

## Validation report

Call `LocalizationValidationService.Validate(schema, scanResult)`. The returned `LocalizationValidationReport` contains:

- deterministically ordered diagnostics and error/warning/info counts;
- `IsValid`, which is false when at least one error exists;
- an immutable `LocalizationKeyOwnershipIndex` mapping each suggested key to sorted source/property owners.

Validation does not mutate drafts, source assets, string tables, or schemas. It aggregates scan and entry diagnostics, removes only exact duplicates, and adds checks for:

- duplicate source identities, element identities, and normalized keys;
- empty normalized keys and unstable or unsupported collection identity;
- wrong, unresolved, or missing table/entry references;
- required locale tables, entries, and non-empty values for required targets;
- Smart String parse errors and exact placeholder-contract parity.

Placeholder parsing uses the SmartFormat parser bundled with Unity Localization 1.5.9, including selector chains, escaped braces, and nested formats. Each placeholder's first selector is compared with `placeholderContract` using ordinal set equality.

An existing key different from the suggested normalized key remains a visible `RenameKey` draft preview. Schema v1 has no legacy-alias field, so alias migration is not added implicitly in this milestone and no old key is deleted.
