# Phase 11 — Patient Context, Hospital Number & Clinical Workflow UX — Delivery Report

Repository: `D:\Programming\Projects\NeuroStrokeCare` (verified via `git rev-parse --show-toplevel` + `git branch --show-current` repeatedly throughout this phase, including twice after the device bridge temporarily disconnected and reconnected). Scope: the "PHASE 11 — PATIENT CONTEXT, HOSPITAL NUMBER & CLINICAL WORKFLOW UX" spec (21 numbered areas), additive to `PHASE9_REPORT.md`, `PHASE10_REPORT.md` and `ADDITIONAL_TASKS_REPORT.md`. **No commit, no push, no deploy was performed** — every change below is sitting in the working tree for your own review (`git status` / `git diff`).

Every claim below was checked against the real source files read through the device bridge before anything was written. Where the spec assumed something the repo doesn't actually support (e.g. a three-way Stroke Code "active / resolved / never activated" state), that gap is called out rather than invented.

---

## 1. Files changed

**Backend (hand-written — no real compiler available in this sandbox; see §11):**

New:
- `NeuroStrokeCare.infrastructure/Migrations/20261002154700_AddPatientHospitalNumber.cs` + `.Designer.cs` — new migration (nullable `nvarchar(30)` column + filtered unique index).
- `NeuroStrokeCare.Tests/Phase11Tests.cs` — new regression test file (see §9).

Modified:
- `NeuroStrokeCare.Data/Entities/Patient.cs` — `HospitalNumber` field added.
- `NeuroStrokeCare.Core/Features/Patient/Dtos/{CreatePatientRequest,UpdatePatientRequest,PatientResponse}.cs` — `HospitalNumber` added to all three.
- `NeuroStrokeCare.api/Controllers/PatientController.cs` — `[Authorize(Roles = Roles.AnyClinical)]` added to Create/Update/ChangeStatus (previously had none); HospitalNumber normalization + explicit uniqueness pre-check + a correctly-unwrapped `DataAccessException` catch as a last-resort safety net.
- `NeuroStrokeCare.api/Controllers/AdmissionController.cs` — `Search` rewritten: priority ordering (HospitalNumber → National ID → name), `[Authorize(Roles = Roles.AnyClinical)]`, `AsNoTracking`, database-side filtering throughout, clamped/deterministic pagination.
- `NeuroStrokeCare.Core/Features/Admission/Dtos/AdmissionSearchResultResponse.cs` — `HospitalNumber` field added (file itself was introduced in the previous "ADDITIONAL CLINICAL UX" phase).
- `NeuroStrokeCare.infrastructure/Context/NeuroFlowDbContext .cs` (note the literal space in the filename) — `HospitalNumber` given `HasMaxLength(30)` + a filtered unique index (`WHERE [HospitalNumber] IS NOT NULL`).
- `NeuroStrokeCare.infrastructure/Migrations/NeuroFlowDbContextModelSnapshot.cs` — updated to match.
- `NeuroStrokeCare.Tests/TestDataHelper.cs` — `SeedPatientAsync` given optional trailing parameters (`nationalId`, `hospitalNumber`, `firstName`, `lastName`); every existing call site is untouched (same defaults as before).

**Frontend (type-checked with `tsc -b --force` and production-built with `npm run build` — both clean; see §10):**

New:
- `NeuroStrokeCare.client/src/context/PatientSelectionContext.tsx` — the shared "Global Patient Context" (§4 below).
- `NeuroStrokeCare.client/src/components/PatientContextHeader.tsx` — the compact header bar (§4).
- `NeuroStrokeCare.client/src/components/ContextAwareAdmissionField.tsx` — wraps `AdmissionPicker` so write forms auto-use the context admission (§5).
- `NeuroStrokeCare.client/src/pages/PatientSummary.tsx` — new compact clinical overview page.
- `NeuroStrokeCare.client/src/lib/patients.ts` — `maskNationalId` extracted here (was a private function inside `Patients.tsx`; `Patients.tsx` now imports it) so `PatientSummary.tsx`/`Report.tsx`'s context-snapshot builder can reuse the exact same masking rule instead of a second copy.

Modified:
- `NeuroStrokeCare.client/src/types/entities.ts` — `hospitalNumber` added to `PatientResponse` and `AdmissionSearchResultResponse`.
- `NeuroStrokeCare.client/src/lib/roles.ts` — `isClinicalRole` added.
- `NeuroStrokeCare.client/src/lib/alertsApi.ts` — `hospitalNumber` added to every `AlertItem`, attached in one pass at the end of `loadAlerts()` rather than at each of its ~20 individual `alerts.push(...)` sites.
- `NeuroStrokeCare.client/src/components/AdmissionPicker.tsx` — rewritten: full WAI-ARIA combobox keyboard pattern (ArrowUp/Down, Enter, Escape, `aria-activedescendant`), Hospital Number shown first in both the result list and the "selected" chip.
- `NeuroStrokeCare.client/src/components/Layout.tsx` — mounts `<PatientContextHeader />` once, just above `<Outlet />`.
- `NeuroStrokeCare.client/src/App.tsx` — wraps the routed app in `<PatientSelectionProvider>`; adds `/patient-summary`, `/patient-summary/:admissionId`, and a param-less `/report` fallback.
- `NeuroStrokeCare.client/src/hooks/useAdmissionContext.ts` — `hospitalNumberByAdmissionId` map added alongside the existing `patientNameByAdmissionId`.
- `NeuroStrokeCare.client/src/pages/{Assessments,LabResults,FollowUp,DoorTiming}.tsx` — GUID-slice "Admission #12AB34CD" fallbacks replaced with Hospital Number; write-modal admission fields swapped for `ContextAwareAdmissionField` (except Door Timing — see §5); Door Timing's activation confirmation and admission list now show Hospital Number and explain context ineligibility.
- `NeuroStrokeCare.client/src/pages/Patients.tsx` — Hospital Number field on create/edit forms, list, and the thrombolysis confirmation text (from the previous phase, confirmed still intact); `maskNationalId` now imported from `lib/patients.ts` instead of a local copy.
- `NeuroStrokeCare.client/src/pages/Report.tsx` — a `Hospital number` row added to the Patient section; now falls back to the shared context / an `AdmissionPicker` when reached without a route param.
- `NeuroStrokeCare.client/src/pages/Admissions.tsx` — a "Summary" row action added next to "Report"; the discharge/transfer confirmation checkbox now names the patient + Hospital Number.
- `NeuroStrokeCare.client/src/pages/Alerts.tsx` — Hospital Number shown under the patient name in the Patient column.

**Isolated diff** (the exact file list above, excluding the pre-existing CRLF/LF checkout artifact documented in `PHASE9_REPORT.md`'s appendix — confirmed again this phase via `git diff --numstat`: every excluded file has **equal** insertion/deletion counts, i.e. no real content changed):

```
23 files changed, 837 insertions(+), 131 deletions(-)
```

plus 11 new files (2,833 lines total, the largest being the generated `*.Designer.cs` migration snapshot at 1,586 lines).

Note: `AdmissionController.cs`'s cumulative diff also still carries the *previous* phase's original `Search` endpoint (nothing has been committed between phases), so that one file's line count is not Phase-11-only — the content described in §3 below is what Phase 11 actually changed about it.

---

## 2. HospitalNumber — model, API, UI

Reviewed first, per the spec's own instruction: `Patient.cs` had `NationalId`, no hospital/MRN-style field anywhere, and `NationalId` was already indexed (`nvarchar(450)`, unique, used as the de facto "find a patient" key in `AdmissionController.Search`'s previous-phase version).

Added `Patient.HospitalNumber` (`string?`, `nvarchar(30)`) — independent from `NationalId`, never derived from it, nullable (existing patients stay `null`; nothing backfills it), unique **only among non-null values** via a SQL Server filtered index (`HasFilter("[HospitalNumber] IS NOT NULL")`) — the same pattern already used for "one open admission per patient." `[StringLength(30)]` on the two request DTOs matches the column's `HasMaxLength(30)` (required for SQL Server to index the column at all — an unindexed `nvarchar(max)` column, like the previous phase's `EmployeeId`, can't carry a unique index).

Surfaced in: `PatientResponse` (API), the Patients.tsx create/edit forms, the Patients.tsx list (replacing the previous phase's bare masked-National-ID line), `AdmissionSearchResultResponse` → the AdmissionPicker and every context-aware page's "Patient" column, the Patient Context Header, the Patient Summary page, the Report page, the Alerts page, and the thrombolysis / stroke-code-activation / discharge-transfer confirmation texts. Nowhere does the UI fall back to a raw database GUID instead — the previous "Admission #12AB34CD" slices in Assessments.tsx and FollowUp.tsx are gone, replaced with "Hospital No. X" / "No hospital number assigned."

`PatientController.Create`/`Update` now explicitly check uniqueness before writing (`HospitalNumberTakenAsync`, excluding the patient's own id on Update) and return `409 Conflict` with a clear message; the database's own unique index is a second line of defense against a race condition, exactly like `AdmissionController.Create`'s existing pattern for "one open admission per patient."

---

## 3. Admission search — `GET /api/Admission/search`

Rewritten (the endpoint itself was added by the previous phase; this phase changed its behavior, not its existence):

- **Priority**: Hospital Number → National ID → name, implemented as a match-rank in the SQL `ORDER BY` (0/1/2), then most-recent admission first — not three separate queries.
- **Performance** (area 15): `AsNoTracking()`; `take` clamped `Math.Clamp(take, 1, 50)`; every filter (`CurrentState`, `Patient.CurrentState`, `openOnly` → `DischargeTime == null`, the term match) is expressed in the LINQ query translated to SQL, not loaded into memory first. No "load everything, filter in C#."
- **Security** (area 16): `[Authorize(Roles = Roles.AnyClinical)]` — closes a real gap (this endpoint previously relied on nothing but the global authenticated-user fallback policy). Soft-deleted patients and admissions are excluded (`CurrentState == Active` on both sides of the join). Only the picker-required fields are returned (`AdmissionSearchResultResponse`), never a full `Patient`/`Admission` record.
- **Historical search** (area 2): `openOnly` defaults to `true` (new clinical writes only see open admissions) but can be set `false` for read-only/historical workflows (Report/Patient Summary's own `AdmissionPicker` usage still defaults to `true`, matching the spec's "only appropriate active/open admissions offered for new clinical writes").

One honest limitation, not worked around: `AnyClinical` is defined in `Roles.cs` as the union of every one of this app's 6 roles (`AnyDoctor + Nurse + NursingSupervisor`), so there is no "wrong clinical role" to test a 403 against for this endpoint specifically — only "unauthenticated" produces a failure. `Phase11Tests.Search_RequiresAuthentication_Returns401` documents exactly that boundary rather than asserting a 403 case that cannot occur.

---

## 4. Global Patient Context + Patient Context Header

`context/PatientSelectionContext.tsx` — a plain React Context (deliberately named `PatientSelectionContext`/`usePatientSelection`, not "PatientContext", to avoid confusion with the pre-existing, unrelated `useAdmissionContext` id→label lookup hook). Stores only `{ admissionId, snapshot }` where `snapshot` is exactly the `AdmissionSearchResultResponse` shape the picker/search endpoint already returns — never a full Patient or Admission record. Persisted to `sessionStorage` (not `localStorage`) specifically so it clears when the tab closes, and is also cleared immediately on logout (`useEffect` watching `isAuthenticated`) so a different staff member on a shared workstation never inherits the previous user's selected patient.

`components/PatientContextHeader.tsx` — mounted once in `Layout.tsx` (which itself only mounts per sign-in, not per navigation — confirmed by reading its existing alert-badge-polling comment), so it costs nothing extra on ordinary navigation. Renders nothing when no patient is selected. Shows name, Hospital Number, age/sex and stroke type (fetched lazily, once per admission change, into this component's own local state — not duplicated into the shared context), admission date, ward/bed, status, and a Stroke Code indicator that links to `/door-timing`. "Change patient" opens an inline `AdmissionPicker` in a modal; "Clear patient" is always visible.

---

## 5. Context-aware pages

- **Assessments / Lab Results / Follow-up notes**: their "New ⟨X⟩" modal now opens with the admission pre-filled from context (`ContextAwareAdmissionField`), shown as a compact read-only "Recording for ⟨name⟩ (Hospital No. ⟨X⟩) — Change" line instead of an always-empty picker; clicking "Change" reveals the full searchable `AdmissionPicker`.
- **Door Timing — deliberately NOT using `ContextAwareAdmissionField`** (area 5's explicit warning against weakening the safety rule): it keeps its own `<Select>` built from `admissionsWithoutOpenCode`. A new `contextEligible`/`contextIneligibleReason` check pre-fills the activation form from context **only when the context's admission passes that same eligibility filter**; when it doesn't (already has an active code, or discharged), the admission field is left empty and a clear warning banner explains why, rather than silently allowing (or silently blocking without explanation) the write.
- **Report / "Timeline"**: confirmed by grep that this codebase has no separate Timeline page — `getTimeline()` is called from inside `Report.tsx`/`reportApi.ts`, so "make Timeline context-aware" and "make Report context-aware" are the same change. `Report.tsx` now accepts being reached with no `:admissionId` (new `/report` route), falling back to context, then to an `AdmissionPicker`; reaching it directly via `/report/:admissionId` also establishes that admission as the shared context.
- **Patient Summary** (new, area 6): same fallback chain (route param → context → picker). Reuses `loadAdmissionReport` (the exact aggregation `Report.tsx` already uses) rather than re-implementing any backend business logic — this page only decides what to *show*: demographics, Hospital Number, masked National ID, current admission/ward/bed, stroke type, thrombolysis status, the single latest result per assessment type and the latest lab panel, current Stroke Code status, and links out to the full modules.

---

## 6. Patient Edit authorization

Reviewed first: every other write-capable controller already has a role boundary (`AnyDoctor` for Admission/NIHSS/ASPECTS/ICH/CanadianTIA/GCS, `AnyNurse` for Braden/GUSS/Morse, `AnyClinical` for LabResults/FollowUpNote) — `PatientController` had **none** before this phase, relying only on the global authenticated-user fallback. `Roles.AnyClinical` was chosen for Patient Create/Update/ChangeStatus specifically because it's the existing constant already used for the same shape of entity (LabResults/FollowUp), not a new or invented boundary; it's a one-word change in the attribute if a narrower group is actually intended.

Confirmed (by reading `TableRepository.UpdateAsync`) that the generic `UpdateCommand<T>` already re-fetches the existing row and restores `CreatedAt`/`CreatedBy`/`CurrentState` before saving, regardless of what `UpdatePatientRequest` does or doesn't carry — so an ordinary Patient edit cannot indirectly clobber audit history or lifecycle state; this was pre-existing infrastructure, not something this phase had to add.

Frontend: `Patients.tsx`'s "Edit" button and modal are gated on `isClinicalRole(user?.role)` (matching the backend boundary exactly), kept as a separate action/modal from Admission transfer, Stroke Code activation, and the assessment forms. The backend attribute remains the real boundary either way.

---

## 7. Stroke Code context integration

The Patient Context Header's indicator (§4) is a single `GET /DoorTiming/paged?admissionId=...&pageSize=1` check, re-run only when the selected admission changes — no polling, no SignalR, no new real-time infrastructure, per the spec's explicit limit. It links to `/door-timing` rather than duplicating the activation UI.

One honest model limitation: `DoorTiming`'s "stand down" (area 10's "resolved" state) reuses the same generic `CurrentState = Inactive` soft-delete mechanism as everything else in this codebase, and the only read endpoint used here (`CurrentState == Active`) can't distinguish "stood down" from "never activated" — both simply don't appear. The indicator can therefore only show *active / not active*, not a three-way active/resolved/never state. This is the existing data model, not something this phase invented or could safely add a new column to without a migration beyond what was asked for.

Duplicate-activation prevention, the activation confirmation checkbox, and the AuditLog writes from the previous phase are all untouched — the confirmation text was extended (area 13) to name the patient + Hospital Number, nothing about its mechanics changed.

---

## 8. Search / privacy decisions

- **National ID**: unmasked only in the formal clinical report (`Report.tsx`) — unchanged, confirmed still correct. Everywhere else (AdmissionPicker, Patient Context Header, Patient Summary, Alerts) it's masked, using one shared `maskNationalId` helper (`lib/patients.ts`) instead of the previous phase's private copy inside `Patients.tsx`.
- **Hospital Number** is now the first identifier shown in ordinary workflows (AdmissionPicker results/chip, Patient Context Header, Patient Summary, Alerts), with National ID as a secondary, masked identifier — matching "HospitalNumber becomes the preferred operational identifier."
- **High-impact confirmations** (area 13) — thrombolysis (previous phase), Stroke Code activation, and discharge/transfer — all name the patient and show Hospital Number (never National ID) in their confirmation text.
- **Wrong-patient safety**: every write-capable context-aware form shows the selected patient's name + Hospital Number before submission is even possible (the compact "Recording for..." summary, or the Door Timing `<Select>`'s own option labels).

---

## 9. Tests added

New file `NeuroStrokeCare.Tests/Phase11Tests.cs` (9 tests), extending `TestDataHelper.SeedPatientAsync` with optional parameters rather than touching any existing call site:

- `CreatePatient_DuplicateHospitalNumber_Rejected`, `CreatePatient_TwoPatientsWithNoHospitalNumber_BothSucceed` — the filtered unique index allows unlimited `NULL`s but rejects a real duplicate.
- `UpdatePatient_ToAnotherPatientsHospitalNumber_Rejected`, `UpdatePatient_KeepingItsOwnHospitalNumber_Succeeds` — the `excludingPatientId` exclusion works both ways.
- `Search_RequiresAuthentication_Returns401` — the new `[Authorize]` on `Search` (see §3's note on why there's no "wrong role" case to test here).
- `Search_MatchesHospitalNumberBeforeNationalId` — asserts the actual result ordering, not just that both rows are present.
- `Search_ExcludesSoftDeletedAdmission`, `Search_ExcludesAdmissionOfSoftDeletedPatient` — area 16's two distinct soft-delete paths (the admission itself, and the patient it belongs to).
- `Search_OpenOnlyDefault_ExcludesDischargedAdmission_ButIncludesWhenOpenOnlyFalse` — both the default clinical-write behavior and the historical-search override.
- `Search_TakeIsClampedToAtLeastOne` — the `Math.Clamp(take, 1, 50)` lower bound.

No existing Phase 9/10 test file was modified, deleted, or weakened.

---

## 10. Frontend build result

```
npx tsc -b --force       → clean, no errors
npm run build (vite)     → succeeded, 539 modules transformed
  dist/assets/index-*.js  ≈ 612 kB (pre-existing >500kB chunk-size warning, unrelated to this phase)
```
Re-run as the final step of this phase, after every change described above.

---

## 11. Backend build/test result

Unchanged limitation from Phase 9/10: `dotnet`/`dotnet ef` are unreachable in this sandbox (`packages.microsoft.com` blocked by the network allowlist — confirmed again this phase: `curl` to it returns no response). **No backend compile or test run was possible.** Every C# change above (including the new migration and `Phase11Tests.cs`) was written by hand and checked only via careful reading of existing patterns plus brace-balance scripts (`{`/`}` counts matched on every touched/new file). The previous phase's independent validation signal — a `dotnet ef migrations add` run by you producing an *empty* migration (`20261002152126_staff_profiles`) — is still the best available evidence that this hand-written-migration approach has been tracking the real model correctly; the same verification is recommended again after this phase (`dotnet ef migrations add Phase11Check` should also come up empty if `20261002154700_AddPatientHospitalNumber` and the `NeuroFlowDbContextModelSnapshot.cs` edit are consistent).

---

## 12. Remaining risks

- **Backend never compiled** (§11) — the single biggest risk. Please run `dotnet build` / `dotnet test` on your machine before trusting any backend behavior here beyond careful reading.
- **Stroke Code active/resolved ambiguity** (§7) — by design of the existing data model, not fixable without a schema change beyond this phase's scope; flagged rather than silently accepted.
- **`AnyClinical` has no deny case for the new `[Authorize]` additions** (§3, §6) — both `PatientController`'s and `Search`'s new role boundaries are real gaps closed, but because `AnyClinical` spans all 6 roles, there's no role-based 403 to regression-test for either; only "unauthenticated" is verifiable here.
- **`PatientContextHeader`'s age/sex/stroke-type/Stroke-Code enrichment fetch** depends on three endpoints (`Admission` by id, `Patient` by id, `DoorTiming/paged`) succeeding; a failure there silently drops the extra fields (the header still shows name/Hospital Number/ward/bed/status from the snapshot) rather than erroring the whole header — a deliberate choice, but worth knowing about if those fields seem to be "missing" during testing.
- **`git diff` on the whole repo still shows the pre-existing CRLF/LF checkout artifact** documented in `PHASE9_REPORT.md`'s appendix (~100+ files, equal insertion/deletion counts) — not caused or fixed by this phase.
