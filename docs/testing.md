# Unity package validation

## What the fixture verifies

`Tests/UnityProject` is a minimal Unity 2022.3.62f3 project. Its package manifest imports `Packages/com.siyan.unity-localization-assistant` by local UPM path and enables the package's test assembly with `testables`.

The repository intentionally does not commit Unity-generated `Library`, `Temp`, `Logs`, solution, or lock files. Unity creates them during the first import.

## PowerShell batchmode

Install a Unity 2022.3 Editor and PowerShell 7 or newer, including the platform modules required by your environment, then run from `pwsh`:

```powershell
.\scripts\Test-UnityPackage.ps1 -UnityEditorPath 'C:\Program Files\Unity\Hub\Editor\2022.3.62f3\Editor\Unity.exe'
```

The script performs two phases:

1. Open the fixture in batchmode and quit after package resolution and script compilation.
2. Reopen the fixture and run all EditMode tests.

Logs and NUnit XML are written under `artifacts/unity`. The script exits non-zero when Unity fails, results are absent, no test is discovered, or the XML reports any failure.

## Unity Editor verification

Use these steps when GUI verification is faster or batchmode cannot activate a local license:

1. In Unity Hub, add `Tests/UnityProject` as a project and open it with Unity 2022.3.62f3 (or another installed 2022.3 Editor for an additional compatibility check).
2. Wait until package resolution and compilation finish.
3. Confirm the Console contains no errors.
4. Open **Window > General > Test Runner**, select **EditMode**, and confirm `Siyan.UnityLocalizationAssistant.Editor.Tests.PackageSmokeTests.PackageIdentity_IsStable` is listed.
5. Choose **Run All**. Expected result: at least one test runs and all tests pass.
6. Open **Window > Package Manager**, select **Unity Localization Assistant**, open **Samples**, and import **Generic Item Catalog**.
7. Wait for compilation, confirm the Console still contains no errors, and confirm `Assets/Samples/Unity Localization Assistant/0.1.0/Generic Item Catalog` contains the sample README, assembly definition, and `GenericCatalogItem.cs`.
8. Optionally create a sample asset with **Assets > Create > Localization Assistant Samples > Generic Catalog Item** and confirm its Inspector shows stable ID, display name, and description fields.

When reporting a manual run, include the exact Unity version, Console error count, EditMode passed/failed/skipped counts, and whether the sample imported and compiled.

### Recorded manual validation

On 2026-07-18, the fixture was opened with Unity 2022.3.62f3. The local package resolved and compiled without Console errors, **Generic Item Catalog** imported and compiled without errors, and Test Runner discovered and passed `PackageSmokeTests.PackageIdentity_IsStable`.

Later on the same date, the batchmode workflow was run against a clean temporary copy of the fixture with Unity 2022.3.62f3 after Schema v1 implementation. Package import and compilation succeeded, and all 19 EditMode tests passed with 0 failures. A deliberately failing intermediate run returned a non-zero process result, confirming that the wrapper blocks on test failure.

## GitHub Actions

`.github/workflows/unity-editmode.yml` runs the fixture with `game-ci/unity-test-runner@v4` and uploads logs/results even if the test step fails. Configure Unity licensing secrets according to the repository's license type. A missing license prevents Unity from starting and is an infrastructure failure, not a package validation pass.
