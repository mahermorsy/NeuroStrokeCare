# Phase 9 — Automated Tests & Regression Coverage + Branding Assets

Working tree: `D:\Programming\Projects\NeuroStrokeCare` (confirmed via `git rev-parse --show-toplevel` = `D:/Programming/Projects/NeuroStrokeCare` before every edit in this phase). All work below was done directly on this repository through the device link — no sandbox copy was used this phase.

Nothing was committed or pushed. Everything is sitting in the working tree for review.

---

## 1. git status (relevant files only — the repo also shows ~100 files as "modified" with equal insertion/deletion counts; that is a pre-existing CRLF/LF line-ending artifact between Windows git and the Linux tool used to inspect the repo, unrelated to this phase — see the appendix)

```
 M  NeuroStrokeCare.api/Program.cs
 M  NeuroStrokeCare.slnx
?? NeuroStrokeCare.Tests/
 M  NeuroStrokeCare.client/index.html
 M  NeuroStrokeCare.client/src/components/Layout.tsx
 M  NeuroStrokeCare.client/src/pages/ForgotPassword.tsx
 M  NeuroStrokeCare.client/src/pages/IdCard.tsx
 M  NeuroStrokeCare.client/src/pages/Login.tsx
 M  NeuroStrokeCare.client/src/pages/Report.tsx
 M  NeuroStrokeCare.client/src/pages/RequestAccount.tsx
 M  NeuroStrokeCare.client/src/pages/ResetPassword.tsx
?? NeuroStrokeCare.client/public/brand/
```

## 2. git diff --stat (this phase's intentional changes)

```
NeuroStrokeCare.api/Program.cs | 5 +++++   (added `public partial class Program { }` — testability only, no behavior change)
NeuroStrokeCare.slnx           | 1 +       (added NeuroStrokeCare.Tests project entry)
```
(client-side diffs and the new `NeuroStrokeCare.Tests/` / `public/brand/` directories are new files, so they don't show in `--stat` against existing tracked content — see directory listings below.)

## 3. Solution entries

`NeuroStrokeCare.slnx` now lists 6 projects, the last one new:
```
<Project Path="NeuroStrokeCare.Tests/NeuroStrokeCare.Tests.csproj" />
```

## 4. NeuroStrokeCare.Tests/ directory listing

```
NeuroStrokeCare.Tests.csproj
CustomWebApplicationFactory.cs
TestDataHelper.cs
AdmissionLifecycleTests.cs
BedIntegrityTests.cs
ThrombolysisRegressionTests.cs
StrokeTypeTests.cs
LabResultsTests.cs
SoftDeleteTests.cs            (folded into AdmissionLifecycleTests.cs — see note below)
AuthorizationTests.cs
TimelineTests.cs
JwtRegressionTests.cs
ErrorBehaviorTests.cs
AuditAtomicityTests.cs        (documentation-only, see §7)
SkippedKnownGapsTests.cs      (documentation-only, see §7)
```
Note: a dedicated `SoftDeleteTests.cs` was planned but its one real scenario (`GetById_AfterSoftDelete_StillReturnsTheRecord`) ended up belonging next to the rest of the admission lifecycle, so it lives in `AdmissionLifecycleTests.cs` instead — no behavior lost, just file organization.

## 5. Test count

**43 test cases** across 39 `[Fact]`/`[Fact(Skip=...)]` methods + 1 `[Theory]` with 4 `[InlineData]` cases.
- 40 expected to actually run and assert real behavior.
- 3 are `[Fact(Skip = "...")]` — these document confirmed-missing features (decimal precision config, RowVersion/concurrency) rather than faking a pass. Their skip reasons are the citation of exactly what was checked in the real source.

## 6. Build/test execution — **could not be run**

No `dotnet` runtime is reachable from the environment this phase's edits were made through:
- `dotnet` is not installed in the Linux VM behind the device link (`which dotnet` → nothing, no `/usr/share/dotnet`).
- Installing it was attempted directly (`dotnet-install.sh --channel 10.0`) — failed with `403 Unable to download ... builds.dotnet.microsoft.com` / `ci.dot.net`: both hosts are outside this environment's network allowlist.
- Driving a real terminal on your Windows machine directly was also attempted (via on-screen control) — blocked: terminal/IDE windows are granted "click only" access for security, typing/keystrokes into them is refused.

**So: `dotnet restore && dotnet build && dotnet test` has not been run against this project. Nothing here should be read as "tests passed."** Every test body was written directly against the real source (controllers, entities, DTOs, enums, DbContext, AutoMapper profiles — all read in full, not guessed), and package versions in the `.csproj` were matched to the rest of the solution's `net10.0` / EF Core `10.0.11` pins, but the only way to actually know whether this compiles is to run it.

**Next step for you:** on your machine, from the repo root:
```
dotnet restore
dotnet build
dotnet test
```
If `dotnet restore` can't resolve one of the exact package versions pinned in `NeuroStrokeCare.Tests.csproj` (`Microsoft.NET.Test.Sdk 17.12.0`, `xunit 2.9.2`, `xunit.runner.visualstudio 2.8.2`, `Microsoft.AspNetCore.Mvc.Testing` / `Microsoft.EntityFrameworkCore.Sqlite` `10.0.11`), bump that one line with `dotnet add NeuroStrokeCare.Tests package <name>` and it'll pick the latest compatible version. Please paste back the first build error (if any) and I'll fix it directly against the real repo the same way everything above was written.

## 7. The most important finding — confirmed, not theoretical

**A normal `PUT /api/admission` erases a patient's recorded thrombolysis treatment.**

Traced through the actual source:
- `UpdateAdmissionRequest` has no `Thrombolysis*` properties at all.
- `AdmissionProfile`'s `CreateMap<UpdateAdmissionRequest, Admission>()` has no `.Ignore()` for those fields.
- `TableRepository.UpdateAsync` only carries `CreatedAt`/`CreatedBy`/`CurrentState` forward from the existing row before doing a full-entity `_dbSet.Update(entity)`.
- Net effect: any routine edit (e.g. a doctor correcting CT findings text) silently nulls out `ThrombolysisGivenAt`, `ThrombolysisDrug`, `ThrombolysisDoseMg`, and `ThrombolysisRecordedById` on save.

`ThrombolysisRegressionTests.NormalAdmissionPut_DoesNotEraseThrombolysisFields` asserts the **correct** behavior (fields survive the PUT) and is written to currently **fail** — that red result is the point; it pinpoints the exact bug. Recommended fix: either add the four fields to `UpdateAdmissionRequest` and preserve them through the map, or (cleaner) have `TableRepository.UpdateAsync` copy forward any field the incoming DTO doesn't carry, the same way it already does for `CreatedAt`/`CreatedBy`.

## 8. Other confirmed discrepancies (previous phase reports vs. the real repository)

| Area | Claimed | Actual (verified by reading the source) |
|---|---|---|
| Decimal precision | `HasPrecision` configured for lab/dose fields | **Not configured anywhere.** `NeuroFlowDbContext.OnModelCreating` has zero `HasPrecision`/`[Precision]` calls. The migration named `FixDecimalPrecision` was opened directly — it only adds `EmployeeId`/`ProfilePhotoUrl` columns and recreates an index; it never touches decimal precision despite its name. |
| Concurrency / RowVersion | Optimistic concurrency (stale write → 409) | **No `RowVersion` property exists on `Admission`** (or anywhere), and no `IsRowVersion()`/`[ConcurrencyCheck]` in the model. A second writer silently overwrites the first. |
| Bed integrity | Update/ChangeStatus blocked on an occupied bed | **Not blocked.** `BedService` is an empty class; `BedController` is generic CRUD with zero occupancy checks. `Update_OnOccupiedBed_IsNotBlocked` / `ChangeStatus_OnOccupiedBed_IsNotBlocked` demonstrate this directly. |
| Soft delete | `GetById` returns 404 for a soft-deleted admission | **It returns 200 with the data.** `AdmissionController.GetById` filters only on `Id == id`, with no `CurrentState` check — unlike `GetAll`/`GetPaged`, which do filter correctly. |
| Error behavior | Domain failures avoid bare 500s | **`ChangeStatus` on a missing id returns 500**, not 404 — the repository throws `DataAccessException` wrapping `KeyNotFoundException`, and the one global handler in `Program.cs` maps every unhandled exception to 500. (`UpdateAsync`'s missing-row path is fine — it returns a sentinel the controller turns into a clean 404; the inconsistency between the two is the actual bug.) |
| Lab validation | Range/sanity rules on lab values | **None exist server-side.** `CreateLabResultsRequest` has no `[Range]` attributes; `INRAlert`/`GlucoseAlert`/`PlateletsAlert` are read-only *display* flags computed from whatever was stored, never a gate on Create/Update. |
| Thrombolysis validation | Empty drug / invalid dose rejected | **Neither is rejected** — `drug` and `doseMg` are plain query parameters with no `[Required]`/`[Range]` attributes. |
| Closed-admission guards | Stroke type / thrombolysis blocked once an admission is closed | **Only `Transfer` checks for a terminal status.** `SetStrokeType` and `RecordThrombolysis` have no such guard. |
| Audit atomicity | `SetStrokeType`/`RecordThrombolysis` write state + audit atomically | **They don't** — both call `SaveChangesAsync` twice (once for the field, once for the `AuditLog` row), unlike `Transfer`/`Create`, which add the audit row to the same `_context` before one single `SaveChangesAsync`. Documented in `AuditAtomicityTests.cs` rather than tested directly (proving it needs a fault-injecting interceptor, which needs a real `dotnet test` run to get right — see §6). |
| Clinical report endpoint | A dedicated Report DTO/endpoint | **There is no backend Report controller or endpoint at all** (confirmed: zero matches for "Report" anywhere under `NeuroStrokeCare.api`, `.Core`, `.Service`). The frontend's Report page assembles the view client-side from the Admission/Timeline/LabResults/DoorTiming/FollowUpNotes endpoints. Area 9 of the spec (clinical report) is therefore covered indirectly, through those individual endpoints' own tests, not as one combined report test. |
| Missing-patient check | `Create` rejects a nonexistent `PatientId` | **No such check exists.** `AdmissionController.Create` never verifies the patient — it falls through to `SaveChangesAsync`, where only two specifically-named unique-index violations are translated to a friendly response; anything else re-throws to the global 500 handler. Documented, not asserted to a specific status code, since behavior also depends on whether FK enforcement is on (SQL Server: yes: SQLite test provider here: off by default, not changed). |

## 9. Uncovered risks worth flagging even though no test covers them

- **SQLite vs. SQL Server fidelity** — the test suite uses SQLite (per the spec's instructions) via a shared in-memory connection. The filtered unique index on `Admission.PatientId` (`[DischargeTime] IS NULL AND [CurrentState] = 1`) was ported as-is since SQLite accepts bracket-quoted identifiers, but this was never confirmed against a real SQLite engine (no `dotnet test` run — see §6). If it doesn't behave identically, `Create_DuplicateActiveAdmissionForSamePatient_Rejected` is the test that will reveal it.
- **FK enforcement is off by default on the SQLite connection used here** and was not turned on — a few tests (missing-patient Create) document current behavior rather than assert a single expected status code for exactly this reason.
- **`IsRootSuperAdmin`** on `ApplicationUser` was noticed in passing but has no test coverage — it's not mentioned in the spec's 14 areas, but may deserve a look given it appears to bypass normal role checks in places.
- **No endpoint-level request-size/throttling tests** — out of scope per the spec, noting only because a clinical system is being hardened.

## 10. Branding assets (second half of this phase)

Used your existing files as-is (verified byte-identical after copying — see commands below), wired into exactly the 5 locations you specified. Nothing else was touched, no SVG content was modified, no colors changed, no pages redesigned.

Files copied **unmodified** from `Logo.SVG/` into `NeuroStrokeCare.client/public/brand/` (a `diff` confirmed byte-for-byte identical to source):
- `NeuroStrokeCare_Logo.svg` — the full lockup (shield/brain/pulse mark + "NeuroStrokeCare" + "Mansoura University Hospital" + "STROKE DEPARTMENT", on its own dark-teal background)
- `WindowIcon.svg` — the compact mark alone (same dark-teal background, square)

| Location | File | What changed |
|---|---|---|
| Sidebar + mobile header (`Layout.tsx`) | WindowIcon.svg | Replaced the `LogoMark` SVG sitting inside a separate `bg-accent` badge with the icon image directly (it already has its own dark background baked in — nesting it inside another colored badge would have doubled up backgrounds). Same size/position/corners as before. |
| Login, Forgot password, Request account, Reset password | NeuroStrokeCare_Logo.svg | Replaced the small accent-badge mark with the full wide logo, sized to the card (`max-w-[280–300px]`, `h-auto` — aspect ratio preserved, never stretched). On the Login page specifically, the separate "NeuroStrokeCare" / "Mansoura University Hospital" text lines were dropped since the logo image already contains both — keeping them would have shown the same two lines twice. The other three pages keep their own distinct headings (e.g. "Reset your password") since those aren't duplicated by the logo. |
| ID Card (`IdCard.tsx`) | WindowIcon.svg | Same badge-replacement treatment as the sidebar, at the existing `h-9 w-9` size. |
| Clinical Report (`Report.tsx`) | NeuroStrokeCare_Logo.svg | There is no logo anywhere on this page today — found the existing **print-only** header block (`hidden print:block`, currently just plain "NeuroStrokeCare — Patient Report" text, shown when exporting/printing) and added the logo image there, above the heading. Simplified that heading from "NeuroStrokeCare — Patient Report" to "Patient Report" (the brand name is now in the image) and dropped the now-duplicate "Mansoura University Hospital" from the subtitle line, keeping the dynamic "generated {timestamp}" part. |
| Favicon / browser tab | WindowIcon.svg | `index.html`'s `<link rel="icon">` now points at `/brand/WindowIcon.svg` instead of the old placeholder `/favicon.svg`. |

`npx tsc -b --force` run after every change — **clean, zero errors**, confirmed after the full set of edits above.

## Appendix — the long "~100 modified files" list in `git status`

Not caused by this phase. It's a pre-existing CRLF/LF line-ending mismatch between git running on your Windows machine (autocrlf) and the Linux tool used to inspect the repo through the device link — confirmed earlier this session: `git diff --stat` on those files shows equal insertion/deletion counts per file (e.g. `658 ++--`), the signature of whitespace-only churn, not real content changes. Your own `git status` on your machine showed a clean tree before this phase started. Nothing in this phase added to that list — it only touched the files named in §1 above.
