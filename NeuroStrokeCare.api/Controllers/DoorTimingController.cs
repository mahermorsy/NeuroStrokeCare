using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NeuroStrokeCare.Core.Features.BaseService.Commands.Models;
using NeuroStrokeCare.Core.Features.BaseService.Queries.Models;
using NeuroStrokeCare.Core.Features.DoorTiming.Dtos;
using NeuroStrokeCare.Data.AuditLogModel;
using NeuroStrokeCare.Data.Constants;
using NeuroStrokeCare.Data.Entities;
using NeuroStrokeCare.Data.Enums;
using NeuroStrokeCare.Data.PageModel;
using NeuroStrokeCare.infrastructure.Context;

namespace NeuroStrokeCare.api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DoorTimingController : ControllerBase
    {
        #region Fields
        private readonly IMediator _mediator;
        private readonly IMapper _mapper;
        private readonly NeuroFlowDbContext _context;
        #endregion

        #region Constructor
        // NeuroFlowDbContext injected only for the Stroke Code AuditLog writes below (area 6
        // of ADDITIONAL CLINICAL UX & STAFF PROFILE TASKS) - mirrors AdmissionController's
        // existing constructor shape. The generic Create/Update/ChangeStatus actions below are
        // otherwise unchanged and still go through the same MediatR commands as before.
        public DoorTimingController(IMediator mediator, IMapper mapper, NeuroFlowDbContext context)
        {
            _mediator = mediator;
            _mapper = mapper;
            _context = context;
        }
        #endregion

        #region Selectors
        private static readonly System.Linq.Expressions.Expression<Func<DoorTiming, DoorTimingResponse>> ToResponse =
            d => new DoorTimingResponse
            {
                Id = d.Id,
                AdmissionId = d.AdmissionId,
                Er_StrokeArrival = d.Er_StrokeArrival,
                DoorToCT = d.DoorToCT,
                DoorToNeedle = d.DoorToNeedle,
                DoorToGroin = d.DoorToGroin,
                CTRecordedById = d.CTRecordedById,
                NeedleRecordedById = d.NeedleRecordedById,
                GroinRecordedById = d.GroinRecordedById,
                MinutesToCT = d.MinutesToCT,
                MinutesToNeedle = d.MinutesToNeedle,
                MinutesToGroin = d.MinutesToGroin,
                CTDelayed = d.CTDelayed,
                NeedleDelayed = d.NeedleDelayed
            };
        #endregion

        // GET: api/doortiming
        [HttpGet]
        public async Task<ActionResult<List<DoorTimingResponse>>> GetAll(CancellationToken cancellationToken)
        {
            var query = new GetListWithFiltersQuery<DoorTiming, DoorTimingResponse>(
                filter: d => d.CurrentState == (int)CurrentStatusType.Active,
                selector: ToResponse,
                orderBy: d => d.Er_StrokeArrival);

            var result = await _mediator.Send(query, cancellationToken);
            return Ok(result);
        }

        // GET: api/doortiming/paged?pageNumber=1&pageSize=10&admissionId=
        [HttpGet("paged")]
        public async Task<ActionResult<PagedResult<DoorTimingResponse>>> GetPaged(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] Guid? admissionId = null,
            CancellationToken cancellationToken = default)
        {
            var query = new GetPagedListQuery<DoorTiming, DoorTimingResponse>(
                filter: d => d.CurrentState == (int)CurrentStatusType.Active &&
                             (admissionId == null || d.AdmissionId == admissionId),
                selector: ToResponse,
                orderBy: d => d.Er_StrokeArrival,
                pageNumber: pageNumber,
                pageSize: pageSize);

            var result = await _mediator.Send(query, cancellationToken);
            return Ok(result);
        }

        // GET: api/doortiming/{id}
        [HttpGet("{id:guid}")]
        public async Task<ActionResult<DoorTimingResponse>> GetById(Guid id, CancellationToken cancellationToken)
        {
            // FINAL RELEASE-CANDIDATE PASS (section 6, "Soft delete integrity"): CONFIRMED BUG,
            // FIXED HERE. This GetById used to filter on Id alone, so a soft-deleted row
            // (CurrentState != Active) was still returned as a normal 200 through this exact
            // same endpoint any ordinary clinical read uses - there was no separate "admin can
            // still see deleted records" endpoint being bypassed here, this WAS the only read
            // path, and it did not distinguish. Adding the CurrentState check makes a
            // soft-deleted record 404 here, consistent with GetAll/paged (which already filter
            // on CurrentState) and with how every write endpoint already treats "not found".
            var query = new GetByIdWithFiltersQuery<DoorTiming, DoorTimingResponse>(
                filter: d => d.Id == id && d.CurrentState == (int)CurrentStatusType.Active,
                selector: ToResponse);

            var result = await _mediator.Send(query, cancellationToken);
            if (result == null)
                return NotFound();

            return Ok(result);
        }

        // POST: api/doortiming?actingUserId=...
        // إنشاء سجل DoorTiming لإدخال معين = تفعيل "Stroke Code" لهذا الإدخال (ده مش Concept
        // منفصل - دي هي آلية التفعيل الموجودة بالفعل؛ الفرونت إند بيسمّي الزرار المقابل لها
        // "+ Activate stroke code" بالحرف. الـ 1-to-1 FK بين Admission و DoorTiming في
        // الـ DbContext يمنع بنيويًا أي تفعيل مكرر لنفس الإدخال.
        // FINAL RELEASE-CANDIDATE PASS (section 13/16, "Stroke Code"/"Authorization recheck"):
        // CONFIRMED BUG, FIXED. This was the only clinical-write controller with NO
        // [Authorize(Roles=...)] at all on Create/Update/ChangeStatus - every sibling controller
        // (LabResults, FollowUpNote, the Assessment controllers) requires at least AnyClinical/
        // AnyDoctor, but DoorTiming (= Stroke Code activation/stand-down) fell through to only
        // the global "any authenticated user" fallback policy, meaning ANY logged-in account
        // could activate or stand down a Stroke Code. AnyClinical (doctors + nurses + nursing
        // supervisor) matches the same role set already used for LabResults/FollowUpNote - the
        // other "frontline clinical event" write endpoints - rather than the stricter AnyDoctor
        // used for diagnostic assessments.
        [Authorize(Roles = Roles.AnyClinical)]
        [HttpPost]
        public async Task<ActionResult<Guid>> Create(
            [FromBody] CreateDoorTimingRequest request,
            [FromQuery] Guid actingUserId,
            CancellationToken cancellationToken)
        {
            var doorTiming = _mapper.Map<DoorTiming>(request);

            var command = new AddAsyncGetIDCommand<DoorTiming>(doorTiming, actingUserId);
            var (success, entityId) = await _mediator.Send(command, cancellationToken);

            if (!success)
                return BadRequest();

            // ADDITIONAL CLINICAL UX & STAFF PROFILE TASKS (area 6): سجل Audit لتفعيل
            // Stroke Code - بيسجل الإدخال، وقت التفعيل، واليوزر المفعّل، عشان يبقى فيه أثر
            // قابل للمراجعة. ملحوظة أمانة: دي SaveChangesAsync ثانية منفصلة عن حفظة الـ
            // MediatR command فوق (نفس القيد الموجود في كل الـ generic-CRUD controllers
            // التانية اللي معندهاش _context خاص بيها) - لو التفعيل نجح والحفظة دي فشلت
            // (نادر جدًا)، السجل نفسه هيفضل موجود من غير Audit entry مقابله. لو الذرية
            // الكاملة مطلوبة هنا، ده محتاج تحويل الـ Create دي بالكامل لكود مخصص بدل
            // الـ MediatR command، بنفس باترن AdmissionController.Transfer/.SetStrokeType.
            try
            {
                _context.Set<AuditLog>().Add(new AuditLog
                {
                    EntityName = nameof(DoorTiming),
                    EntityId = entityId,
                    Action = "StrokeCodeActivated",
                    UserId = actingUserId,
                    Details = $"AdmissionId={request.AdmissionId}, ArrivalAt={request.Er_StrokeArrival:O}",
                    Timestamp = DateTime.UtcNow
                });
                await _context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException)
            {
                // الـ DoorTiming نفسه اتسجل بنجاح فعلاً - مانرجعش BadRequest/500 للمستخدم
                // عشان خطأ في كتابة سجل الـ Audit بس، لكن الغياب ده لازم يتلاحظ فحص دوري
                // للـ AuditLog (مفيش تسجيل Logger هنا دلوقتي - أضيفي لو فيه ILogger متاح).
            }

            return CreatedAtAction(nameof(GetById), new { id = entityId }, entityId);
        }

        // PUT: api/doortiming?actingUserId=...
        // ملحوظة: تسجيل توقيت لقطة واحدة (مثلاً CT بس) هيبقاله Endpoint خاص بعدين،
        // دلوقتي التحديث بيستبدل كل الحقول مع بعض
        // FINAL RELEASE-CANDIDATE PASS (section 13/16, "Stroke Code"/"Authorization recheck"):
        // CONFIRMED BUG, FIXED. This was the only clinical-write controller with NO
        // [Authorize(Roles=...)] at all on Create/Update/ChangeStatus - every sibling controller
        // (LabResults, FollowUpNote, the Assessment controllers) requires at least AnyClinical/
        // AnyDoctor, but DoorTiming (= Stroke Code activation/stand-down) fell through to only
        // the global "any authenticated user" fallback policy, meaning ANY logged-in account
        // could activate or stand down a Stroke Code. AnyClinical (doctors + nurses + nursing
        // supervisor) matches the same role set already used for LabResults/FollowUpNote - the
        // other "frontline clinical event" write endpoints - rather than the stricter AnyDoctor
        // used for diagnostic assessments.
        [Authorize(Roles = Roles.AnyClinical)]
        [HttpPut]
        public async Task<IActionResult> Update(
            [FromBody] UpdateDoorTimingRequest request,
            [FromQuery] Guid actingUserId,
            CancellationToken cancellationToken)
        {
            var doorTiming = _mapper.Map<DoorTiming>(request);

            var command = new UpdateCommand<DoorTiming>(doorTiming, actingUserId);
            var affectedRows = await _mediator.Send(command, cancellationToken);

            if (affectedRows == -1)
                return NotFound();

            return NoContent();
        }

        // PATCH: api/doortiming/{id}/status?actingUserId=...&status=1
        // مفيش DELETE هنا — تاريخ طبي، الإلغاء بيتم بس عن طريق ChangeStatus (Soft Delete)
        // FINAL RELEASE-CANDIDATE PASS (section 13/16, "Stroke Code"/"Authorization recheck"):
        // CONFIRMED BUG, FIXED. This was the only clinical-write controller with NO
        // [Authorize(Roles=...)] at all on Create/Update/ChangeStatus - every sibling controller
        // (LabResults, FollowUpNote, the Assessment controllers) requires at least AnyClinical/
        // AnyDoctor, but DoorTiming (= Stroke Code activation/stand-down) fell through to only
        // the global "any authenticated user" fallback policy, meaning ANY logged-in account
        // could activate or stand down a Stroke Code. AnyClinical (doctors + nurses + nursing
        // supervisor) matches the same role set already used for LabResults/FollowUpNote - the
        // other "frontline clinical event" write endpoints - rather than the stricter AnyDoctor
        // used for diagnostic assessments.
        [Authorize(Roles = Roles.AnyClinical)]
        [HttpPatch("{id:guid}/status")]
        public async Task<IActionResult> ChangeStatus(
            Guid id,
            [FromQuery] Guid actingUserId,
            [FromQuery] int status,
            CancellationToken cancellationToken)
        {
            // بنقرأ الـ AdmissionId قبل التغيير - للسياق في الـ Audit بس (الـ ChangeStatusCommand
            // نفسه هو المسؤول عن الـ Soft Delete الفعلي، زي ما كان).
            var admissionId = await _context.Set<DoorTiming>()
                .AsNoTracking()
                .Where(d => d.Id == id)
                .Select(d => (Guid?)d.AdmissionId)
                .FirstOrDefaultAsync(cancellationToken);

            var command = new ChangeStatusCommand<DoorTiming>(id, actingUserId, status);
            await _mediator.Send(command, cancellationToken);

            // "Stand down" (status = Inactive) بيقفل الـ Stroke Code. برضه حفظة منفصلة -
            // نفس الملحوظة الموجودة فوق في Create بخصوص الذرية.
            try
            {
                _context.Set<AuditLog>().Add(new AuditLog
                {
                    EntityName = nameof(DoorTiming),
                    EntityId = id,
                    Action = status == (int)CurrentStatusType.Active ? "StrokeCodeReactivated" : "StrokeCodeStoodDown",
                    UserId = actingUserId,
                    Details = admissionId.HasValue ? $"AdmissionId={admissionId}" : null,
                    Timestamp = DateTime.UtcNow
                });
                await _context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException)
            {
            }

            return NoContent();
        }
    }
}
