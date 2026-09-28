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
        [Authorize(Roles = Roles.AnyDoctor)]
        [HttpPost]
        public async Task<ActionResult<Guid>> Create(
            [FromBody] CreateAdmissionRequest request,
            [FromQuery] Guid actingUserId,
            CancellationToken cancellationToken)
        {
            var admission = _mapper.Map<Admission>(request);
            admission.AdmittedById = actingUserId;

            var command = new AddAsyncGetIDCommand<Admission>(admission, actingUserId);
            var (success, entityId) = await _mediator.Send(command, cancellationToken);

            if (!success)
                return BadRequest();

            return CreatedAtAction(nameof(GetById), new { id = entityId }, entityId);
        }

        // PUT: api/admission?actingUserId=...
        [Authorize(Roles = Roles.AnyDoctor)]
        [HttpPut]
        public async Task<IActionResult> Update(
            [FromBody] UpdateAdmissionRequest request,
            [FromQuery] Guid actingUserId,
            CancellationToken cancellationToken)
        {
            var admission = _mapper.Map<Admission>(request);
            admission.AdmittedById = actingUserId;

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

            // مريض خارج من المستشفى نهائيًا - السرير لازم يتحرر حتى لو الطلب نسي يقول كده
            var isLeavingHospital = newStatus == PatientStatus.Discharged || newStatus == PatientStatus.TransferredOut;
            if (isLeavingHospital)
            {
                changeBed = true;
                newBedId = null;
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

            try
            {
                await _context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException)
            {
                // شبكة الأمان الأخيرة: لو اتنين حاولوا يحجزوا نفس السرير في نفس اللحظة بالظبط،
                // الـ Unique Index على Admission.BedId هيمنع التاني على مستوى قاعدة البيانات
                // نفسها حتى لو الفحص فوق فاته - بنرجعله رسالة واضحة بدل خطأ 500 خام.
                return Conflict(new { message = "This bed was just taken by another admission — pick a different bed." });
            }

            _context.Set<AuditLog>().Add(new AuditLog
            {
                EntityName = nameof(Admission),
                EntityId = id,
                Action = "StatusChanged",
                UserId = actingUserId,
                Details = $"NewState={(int)newStatus}",
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
                int? status = null;
                var parts = log.Details?.Split('=');
                if (parts != null && parts.Length == 2 && int.TryParse(parts[1], out var parsed))
                    status = parsed;

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
