using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NeuroStrokeCare.Core.Features.Admission.Dtos;
using NeuroStrokeCare.Core.Features.BaseService.Commands.Models;
using NeuroStrokeCare.Core.Features.BaseService.Queries.Models;
using NeuroStrokeCare.Data.AuditLogModel;
using NeuroStrokeCare.Data.Entities;
using NeuroStrokeCare.Data.Constants;
using NeuroStrokeCare.Data.Enums;
using NeuroStrokeCare.Data.PageModel;
using NeuroStrokeCare.infrastructure.Context;
using Microsoft.Data.SqlClient;
using System.Text.RegularExpressions;

namespace NeuroStrokeCare.api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AdmissionController : ControllerBase
    {
        #region Fields
        private readonly IMediator _mediator;
        private readonly IMapper _mapper;
        private readonly NeuroFlowDbContext _context;
        #endregion

        #region Constructor
        public AdmissionController(IMediator mediator, IMapper mapper, NeuroFlowDbContext context)
        {
            _mediator = mediator;
            _mapper = mapper;
            _context = context;
        }
        #endregion

        #region Selectors
        private static readonly System.Linq.Expressions.Expression<Func<Admission, AdmissionResponse>> ToResponse =
            a => new AdmissionResponse
            {
                Id = a.Id,
                PatientId = a.PatientId,
                PatientName = a.Patient.FirstName + " " + a.Patient.LastName,
                PatientHospitalNumber = a.Patient.HospitalNumber,
                AdmissionTime = a.AdmissionTime,
                Status = a.Status,
                StrokeType = a.StrokeType,
                StrokeTypeSetAt = a.StrokeTypeSetAt,
                BedId = a.BedId,
                DischargeTime = a.DischargeTime,
                DischargeNotes = a.DischargeNotes,
                AdmittedById = a.AdmittedById,
                StrokeTypeSetById = a.StrokeTypeSetById,
                CTDone = a.CTDone,
                MRIDone = a.MRIDone,
                CTADone = a.CTADone,
                CTFindings = a.CTFindings,
                MRIFindings = a.MRIFindings,
                CTAFindings = a.CTAFindings,
                ThrombolysisGivenAt = a.ThrombolysisGivenAt,
                ThrombolysisDrug = a.ThrombolysisDrug,
                ThrombolysisDoseMg = a.ThrombolysisDoseMg,
                ThrombolysisRecordedById = a.ThrombolysisRecordedById,
                RowVersion = a.RowVersion
            };
        #endregion

        // بنحدد تعارض الـ Unique Index بالاسم الصريح للإندكس نفسه (مش بس "أي DbUpdateException")
        // عشان منلخّصش أخطاء قاعدة بيانات تانية (زي انتهاك Foreign Key) على إنها تعارض سرير/مريض
        // غلط. 2601 = "Cannot insert duplicate key row ... with unique index '...'" في SQL Server.
        private static bool IsUniqueIndexViolation(DbUpdateException ex, string indexName) =>
            ex.InnerException is SqlException sqlEx
            && sqlEx.Number == 2601
            && sqlEx.Message.Contains(indexName, StringComparison.OrdinalIgnoreCase);

        // GET: api/admission
        [HttpGet]
        public async Task<ActionResult<List<AdmissionResponse>>> GetAll(CancellationToken cancellationToken)
        {
            var query = new GetListWithFiltersQuery<Admission, AdmissionResponse>(
                filter: a => a.CurrentState == (int)CurrentStatusType.Active,
                selector: ToResponse,
                orderBy: a => a.AdmissionTime);

            var result = await _mediator.Send(query, cancellationToken);
            return Ok(result);
        }

        // GET: api/admission/paged?pageNumber=1&pageSize=10&patientId=&status=
        [HttpGet("paged")]
        public async Task<ActionResult<PagedResult<AdmissionResponse>>> GetPaged(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] Guid? patientId = null,
            [FromQuery] PatientStatus? status = null,
            CancellationToken cancellationToken = default)
        {
            var query = new GetPagedListQuery<Admission, AdmissionResponse>(
                filter: a => a.CurrentState == (int)CurrentStatusType.Active &&
                             (patientId == null || a.PatientId == patientId) &&
                             (status == null || a.Status == status),
                selector: ToResponse,
                orderBy: a => a.AdmissionTime,
                pageNumber: pageNumber,
                pageSize: pageSize);

            var result = await _mediator.Send(query, cancellationToken);
            return Ok(result);
        }

        // GET: api/admission/search?q=...&openOnly=true&take=20
        // PHASE 11 (areas 1/2/15/16): minimal read-only typeahead search for the clinical
        // forms that used to load every active admission into one giant <select>
        // (Assessments, Lab Results, Door Timing activation, Follow-up Notes).
        //
        // Search priority is now HospitalNumber -> NationalId -> name, as this phase asked -
        // the "no hospital number field exists" discrepancy from the previous phase's report
        // is resolved: HospitalNumber now exists on Patient (see PatientController/the
        // migration). Priority is implemented as a match-rank in the ORDER BY (0 = matched on
        // HospitalNumber, 1 = NationalId, 2 = name/no term), translated to a SQL CASE
        // expression by EF Core - not a two-pass or client-side sort.
        //
        // - [Authorize(Roles = Roles.AnyClinical)] added this phase - every other clinical
        //   read/write endpoint in the solution already requires at least AnyClinical/
        //   AnyDoctor/AnyNurse; this one previously relied on nothing but the global
        //   authenticated-user fallback policy, which is the gap area 16 asked to close.
        // - Never returns a soft-deleted Patient or Admission (CurrentState filter on both).
        // - "openOnly" (default true) restricts results to admissions with no DischargeTime,
        //   matching isOpenAdmission() on the frontend - "only active/open admissions
        //   selectable for new clinical writes". Historical/read-only callers (e.g. Report,
        //   Timeline) pass openOnly=false to also reach discharged admissions.
        // - Only the fields the picker needs are returned (AdmissionSearchResultResponse) -
        //   never a full Patient/Admission record.
        // - Database-side filtering throughout (AsNoTracking, no in-memory Where/Contains) -
        //   capped result count (clamped 1-50), deterministic ordering (match rank, then most
        //   recent admission first), no pagination. An empty/missing `q` still only returns
        //   up to `take` rows (most recent open admissions), never the whole table.
        [Authorize(Roles = Roles.AnyClinical)]
        [HttpGet("search")]
        public async Task<ActionResult<List<AdmissionSearchResultResponse>>> Search(
            [FromQuery] string? q,
            [FromQuery] bool openOnly = true,
            [FromQuery] int take = 20,
            CancellationToken cancellationToken = default)
        {
            take = Math.Clamp(take, 1, 50);
            var term = q?.Trim();
            var hasTerm = !string.IsNullOrWhiteSpace(term);

            var query = _context.Set<Admission>()
                .AsNoTracking()
                .Where(a => a.CurrentState == (int)CurrentStatusType.Active &&
                            a.Patient.CurrentState == (int)CurrentStatusType.Active);

            if (openOnly)
                query = query.Where(a => a.DischargeTime == null);

            if (hasTerm)
            {
                query = query.Where(a =>
                    (a.Patient.HospitalNumber != null && a.Patient.HospitalNumber.Contains(term)) ||
                    (a.Patient.NationalId != null && a.Patient.NationalId.Contains(term)) ||
                    a.Patient.FirstName.Contains(term) ||
                    a.Patient.LastName.Contains(term) ||
                    (a.Patient.FirstName + " " + a.Patient.LastName).Contains(term));
            }

            var results = await query
                // Match-rank first (HospitalNumber=0, NationalId=1, name/no-term=2), then most
                // recent admission first within the same rank.
                .OrderBy(a => hasTerm && a.Patient.HospitalNumber != null && a.Patient.HospitalNumber.Contains(term)
                    ? 0
                    : hasTerm && a.Patient.NationalId != null && a.Patient.NationalId.Contains(term)
                        ? 1
                        : 2)
                .ThenByDescending(a => a.AdmissionTime)
                .Take(take)
                .Select(a => new AdmissionSearchResultResponse
                {
                    AdmissionId = a.Id,
                    PatientId = a.PatientId,
                    PatientName = a.Patient.FirstName + " " + a.Patient.LastName,
                    HospitalNumber = a.Patient.HospitalNumber,
                    NationalIdMasked = a.Patient.NationalId,
                    Status = a.Status,
                    IsOpen = a.DischargeTime == null,
                    WardCode = a.Bed != null ? a.Bed.Ward.Code : null,
                    WardName = a.Bed != null ? a.Bed.Ward.Name : null,
                    BedNumber = a.Bed != null ? a.Bed.BedNumber : null,
                    AdmissionTime = a.AdmissionTime
                })
                .ToListAsync(cancellationToken);

            // Masking is plain string logic, not translatable to SQL - done after
            // materializing the (already capped-to-`take`-rows) result set.
            foreach (var r in results)
                r.NationalIdMasked = MaskNationalId(r.NationalIdMasked);

            return Ok(results);
        }

        // GET: api/admission/patient-search?query=...
        // Used only by the New Admission flow: find active patients by HospitalNumber/NationalId
        // without loading the whole patient table into the browser. Results are restricted to
        // patients who do not already have an active/open admission; Create re-checks the same
        // rule before saving, so the UI cannot be the only line of defense.
        [Authorize(Roles = Roles.AnyDoctor)]
        [HttpGet("patient-search")]
        public async Task<ActionResult<List<AdmissionPatientSearchResultResponse>>> PatientSearch(
            [FromQuery] string? query,
            [FromQuery] int take = 10,
            CancellationToken cancellationToken = default)
        {
            var term = query?.Trim();
            if (string.IsNullOrWhiteSpace(term) || term.Length < 2)
                return Ok(new List<AdmissionPatientSearchResultResponse>());

            take = Math.Clamp(take, 1, 20);

            var exactPatientId = await _context.Set<Patient>()
                .AsNoTracking()
                .Where(p => p.CurrentState == (int)CurrentStatusType.Active &&
                            (p.HospitalNumber == term || p.NationalId == term))
                .Select(p => (Guid?)p.Id)
                .FirstOrDefaultAsync(cancellationToken);

            if (exactPatientId.HasValue)
            {
                var exactPatientHasOpenAdmission = await _context.Set<Admission>().AnyAsync(
                    a => a.PatientId == exactPatientId.Value &&
                         a.CurrentState == (int)CurrentStatusType.Active &&
                         a.DischargeTime == null,
                    cancellationToken);

                if (exactPatientHasOpenAdmission)
                    return Conflict(new { message = "Patient already has an active admission." });
            }

            var patients = await _context.Set<Patient>()
                .AsNoTracking()
                .Where(p => p.CurrentState == (int)CurrentStatusType.Active)
                .Where(p =>
                    (p.HospitalNumber != null && p.HospitalNumber.Contains(term)) ||
                    (p.NationalId != null && p.NationalId.Contains(term)))
                .Where(p => !_context.Set<Admission>().Any(a =>
                    a.PatientId == p.Id &&
                    a.CurrentState == (int)CurrentStatusType.Active &&
                    a.DischargeTime == null))
                .OrderBy(p => p.HospitalNumber == term
                    ? 0
                    : p.NationalId == term
                        ? 1
                        : p.HospitalNumber != null && p.HospitalNumber.StartsWith(term)
                            ? 2
                            : p.NationalId != null && p.NationalId.StartsWith(term)
                                ? 3
                                : 4)
                .ThenBy(p => p.LastName)
                .ThenBy(p => p.FirstName)
                .Take(take)
                .Select(p => new AdmissionPatientSearchResultResponse
                {
                    PatientId = p.Id,
                    FullName = p.FirstName + " " + p.MiddleName + " " + p.LastName,
                    HospitalNumber = p.HospitalNumber,
                    NationalIdMasked = p.NationalId
                })
                .ToListAsync(cancellationToken);

            foreach (var patient in patients)
                patient.NationalIdMasked = MaskNationalId(patient.NationalIdMasked);

            return Ok(patients);
        }

        // بيسيب آخر 4 خانات بس ظاهرة - "•••••••1234" - مستخدمة في نتائج البحث فوق فقط،
        // الـ Endpoints التانية (GetById بتاع Patient مثلاً) لسه بترجع الرقم القومي كامل
        // لمن عنده صلاحية يشوف الملف الكامل.
        private static string? MaskNationalId(string? nationalId)
        {
            if (string.IsNullOrEmpty(nationalId))
                return nationalId;

            if (nationalId.Length <= 4)
                return new string('•', nationalId.Length);

            return new string('•', nationalId.Length - 4) + nationalId[^4..];
        }

        // GET: api/admission/{id}
        [HttpGet("{id:guid}")]
        public async Task<ActionResult<AdmissionResponse>> GetById(Guid id, CancellationToken cancellationToken)
        {
            // FINAL RELEASE-CANDIDATE PASS (section 6, "Soft delete integrity"): CONFIRMED BUG,
            // FIXED HERE. This GetById used to filter on Id alone, so a soft-deleted row
            // (CurrentState != Active) was still returned as a normal 200 through this exact
            // same endpoint any ordinary clinical read uses - there was no separate "admin can
            // still see deleted records" endpoint being bypassed here, this WAS the only read
            // path, and it did not distinguish. Adding the CurrentState check makes a
            // soft-deleted record 404 here, consistent with GetAll/paged (which already filter
            // on CurrentState) and with how every write endpoint already treats "not found".
            var query = new GetByIdWithFiltersQuery<Admission, AdmissionResponse>(
                filter: a => a.Id == id && a.CurrentState == (int)CurrentStatusType.Active,
                selector: ToResponse);

            var result = await _mediator.Send(query, cancellationToken);
            if (result == null)
                return NotFound();

            return Ok(result);
        }

        // POST: api/admission?actingUserId=...
        // زي Transfer/SetStrokeType/RecordThrombolysis، الـ Create ده بيتجاوز الـ Add العام
        // (AddAsyncGetIDCommand) عمدًا ويستخدم _context مباشرة، عشان لازم فحصين ينجحوا مع
        // إنشاء الإدخال في نفس الحفظة (SaveChanges واحدة = معاملة واحدة ذرية):
        //   - المريض ده معندوش إدخال مفتوح (DischargeTime == null) بالفعل - مينفعش يكون
        //     عنده إدخالين نشطين في نفس الوقت (Conflict 409 لو عنده).
        //   - لو تم تحديد سرير: السرير ده "شاغر" فعلاً (Conflict 409 لو مش شاغر، NotFound لو
        //     غير موجود) وبيتحجز (BedStatus.Occupied) فورًا في نفس الحفظة.
        // ملحوظة: زي ما Add العام بيعمل (LogAndAudit يتجاهل "Created" عمدًا)، معمول هنا نفس
        // الحاجة - الإدخال نفسه فيه CreatedBy/CreatedAt، فمفيش صف Audit إضافي لإنشائه.
        [Authorize(Roles = Roles.AnyDoctor)]
        [HttpPost]
        public async Task<ActionResult<Guid>> Create(
            [FromBody] CreateAdmissionRequest request,
            [FromQuery] Guid actingUserId,
            CancellationToken cancellationToken)
        {
            if (request.AdmissionTime == default)
                return BadRequest(new { message = "Admission time is required." });

            var patientExists = await _context.Set<Patient>().AnyAsync(
                p => p.Id == request.PatientId && p.CurrentState == (int)CurrentStatusType.Active,
                cancellationToken);

            if (!patientExists)
                return NotFound(new { message = "Patient not found." });

            var hasOpenAdmission = await _context.Set<Admission>().AnyAsync(
                a => a.PatientId == request.PatientId &&
                     a.CurrentState == (int)CurrentStatusType.Active &&
                     a.DischargeTime == null,
                cancellationToken);

            if (hasOpenAdmission)
                return Conflict(new { message = "Patient already has an active admission." });

            Bed? bed = null;
            if (request.BedId.HasValue)
            {
                bed = await _context.Set<Bed>().FirstOrDefaultAsync(b => b.Id == request.BedId.Value, cancellationToken);
                if (bed == null)
                    return NotFound(new { message = "Bed not found." });

                if (bed.Status != BedStatus.Vacant)
                    return Conflict(new { message = "This bed is already occupied or reserved — pick another bed." });
            }

            var admission = _mapper.Map<Admission>(request);
            admission.AdmittedById = actingUserId;
            admission.CreatedBy = actingUserId;
            admission.CreatedAt = DateTime.UtcNow;
            admission.CurrentState = (int)CurrentStatusType.Active;

            _context.Set<Admission>().Add(admission);

            if (bed != null)
            {
                bed.Status = BedStatus.Occupied;
                bed.UpdatedBy = actingUserId;
                bed.UpdatedAt = DateTime.UtcNow;
            }

            try
            {
                await _context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException ex)
            {
                // شبكتين أمان أخيرتين على مستوى قاعدة البيانات، لكل فحص فوق Unique Index بتاعه:
                //   - IX_Admissions_BedId: لو حد سبقنا لنفس السرير في نفس اللحظة بالظبط.
                //   - IX_Admissions_PatientId (Unique + Filtered على DischargeTime/CurrentState):
                //     لو حد سبقنا بإدخال مفتوح لنفس المريض في نفس اللحظة بالظبط (الفحص AnyAsync
                //     فوق بيغطي الحالة العادية، ده الحماية الأخيرة من الـ Race Condition).
                // أي DbUpdateException تاني (مش من الاندكسين دول) بيتعاد رميه زي ما هو - مش كل
                // خطأ قاعدة بيانات معناه تعارض سرير أو مريض.
                if (IsUniqueIndexViolation(ex, "IX_Admissions_BedId"))
                    return Conflict(new { message = "This bed was just taken by another admission — pick a different bed." });

                if (IsUniqueIndexViolation(ex, "IX_Admissions_PatientId"))
                    return Conflict(new { message = "Patient already has an active admission." });

                throw;
            }

            return CreatedAtAction(nameof(GetById), new { id = admission.Id }, admission.Id);
        }

        // PUT: api/admission?actingUserId=...
        // ملحوظة أمان: PUT ده Update أعمى (بيستبدل كل الحقول) - معندوش أي تحقق من إشغال
        // السرير أو تحرير القديم زي /transfer. عشان منفتحش نفس ثغرة حجز نفس السرير مرتين
        // من هنا، بنمنع أي تغيير في BedId من الطريق ده خالص - أي نقل/تغيير سرير لازم يعدي
        // على PATCH /{id}/transfer اللي فيها كل الحماية (فحص الإشغال + قفل الداتابيز).
        [Authorize(Roles = Roles.AnyDoctor)]
        [HttpPut]
        public async Task<IActionResult> Update(
            [FromBody] UpdateAdmissionRequest request,
            [FromQuery] Guid actingUserId,
            CancellationToken cancellationToken)
        {
            var current = await _context.Set<Admission>()
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.Id == request.Id, cancellationToken);

            if (current == null)
                return NotFound();

            if (current.BedId != request.BedId)
            {
                return BadRequest(new
                {
                    message = "Bed changes must go through PATCH /admission/{id}/transfer, which safely checks vacancy - not this general update."
                });
            }

            var admission = _mapper.Map<Admission>(request);

            // ثابتة من أول ما اتعمل الإدخال - مش بتتغير أبدًا بعد كده حتى لو حد تاني عدّل
            // بيانات الإدخال، عشان سجل "مين قبل المريض فعليًا" يفضل صحيح لأي مراجعة/تدقيق
            // لاحقة. اللي بيتغير مع كل تعديل هو UpdatedBy (بيتحط تلقائيًا من UpdateCommand).
            admission.AdmittedById = current.AdmittedById;

            // FINAL RELEASE-CANDIDATE PASS (section 2) - CONFIRMED BUG, FIXED HERE:
            // ThrombolysisGivenAt/ThrombolysisDrug/ThrombolysisDoseMg/ThrombolysisRecordedById
            // are NOT fields on UpdateAdmissionRequest, so AutoMapper always left them null/0 on admission
            // above - and because this PUT replaces the whole row via the generic
            // UpdateCommand (which only restores CreatedAt/CreatedBy/CurrentState, not these),
            // every ordinary Update call used to silently WIPE a recorded thrombolysis dose,
            // 100% of the time, regardless of what the frontend sent. These 4 fields have their
            // own dedicated, audited command (PATCH /{id}/thrombolysis, RecordThrombolysis below)
            // exactly like BedId has /transfer above - so this PUT must never be able to touch
            // them at all, not just "hope the caller echoes the right value back."
            admission.ThrombolysisGivenAt = current.ThrombolysisGivenAt;
            admission.ThrombolysisDrug = current.ThrombolysisDrug;
            admission.ThrombolysisDoseMg = current.ThrombolysisDoseMg;
            admission.ThrombolysisRecordedById = current.ThrombolysisRecordedById;

            // Same latent risk found on StrokeType/StrokeTypeSetAt/StrokeTypeSetById while fixing
            // the above: UpdateAdmissionRequest still carries these three (the comment on the DTO
            // says so explicitly - "until migrated to a dedicated command, kept here so we don't
            // lose data"), but SetStrokeType below IS already that dedicated command, and nothing
            // forces this PUT's caller to actually echo the current values back. Closing the same
            // gap the same way, now that both dedicated commands exist, is strictly safer than
            // leaving it to caller discipline.
            admission.StrokeType = current.StrokeType;
            admission.StrokeTypeSetAt = current.StrokeTypeSetAt;
            admission.StrokeTypeSetById = current.StrokeTypeSetById;

            // FINAL RELEASE-CANDIDATE PASS (section 4): optimistic concurrency. `admission` here
            // is a brand-new, never-tracked object (AutoMapper built it from `request` above) -
            // whatever we set on its RowVersion right before GenericRepository.UpdateAsync calls
            // _dbSet.Update(admission) becomes EF's "original value" for the concurrency check,
            // since EF has no earlier snapshot of its own for an entity it has never tracked.
            // Prefer the caller's own echoed RowVersion (true protection against edits made since
            // THEIR last read of this admission) and fall back to the value this request itself
            // just read in `current` above (still protects against a genuinely concurrent write
            // landing between that read and this save) when the caller didn't send one - see the
            // comment on UpdateAdmissionRequest.RowVersion for why this stays optional.
            admission.RowVersion = request.RowVersion ?? current.RowVersion;

            var command = new UpdateCommand<Admission>(admission, actingUserId);
            var affectedRows = await _mediator.Send(command, cancellationToken);

            if (affectedRows == -1)
                return NotFound();

            return NoContent();
        }

        // PATCH: api/admission/{id}/status?actingUserId=...&status=1
        // مفيش DELETE هنا برضو — Admission تاريخ مريض، الإلغاء بيتم بس عن طريق ChangeStatus (Soft Delete)
        [Authorize(Roles = Roles.AnyDoctor)]
        [HttpPatch("{id:guid}/status")]
        public async Task<IActionResult> ChangeStatus(
            Guid id,
            [FromQuery] Guid actingUserId,
            [FromQuery] int status,
            CancellationToken cancellationToken)
        {
            var command = new ChangeStatusCommand<Admission>(id, actingUserId, status);
            await _mediator.Send(command, cancellationToken);
            return NoContent();
        }

        // PATCH: api/admission/{id}/transfer?actingUserId=&newStatus=9&changeBed=true&newBedId=<guid>
        // الطريقة الآمنة الوحيدة لنقل مريض/تغيير حالته الفعلية (Admission.Status) - الـ PATCH
        // .../status التاني ده Soft-Delete عام بيغيّر CurrentState بس (زي باقي الكيانات)، مش
        // الحالة السريرية. الـ endpoint ده بيعمل كل حاجة في معاملة واحدة (Transaction واحدة عن
        // طريق SaveChanges واحدة + التقاط تعارض قاعدة البيانات):
        //   - لو السرير هيتغيّر: بيتأكد إن السرير الجديد "شاغر" فعلاً قبل ما يحجزه (لو حد سبقه
        //     برجع Conflict 409 بدل ما الاتنين ينجحوا)، وبيحرر السرير القديم يرجع "شاغر".
        //   - لو المريض بيتخرج أو بيتحول لمستشفى تاني (Discharged/TransferredOut): بيحرر السرير
        //     تلقائيًا حتى لو الطلب متقلش changeBed - عشان السرير متفضلش "متحجز" للأبد بالغلط
        //     (فيه Unique Index على Admission.BedId بيمنع إعادة استخدام سرير لسه متسجل لإدخال تاني).
        //   - بيسجل التغيير في الـ AuditLog (StatusChanged) عشان GET .../timeline يقدر يعرضه.
        [Authorize(Roles = Roles.AnyDoctor)]
        [HttpPatch("{id:guid}/transfer")]
        public async Task<IActionResult> Transfer(
            Guid id,
            [FromQuery] Guid actingUserId,
            [FromQuery] PatientStatus newStatus,
            [FromQuery] bool changeBed = false,
            [FromQuery] Guid? newBedId = null,
            CancellationToken cancellationToken = default)
        {
            var admission = await _context.Set<Admission>().FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
            if (admission == null)
                return NotFound();

            // قرار سياسة صريح: الإدخال اللي وصل لحالة نهائية (Discharged/TransferredOut) بيُقفل
            // للأبد. ممنوع يرجع لحالة غير نهائية، ممنوع ياخد سرير تاني، DischargeTime ما تُمحاش
            // أبدًا. مريض راجع للمستشفى تاني لازم يتعمله Admission جديد (Create) - "إعادة فتح"
            // الإدخال القديم غير مسموح بيها خالص. بنرفض أي محاولة تغيّر الحالة الحالية (حتى لو
            // كانت الحالة الجديدة المطلوبة نهائية هي كمان - زي Discharged -> TransferredOut -
            // ما فيش مطلب عمل واضح بيسمح بالتحويل بين الحالتين النهائيتين، فمبنخترعوش). الطلب
            // المكرر لنفس الحالة النهائية (Discharged -> Discharged) فقط هو المسموح (Idempotent).
            // الفحص ده قبل أي قراية/تعديل تاني عمدًا - عشان لو رفضنا، صفر تغيير يحصل.
            var currentIsTerminal = admission.Status == PatientStatus.Discharged || admission.Status == PatientStatus.TransferredOut;
            if (currentIsTerminal && newStatus != admission.Status)
            {
                return Conflict(new { message = "This admission is closed and cannot be reopened. Create a new admission for the patient." });
            }

            // مريض خارج من المستشفى نهائيًا - السرير لازم يتحرر حتى لو الطلب نسي يقول كده.
            // DischargeTime بتتسجل من السيرفر (UTC) هنا بس - مفيش أي قيمة من العميل (الفرونت)
            // بتتصدّق أبدًا. لو كانت مسجلة بالفعل (مثلاً الـ Transfer ده اتبعت تاني غلط على
            // إدخال خارج بالفعل) بنسيبها زي ما هي - القيمة التاريخية الأولى لا تُمسح ولا تُستبدل.
            var isLeavingHospital = newStatus == PatientStatus.Discharged || newStatus == PatientStatus.TransferredOut;
            if (isLeavingHospital)
            {
                changeBed = true;
                newBedId = null;

                if (admission.DischargeTime == null)
                    admission.DischargeTime = DateTime.UtcNow;
            }

            var oldBedId = admission.BedId;

            if (changeBed)
            {
                var bedIsChanging = newBedId != oldBedId;

                if (newBedId.HasValue && bedIsChanging)
                {
                    var newBed = await _context.Set<Bed>().FirstOrDefaultAsync(b => b.Id == newBedId.Value, cancellationToken);
                    if (newBed == null)
                        return NotFound("Bed not found.");

                    if (newBed.Status != BedStatus.Vacant)
                        return Conflict(new { message = "This bed is already occupied or reserved — pick another bed." });

                    newBed.Status = BedStatus.Occupied;
                    newBed.UpdatedBy = actingUserId;
                    newBed.UpdatedAt = DateTime.UtcNow;
                    admission.BedId = newBedId;
                }
                else if (!newBedId.HasValue)
                {
                    admission.BedId = null;
                }

                if (bedIsChanging && oldBedId.HasValue)
                {
                    var oldBed = await _context.Set<Bed>().FirstOrDefaultAsync(b => b.Id == oldBedId.Value, cancellationToken);
                    if (oldBed != null)
                    {
                        oldBed.Status = BedStatus.Vacant;
                        oldBed.UpdatedBy = actingUserId;
                        oldBed.UpdatedAt = DateTime.UtcNow;
                    }
                }
            }

            admission.Status = newStatus;
            admission.UpdatedBy = actingUserId;
            admission.UpdatedAt = DateTime.UtcNow;

            // لو الإدخال خرج من المستشفى دلوقتي، بنضيف DischargeTime لنص الـ Details - ده
            // بيوسّع الصيغة القديمة ("NewState=9") من غير ما يكسرها (GetTimeline تحت
            // بتقرا NewState بـ Regex دلوقتي بدل Split صارم، فالإضافة دي ما بتأثرش عليه).
            var details = $"NewState={(int)newStatus}";
            if (isLeavingHospital && admission.DischargeTime.HasValue)
                details += $";DischargeTime={admission.DischargeTime.Value:O}";

            // الـ AuditLog بيتضاف لنفس الـ _context قبل أي SaveChanges - عشان الحالة السريرية/
            // السرير وصف الأوديت يتسجلوا الكل في معاملة واحدة ذرية (SaveChangesAsync واحدة بس).
            // قبل كان ده في حفظتين منفصلتين (ممكن الحالة تتسجل والأوديت يفضل من غيرها لو فشلت
            // الحفظة التانية) - دلوقتي مستحيل يحصل ده: لو أي جزء فشل، الكل يرجع (Rollback).
            _context.Set<AuditLog>().Add(new AuditLog
            {
                EntityName = nameof(Admission),
                EntityId = id,
                Action = "StatusChanged",
                UserId = actingUserId,
                Details = details,
                Timestamp = DateTime.UtcNow
            });

            try
            {
                await _context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException ex)
            {
                // شبكة الأمان الأخيرة: لو اتنين حاولوا يحجزوا نفس السرير في نفس اللحظة بالظبط،
                // الـ Unique Index على Admission.BedId هيمنع التاني على مستوى قاعدة البيانات
                // نفسها حتى لو الفحص فوق فاته - بنرجعله رسالة واضحة بدل خطأ 500 خام. أي
                // DbUpdateException تاني (مش من الاندكس ده) بيتعاد رميه زي ما هو.
                if (IsUniqueIndexViolation(ex, "IX_Admissions_BedId"))
                    return Conflict(new { message = "This bed was just taken by another admission — pick a different bed." });

                throw;
            }

            return NoContent();
        }

        // PATCH: api/admission/{id}/stroke-type
        // الطريقة الآمنة الوحيدة لتحديد/تعديل نوع السكتة (Ischemic/Hemorrhagic/TIA) لإدخال معين.
        // مقصودة كـ Command منفصل (مش جزء من الـ PUT العام) عشان: تسجل مين حددها وامتى تلقائيًا،
        // وتفضل الحقول التانية في الإدخال زي ما هي من غير خطر إنها تتصفر بسبب PUT أعمى.
        [Authorize(Roles = Roles.AnyDoctor)]
        [HttpPatch("{id:guid}/stroke-type")]
        public async Task<IActionResult> SetStrokeType(
            Guid id,
            [FromQuery] Guid actingUserId,
            [FromQuery] StrokeType strokeType,
            CancellationToken cancellationToken = default)
        {
            var admission = await _context.Set<Admission>().FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
            if (admission == null)
                return NotFound();

            admission.StrokeType = strokeType;
            admission.StrokeTypeSetAt = DateTime.UtcNow;
            admission.StrokeTypeSetById = actingUserId;
            admission.UpdatedBy = actingUserId;
            admission.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            _context.Set<AuditLog>().Add(new AuditLog
            {
                EntityName = nameof(Admission),
                EntityId = id,
                Action = "StrokeTypeSet",
                UserId = actingUserId,
                Details = $"StrokeType={(int)strokeType}",
                Timestamp = DateTime.UtcNow
            });
            await _context.SaveChangesAsync(cancellationToken);

            return NoContent();
        }

        // PATCH: api/admission/{id}/thrombolysis
        // بيسجل إن المريض اتحقن بالمذيب (Alteplase/Tenecteplase) وامتى بالظبط — التوقيت ده هو
        // اللي بتتحسب عليه نافذة الـ 24 ساعة اللي المفروض ما ياخدش فيها أي مضاد تجلط/صفائح تاني
        // (Antithrombotic lockout)، وده بيتعرض كتنبيه في صفحة الـ Alerts لحد ما الـ 24 ساعة تخلص.
        [Authorize(Roles = Roles.AnyDoctor)]
        [HttpPatch("{id:guid}/thrombolysis")]
        public async Task<IActionResult> RecordThrombolysis(
            Guid id,
            [FromQuery] Guid actingUserId,
            [FromQuery] string drug,
            [FromQuery] decimal doseMg,
            CancellationToken cancellationToken = default)
        {
            var admission = await _context.Set<Admission>().FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
            if (admission == null)
                return NotFound();

            admission.ThrombolysisGivenAt = DateTime.UtcNow;
            admission.ThrombolysisDrug = drug;
            admission.ThrombolysisDoseMg = doseMg;
            admission.ThrombolysisRecordedById = actingUserId;
            admission.UpdatedBy = actingUserId;
            admission.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            _context.Set<AuditLog>().Add(new AuditLog
            {
                EntityName = nameof(Admission),
                EntityId = id,
                Action = "ThrombolysisRecorded",
                UserId = actingUserId,
                Details = $"Drug={drug}, DoseMg={doseMg}",
                Timestamp = DateTime.UtcNow
            });
            await _context.SaveChangesAsync(cancellationToken);

            return NoContent();
        }

        // GET: api/admission/{id}/timeline
        // رحلة المريض من لحظة الدخول (AdmissionTime) لحد كل تغيير حالة اتسجل تلقائيًا في
        // الـ AuditLog (StatusChanged) - بيحسب الوقت من الوصول والوقت بين كل مرحلة والتانية،
        // من غير أي تعديل في جداول الداتابيز. مفتوح لأي مستخدم مسجل دخول (زي باقي الـ GET).
        [HttpGet("{id:guid}/timeline")]
        public async Task<ActionResult<List<AdmissionTimelineEntryResponse>>> GetTimeline(Guid id, CancellationToken cancellationToken)
        {
            var admission = await _context.Set<Admission>()
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

            if (admission == null)
                return NotFound();

            var changes = await _context.Set<AuditLog>()
                .AsNoTracking()
                .Where(l => l.EntityName == nameof(Admission) && l.EntityId == id && l.Action == "StatusChanged")
                .OrderBy(l => l.Timestamp)
                .ToListAsync(cancellationToken);

            var entries = new List<AdmissionTimelineEntryResponse>
            {
                new AdmissionTimelineEntryResponse
                {
                    Timestamp = admission.AdmissionTime,
                    StatusLabel = "Admitted",
                    Status = null,
                    ChangedById = admission.AdmittedById,
                    MinutesSincePrevious = null,
                    MinutesSinceArrival = 0
                }
            };

            var previousTimestamp = admission.AdmissionTime;
            foreach (var log in changes)
            {
                // بنقرا NewState بـ Regex بدل Split('=') الصارم القديم، عشان Details ممكن
                // دلوقتي يحمل معلومة زيادة بعد النقطة والفاصلة (زي DischargeTime في الانتقالات
                // النهائية) من غير ما كده يكسر قراءة NewState نفسها.
                int? status = null;
                if (log.Details != null)
                {
                    var match = Regex.Match(log.Details, @"NewState=(-?\d+)");
                    if (match.Success && int.TryParse(match.Groups[1].Value, out var parsed))
                        status = parsed;
                }

                var label = status.HasValue && Enum.IsDefined(typeof(PatientStatus), status.Value)
                    ? ((PatientStatus)status.Value).ToString()
                    : "Unknown";

                entries.Add(new AdmissionTimelineEntryResponse
                {
                    Timestamp = log.Timestamp,
                    StatusLabel = label,
                    Status = status,
                    ChangedById = log.UserId,
                    MinutesSincePrevious = (int)(log.Timestamp - previousTimestamp).TotalMinutes,
                    MinutesSinceArrival = (int)(log.Timestamp - admission.AdmissionTime).TotalMinutes
                });

                previousTimestamp = log.Timestamp;
            }

            return Ok(entries);
        }
    }
}
