using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NeuroStrokeCare.Core.Features.BaseService.Commands.Models;
using NeuroStrokeCare.Core.Features.BaseService.Queries.Models;
using NeuroStrokeCare.Core.Features.Patient.Dtos;
using NeuroStrokeCare.Data.Constants;
using NeuroStrokeCare.Data.Entities;
using NeuroStrokeCare.Data.Enums;
using NeuroStrokeCare.Data.Exceptions;
using NeuroStrokeCare.Data.PageModel;
using NeuroStrokeCare.infrastructure.Context;
using Microsoft.Data.SqlClient;

namespace NeuroStrokeCare.api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PatientController : ControllerBase
    {
        #region Fields
        private readonly IMediator _mediator;
        private readonly IMapper _mapper;
        // PHASE 11 (area 1/7): injected only for the HospitalNumber uniqueness pre-check
        // below (and the Patient Edit authorization boundary already lives in the
        // [Authorize] attributes, not here) - mirrors DoorTimingController/AdmissionController's
        // existing constructor shape rather than introducing a new pattern.
        private readonly NeuroFlowDbContext _context;
        #endregion

        #region Constructor
        public PatientController(IMediator mediator, IMapper mapper, NeuroFlowDbContext context)
        {
            _mediator = mediator;
            _mapper = mapper;
            _context = context;
        }
        #endregion

        #region Selectors
        private static readonly System.Linq.Expressions.Expression<Func<Patient, PatientResponse>> ToResponse =
            p => new PatientResponse
            {
                Id = p.Id,
                NationalId = p.NationalId,
                HospitalNumber = p.HospitalNumber,
                FirstName = p.FirstName,
                MiddleName = p.MiddleName,
                LastName = p.LastName,
                DateOfBirth = p.DateOfBirth,
                Gender = p.Gender,
                WeightKg = p.WeightKg,
                ChiefComplaint = p.ChiefComplaint
            };
        #endregion

        // 2601 = "Cannot insert duplicate key row ... with unique index '...'" - same
        // detection style as AdmissionController.IsUniqueIndexViolation, kept local here
        // since it's the only place in this controller that needs it.
        //
        // IMPORTANT: unlike AdmissionController.Create (which deliberately bypasses
        // AddAsyncGetIDCommand and writes to _context directly, specifically so a real
        // DbUpdateException reaches its own catch block - see the comment there), Create/
        // Update below still go through the generic AddAsyncGetIDCommand/UpdateCommand to
        // keep every other side effect (CreatedAt/CreatedBy/CurrentState, the repository's
        // own audit logging) identical to before this phase. TableRepository.AddAsyncGetID/
        // UpdateAsync both catch *any* exception and rethrow it wrapped as
        // DataAccessException(ex, ...) - so the real DbUpdateException is one level down, as
        // DataAccessException.InnerException, not the exception type itself. This checks
        // that inner exception instead of pretending a plain `catch (DbUpdateException)`
        // here would ever fire (it wouldn't - confirmed by reading TableRepository.cs).
        private static bool IsUniqueIndexViolation(Exception ex, string indexName) =>
            ex.InnerException is DbUpdateException dbEx
            && dbEx.InnerException is SqlException sqlEx
            && sqlEx.Number == 2601
            && sqlEx.Message.Contains(indexName, StringComparison.OrdinalIgnoreCase);

        // GET: api/patient
        [HttpGet]
        public async Task<ActionResult<List<PatientResponse>>> GetAll(CancellationToken cancellationToken)
        {
            var query = new GetListWithFiltersQuery<Patient, PatientResponse>(
                filter: p => p.CurrentState == (int)CurrentStatusType.Active,
                selector: ToResponse,
                orderBy: p => p.LastName);

            var result = await _mediator.Send(query, cancellationToken);
            return Ok(result);
        }

        // GET: api/patient/paged?pageNumber=1&pageSize=10&search=
        [HttpGet("paged")]
        public async Task<ActionResult<PagedResult<PatientResponse>>> GetPaged(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? search = null,
            CancellationToken cancellationToken = default)
        {
            var query = new GetPagedListQuery<Patient, PatientResponse>(
                filter: p => p.CurrentState == (int)CurrentStatusType.Active &&
                             (string.IsNullOrEmpty(search) ||
                              p.FirstName.Contains(search) ||
                              p.LastName.Contains(search) ||
                              (p.NationalId != null && p.NationalId.Contains(search)) ||
                              (p.HospitalNumber != null && p.HospitalNumber.Contains(search))),
                selector: ToResponse,
                orderBy: p => p.LastName,
                pageNumber: pageNumber,
                pageSize: pageSize);

            var result = await _mediator.Send(query, cancellationToken);
            return Ok(result);
        }

        // GET: api/patient/{id}
        [HttpGet("{id:guid}")]
        public async Task<ActionResult<PatientResponse>> GetById(Guid id, CancellationToken cancellationToken)
        {
            // FINAL RELEASE-CANDIDATE PASS (section 6, "Soft delete integrity"): CONFIRMED BUG,
            // FIXED HERE. This GetById used to filter on Id alone, so a soft-deleted row
            // (CurrentState != Active) was still returned as a normal 200 through this exact
            // same endpoint any ordinary clinical read uses - there was no separate "admin can
            // still see deleted records" endpoint being bypassed here, this WAS the only read
            // path, and it did not distinguish. Adding the CurrentState check makes a
            // soft-deleted record 404 here, consistent with GetAll/paged (which already filter
            // on CurrentState) and with how every write endpoint already treats "not found".
            var query = new GetByIdWithFiltersQuery<Patient, PatientResponse>(
                filter: p => p.Id == id && p.CurrentState == (int)CurrentStatusType.Active,
                selector: ToResponse);

            var result = await _mediator.Send(query, cancellationToken);
            if (result == null)
                return NotFound();

            return Ok(result);
        }

        // بيشيل المسافات الزيادة ومايفرقش بين رقم فاضي ("") ومن غير قيمة (null) - الاتنين
        // بيتعاملوا كـ "من غير رقم ملف" عشان الفهرس الفريد في الداتابيز بيستثني NULL بس.
        private static string? NormalizeHospitalNumber(string? value)
        {
            var trimmed = value?.Trim();
            return string.IsNullOrEmpty(trimmed) ? null : trimmed;
        }

        // فحص صريح قبل الكتابة - يرجع رسالة واضحة للمستخدم ("رقم الملف ده مستخدم بالفعل")
        // بدل ما ينتظر الـ DbUpdateException من الفهرس الفريد في الداتابيز (اللي برضه موجود
        // كشبكة أمان أخيرة ضد أي Race Condition - زي باترن AdmissionController.Create بالظبط).
        private async Task<bool> HospitalNumberTakenAsync(string hospitalNumber, Guid? excludingPatientId, CancellationToken cancellationToken)
        {
            return await _context.Set<Patient>()
                .AsNoTracking()
                .AnyAsync(p => p.CurrentState == (int)CurrentStatusType.Active &&
                               p.HospitalNumber == hospitalNumber &&
                               (excludingPatientId == null || p.Id != excludingPatientId),
                    cancellationToken);
        }

        // POST: api/patient?actingUserId=...
        // PHASE 11 (area 7): PatientController's Create/Update/ChangeStatus had NO
        // [Authorize] at all before this phase (every other write-capable controller in the
        // solution already has one - AdmissionController/*AssessmentController use
        // Roles.AnyDoctor, Braden/GUSS/Morse use Roles.AnyNurse, LabResults/FollowUpNote use
        // Roles.AnyClinical). Roles.AnyClinical was chosen here - not invented - because it's
        // the existing constant already used for the same kind of "any clinical staff may
        // write this" entity (LabResults, FollowUp notes); patient demographic registration
        // reads the same way. If that boundary should be narrower (e.g. doctors only, matching
        // Admission), that's a one-word change to the attribute below, not a model change.
        [Authorize(Roles = Roles.AnyClinical)]
        [HttpPost]
        public async Task<ActionResult<Guid>> Create(
            [FromBody] CreatePatientRequest request,
            [FromQuery] Guid actingUserId,
            CancellationToken cancellationToken)
        {
            var hospitalNumber = NormalizeHospitalNumber(request.HospitalNumber);
            if (hospitalNumber != null && await HospitalNumberTakenAsync(hospitalNumber, null, cancellationToken))
                return Conflict("رقم الملف ده مستخدم بالفعل لمريض آخر");

            var patient = _mapper.Map<Patient>(request);
            patient.HospitalNumber = hospitalNumber;

            var command = new AddAsyncGetIDCommand<Patient>(patient, actingUserId);

            try
            {
                var (success, entityId) = await _mediator.Send(command, cancellationToken);

                if (!success)
                    return BadRequest();

                return CreatedAtAction(nameof(GetById), new { id = entityId }, entityId);
            }
            catch (DataAccessException ex) when (IsUniqueIndexViolation(ex, "IX_Patients_HospitalNumber"))
            {
                return Conflict("رقم الملف ده مستخدم بالفعل لمريض آخر");
            }
        }

        // PUT: api/patient?actingUserId=...
        [Authorize(Roles = Roles.AnyClinical)]
        [HttpPut]
        public async Task<IActionResult> Update(
            [FromBody] UpdatePatientRequest request,
            [FromQuery] Guid actingUserId,
            CancellationToken cancellationToken)
        {
            var hospitalNumber = NormalizeHospitalNumber(request.HospitalNumber);
            if (hospitalNumber != null && await HospitalNumberTakenAsync(hospitalNumber, request.Id, cancellationToken))
                return Conflict("رقم الملف ده مستخدم بالفعل لمريض آخر");

            var patient = _mapper.Map<Patient>(request);
            patient.HospitalNumber = hospitalNumber;

            var command = new UpdateCommand<Patient>(patient, actingUserId);

            try
            {
                var affectedRows = await _mediator.Send(command, cancellationToken);

                if (affectedRows == -1)
                    return NotFound();

                return NoContent();
            }
            catch (DataAccessException ex) when (IsUniqueIndexViolation(ex, "IX_Patients_HospitalNumber"))
            {
                return Conflict("رقم الملف ده مستخدم بالفعل لمريض آخر");
            }
        }

        // PATCH: api/patient/{id}/status?actingUserId=...&status=1
        // مفيش DELETE فعلي خالص هنا — Patient تاريخ مريض، الإلغاء بيتم بس عن طريق ChangeStatus (Soft Delete)
        [Authorize(Roles = Roles.AnyClinical)]
        [HttpPatch("{id:guid}/status")]
        public async Task<IActionResult> ChangeStatus(
            Guid id,
            [FromQuery] Guid actingUserId,
            [FromQuery] int status,
            CancellationToken cancellationToken)
        {
            var command = new ChangeStatusCommand<Patient>(id, actingUserId, status);
            await _mediator.Send(command, cancellationToken);
            return NoContent();
        }
    }
}
