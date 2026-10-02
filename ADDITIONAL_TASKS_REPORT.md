# Additional Clinical UX & Staff Profile Tasks — Delivery Report

Repository: `D:\Programming\Projects\NeuroStrokeCare` (verified via `git rev-parse --show-toplevel` before and after every change in this phase).
Scope: the "ADDITIONAL CLINICAL UX & STAFF PROFILE TASKS" spec, additive to `PHASE9_REPORT.md`. No commit, no push, no deploy was performed — every change below is sitting in the working tree for your own review (`git status` / `git diff`).

All statements below were verified by reading the real source files through the device bridge before writing anything; where something in the spec assumed functionality that doesn't exist in the repo, that discrepancy is called out instead of invented.

---

## 1. Files changed

**Backend (not yet build-verified — see §10):**
- `NeuroStrokeCare.Data/UserApplication/ApplicationUser.cs` — 4 new nullable fields.
- `NeuroStrokeCare.Service/Auth/Dtos/UserSummaryResponse.cs`, `UpdateUserAdminRequest.cs` — same 4 fields.
- `NeuroStrokeCare.Service/Auth/IAuthService.cs`, `AuthService.cs` — `UpdateUserAdminAsync` extended; `GetAllUsersAsync`/`GetMyProfileAsync` return the new fields.
- `NeuroStrokeCare.api/Controllers/AuthController.cs` — passes the new fields through to the service.
- `NeuroStrokeCare.api/Controllers/AdmissionController.cs` — new `GET /api/admission/search` endpoint + `MaskNationalId` helper.
- `NeuroStrokeCare.api/Controllers/DoorTimingController.cs` — `NeuroFlowDbContext` injected; `AuditLog` writes added to `Create` (activation) and `ChangeStatus` (stand-down).
- `NeuroStrokeCare.Core/Features/Admission/Dtos/AdmissionSearchResultResponse.cs` — **new file**.
- `NeuroStrokeCare.infrastructure/Migrations/20261002070000_AddStaffProfileFields.cs` + `.Designer.cs` — **new migration**, written by hand (see §6).
- `NeuroStrokeCare.infrastructure/Migrations/NeuroFlowDbContextModelSnapshot.cs` — updated to match.

**Frontend (type-checked and production-built — see §10):**
- `NeuroStrokeCare.client/src/types/entities.ts` — new fields on `UserSummaryResponse`, new `AdmissionSearchResultResponse` type.
- `NeuroStrokeCare.client/src/lib/usersApi.ts` — `updateAdmin` extended, new `updateProfile`.
- `NeuroStrokeCare.client/src/lib/admissionSearchApi.ts` — **new file**, thin client for the search endpoint.
- `NeuroStrokeCare.client/src/components/AdmissionPicker.tsx` — **new file**, the searchable combobox.
- `NeuroStrokeCare.client/src/pages/Assessments.tsx`, `LabResults.tsx`, `FollowUp.tsx` — admission `<select>` replaced with `AdmissionPicker`.
- `NeuroStrokeCare.client/src/pages/DoorTiming.tsx` — added a required confirmation checkbox before "Activate"; admission select **intentionally left as-is** (see §7).
- `NeuroStrokeCare.client/src/pages/Users.tsx` — Admin "Edit" modal now also edits Profession/JobTitle/AcademicDegree/Department.
- `NeuroStrokeCare.client/src/pages/IdCard.tsx` — shows the new staff fields (when set) and adds a self-service "My profile" editor.

A clean `git diff --stat` limited to exactly these files: **17 files changed, 522 insertions(+), 59 deletions(-)**, plus the 5 new files above. (`git diff --stat` on the *whole* repo shows ~100 unrelated files and the old migrations with huge, suspicious diffs — this is the same pre-existing CRLF/LF checkout artifact documented in `PHASE9_REPORT.md`'s appendix: every one of those files has **equal** insertion/deletion counts, confirmed with `git diff --numstat`, i.e. no real content changed. Nothing in this phase caused or fixed that; it's a line-ending difference between this Linux-side checkout and your Windows one.)

---

## 2. Database / entity changes

`ApplicationUser` gains 4 nullable `string?` columns: `Profession`, `JobTitle`, `AcademicDegree`, `Department`. All default to `null` for every existing user — nothing is backfilled or inferred.

## 3. API endpoints added/changed

- **New:** `GET /api/admission/search?q=&openOnly=true&take=20` — read-only typeahead, capped at 50 rows, never returns a soft-deleted patient or admission. See §9 for the "hospital number" discrepancy.
- **Changed:** `PUT /api/auth/users/{id}` (`UpdateUserAdminRequest`) now also accepts `profession`/`jobTitle`/`academicDegree`/`department` — still `[Authorize(Roles = "Admin")]`, unchanged.
- **Unchanged, now actually used by the frontend:** `PUT /api/auth/profile` already existed but had zero frontend caller before this phase (see §9) — `IdCard.tsx`'s new "My profile" editor now calls it.
- `POST /api/doortiming` and `PATCH /api/doortiming/{id}/status` behave the same from the caller's point of view (same request/response shapes); they now also write an `AuditLog` row as a side effect.

## 4. Frontend changes

Searchable combobox (`AdmissionPicker`) on Assessments / Lab Results / Follow-up Notes, replacing the "load every active admission" `<select>`. Staff ID Card extended with Profession/Job title/Degree/Department (blank ones omitted) and a new self-service "My profile" editor (first/last name, email, phone only). Admin "Staff" page can set the 4 new fields. Door Timing's "Activate stroke code" now requires a confirmation checkbox.

## 5. Role / security behavior

- Nothing was loosened. `AdmissionController.Search` carries the same authentication requirement as every other GET on that controller (global `FallbackPolicy` — any authenticated user); it filters out soft-deleted patients/admissions and, by default, discharged admissions.
- The 4 new staff fields are editable **only** through the existing Admin-only `PUT /api/auth/users/{id}` path. The self-service `PUT /api/auth/profile` DTO (`UpdateProfileRequest`) was **not touched** and still only carries FirstName/LastName/Email/PhoneNumber — there is no way for a user to set their own EmployeeId/Role/Profession/JobTitle/AcademicDegree/Department. No new role or permission was invented anywhere.
- Reviewed Admissions.tsx per area 8: the "Transfer / update status" button and the stroke-type button are already gated on `canManage && isOpenAdmission(a)` (`canManage = isDoctorRole(user?.role)`), and that's the *only* place in the frontend that changes an admission's status — there is no raw, ungated status dropdown anywhere. **No change was needed here.**
- `DoorTimingController` still has **no** `[Authorize(Roles=...)]` restriction (relies only on the global fallback policy, same as before) — any authenticated user can activate/stand down a stroke code. The task said not to invent new role permissions, so this wasn't changed, but it's worth your explicit decision: if only clinical staff should activate a stroke code, that's a one-line `[Authorize(Roles = Roles.AnyClinical)]` addition on `Create`/`ChangeStatus`.

## 6. Migration required

Yes — `AddStaffProfileFields` (`20261002070000`), adding 4 nullable `nvarchar(max)` columns to `AspNetUsers`. **Written by hand**, following the exact `AddColumn<string>` pattern used by the existing `FixDecimalPrecision` migration for `EmployeeId`/`ProfilePhotoUrl` (same table, same shape), because no `dotnet`/`dotnet ef` tooling is reachable from this session (see §10). Before trusting it against a real database: run `dotnet build` then `dotnet ef migrations has-pending-model-changes` locally to confirm the hand-written migration and model snapshot actually match what EF would generate, then `dotnet ef database update`.

Unrelated side note found while reviewing the Migrations folder: the two most recent pre-existing migrations, `AddUserApprovalFields` and `Neuroroles`, both have **completely empty** `Up()`/`Down()` bodies. That's benign (the real `IsApproved`/`RequestedRole` columns were actually added one migration earlier, in `SyncLatestModel`) but they're dead weight — worth squashing next time you touch migrations.

## 7. Stroke Code implementation status

**Already substantially implemented before this phase** — not built from scratch. `DoorTiming.tsx` already has a prominent "+ Activate stroke code" button, `alertsApi.ts` already has a dedicated "Stroke Code" alert category watching the 25/60-minute Door-to-CT/needle windows, and the `Admission`↔`DoorTiming` 1-to-1 FK already prevents duplicate activations structurally. What was missing, and is now added:
- An `AuditLog` entry on activation (`StrokeCodeActivated`) and stand-down (`StrokeCodeStoodDown`/`StrokeCodeReactivated`), recording the admission, timestamp, and activating user.
- An explicit confirmation checkbox before the "Activate" button becomes clickable.

**Caveat, stated plainly rather than hidden:** the `AuditLog` write happens in a **second, separate `SaveChangesAsync`** after the primary MediatR command succeeds — not atomic with it. This is the same limitation every other generic-CRUD controller already has (none of them have their own `DbContext` injected); making it fully atomic would mean rewriting `Create`/`ChangeStatus` as fully custom handlers (like `AdmissionController.Transfer`), which felt like more surgery than this task asked for. Flagging it rather than quietly shipping a false "atomic" claim.

No new real-time infrastructure (SignalR/queues/push) was added or needed — "notification" here still means exactly what it already meant: a client-side alert computed by polling, in `alertsApi.ts`.

## 8. Status Epilepticus implementation status

**Fully implemented already, found during investigation, nothing was changed.** `src/lib/doseCalculator.ts` has a complete `calculateSeizureDose` (Lorazepam/Phenytoin, mg/kg, capped at the protocol's max total dose, administration notes, repeat-dosing note), and it's live-wired into `Patients.tsx` via a "Seizure dose" button opening a modal titled "Status epilepticus dose — {name}", structurally identical to the existing thrombolysis dose modal. Per the task's own safety rule — never invent or touch dosing logic without an approved protocol — **this was left untouched**. If this protocol's source/approval needs to be re-confirmed with your clinical team, that's a question for you, not something this phase can verify from the repo alone.

## 9. Password reset findings (read-only review, no emails sent)

The end-to-end flow is **correctly designed** as implemented:
1. `ForgotPasswordAsync` never reveals whether an email is registered (`"If this email is registered, a reset link will be sent"` either way).
2. Uses ASP.NET Identity's own `GeneratePasswordResetTokenAsync` — a standard, single-use, time-limited token (default lifespan, not overridden anywhere in `Program.cs`).
3. The reset link is built from `Email:FrontendResetUrl` in config, with a `localhost:5173` **development** fallback.
4. `ResetPasswordAsync` consumes the token via `ResetPasswordAsync` (Identity invalidates it on use) and also clears any lockout/failed-attempt counters on success.
5. Neither the token nor the new password is ever written to a log — only `To`/`Subject` are logged, and only when SMTP isn't configured at all (see below).
6. `ResetPassword.tsx` correctly reads `email`/`token` from the URL and shows a clear "link is invalid" state if either is missing.

**Production gap, not a code bug:** `appsettings.json`'s `Email` section has every SMTP field empty (`SmtpHost`, `SmtpUsername`, `SmtpPassword`, `FromAddress` all `""`), and there is no `appsettings.Production.json` in the repo at all. As written, `SmtpEmailService` detects this and **only logs a warning instead of sending**, so right now, in whatever environment this is deployed to, forgot-password emails silently never arrive unless real SMTP credentials and `Email:FrontendResetUrl` (pointing at your real production frontend URL, not `localhost:5173`) are supplied via `appsettings.Production.json` or environment variables on the VPS. No test email was sent as part of this review, per your instruction.

**Search endpoint discrepancy worth flagging explicitly (this also applies to area 1):** the spec asked for search priority "hospital number → National ID → name." There is **no hospital-number/MRN field anywhere in the domain** — `Patient` only has `NationalId` (nullable) plus its own internal Guid. Grepped the whole solution for `HospitalNumber`/`MRN`/`FileNumber`/`PatientNumber` — zero matches. Rather than inventing that field (which would mean a bigger schema/UI change: deciding who assigns it, whether it's unique, whether it's required), `AdmissionController.Search` searches NationalId and name only. If a real hospital-number/MRN concept should exist, that's a decision for you — happy to add it as its own follow-up with a proper migration once you confirm the format/uniqueness rules.

## 10. Build/test results

- **Frontend type check:** `npx tsc -b --force` — clean, no errors, after every edit in this phase.
- **Frontend production build:** `npm run build` (`tsc -b && vite build`) — **succeeded** (534 modules, ~591 kB main bundle, only a pre-existing "chunk is large" advisory warning, unrelated to this phase). Note: this build initially failed in this sandbox with a missing native `@rolldown/binding-linux-x64-gnu` module (an environment/npm-optional-dependency gap, not a code issue); installing that one package fixed it. Your own machine almost certainly already has it and won't hit this.
- **Backend build/test:** **not executed** — same constraint as `PHASE9_REPORT.md`: no `dotnet` runtime/SDK is reachable from this session (`packages.microsoft.com` is blocked by this environment's network allowlist, confirmed again this phase with a fresh `curl` — 403). Every C# change above was written by directly reading the real source files first (entity/DTO/enum member names, existing patterns like `AdmissionController.Transfer`'s `AuditLog` block, the `FixDecimalPrecision` migration's column-add shape) and sanity-checked with a brace-balance script, but **none of it has been compiled**. Please run `dotnet build` and the Phase 9 test suite (`dotnet test NeuroStrokeCare.Tests`) locally before trusting this against a real database or deploying it.

## 11. Remaining risks

- All backend changes are uncompiled (see §10) — review them like a PR, not like tested code.
- The hand-written EF migration should be double-checked with `dotnet ef migrations has-pending-model-changes` before `database update`.
- `DoorTimingController`'s `AuditLog` write is not atomic with the activation/stand-down write (§7).
- `DoorTimingController` still has no role restriction at all (§5) — confirm whether that's intentional.
- Password reset emails will not actually send until real SMTP config + `Email:FrontendResetUrl` are set for production (§9).
- No real "hospital number" field exists; search currently covers NationalId + name only (§9).
- Door Timing's admission picker was deliberately **not** converted to the new search combobox — its existing `admissionsWithoutOpenCode` filter also excludes admissions that already have an open stroke code, which the generic search endpoint has no concept of. Converting it would have required re-implementing that safety-critical exclusion against search results rather than the full in-memory list, which felt riskier than leaving a dropdown that's already naturally small (open admissions minus ones already coded).
