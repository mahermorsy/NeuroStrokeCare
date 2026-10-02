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
                CTAFindings = a.CTAFindings
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

        // GET: api/admission/{id}
        [HttpGet("{id:guid}")]
        public async Task<ActionResult<AdmissionResponse>> GetById(Guid id, CancellationToken cancellationToken)
        {
            var query = new GetByIdWithFiltersQuery<Admission, AdmissionResponse>(
                filter: a => a.Id == id,
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
            var hasOpenAdmission = await _context.Set<Admission>().AnyAsync(
                a => a.PatientId == request.PatientId &&
                     a.CurrentState == (int)CurrentStatusType.Active &&
                     a.DischargeTime == null,
                cancellationToken);

            if (hasOpenAdmission)
                return Conflict(new { message = "This patient already has an open admission — discharge or transfer it before admitting them again." });

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
                    return Conflict(new { message = "This patient already has an open admission — discharge or transfer it before admitting them again." });

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
