# Versioning and prerelease policy

The Unity package and Codex plugin use SemVer and always carry the same version.
The current development line is `0.1.0-alpha.1`; it does not imply that a tag,
GitHub Release, or stable package has been published.

- `alpha.N`: contracts and features are still changing. Milestones E and F use
  this line; each published prerelease increments `N`.
- `beta.N`: Transactional Apply and the Editor workflow are feature-complete and
  ready for broader compatibility testing.
- `rc.N`: feature-frozen; only release blockers, documentation, packaging, and
  validation fixes are accepted.
- `0.1.0`: published only after the Milestone G release gates pass.

Release preparation must update package and plugin manifests together, move
relevant changelog entries out of `Unreleased`, use a matching `v<version>` tag,
and mark non-stable GitHub Releases as prereleases. Ordinary merges do not create
tags or Releases and do not automatically increment the version.
