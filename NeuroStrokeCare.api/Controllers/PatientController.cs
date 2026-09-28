using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using NeuroStrokeCare.Core.Features.BaseService.Commands.Models;
using NeuroStrokeCare.Core.Features.BaseService.Queries.Models;
using NeuroStrokeCare.Core.Features.Patient.Dtos;
using NeuroStrokeCare.Data.Entities;
using NeuroStrokeCare.Data.Enums;
using NeuroStrokeCare.Data.PageModel;

namespace NeuroStrokeCare.api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PatientController : ControllerBase
    {
        #region Fields
        private readonly IMediator _mediator;
        private readonly IMapper _mapper;
        #endregion

        #region Constructor
        public PatientController(IMediator mediator, IMapper mapper)
        {
            _mediator = mediator;
            _mapper = mapper;
        }
        #endregion

        #region Selectors
        private static readonly System.Linq.Expressions.Expression<Func<Patient, PatientResponse>> ToResponse =
            p => new PatientResponse
            {
                Id = p.Id,
                NationalId = p.NationalId,
                FirstName = p.FirstName,
                MiddleName = p.MiddleName,
                LastName = p.LastName,
                DateOfBirth = p.DateOfBirth,
                Gender = p.Gender,
                WeightKg = p.WeightKg,
                ChiefComplaint = p.ChiefComplaint
            };
        #endregion

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
                              (p.NationalId != null && p.NationalId.Contains(search))),
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
            var query = new GetByIdWithFiltersQuery<Patient, PatientResponse>(
                filter: p => p.Id == id,
                selector: ToResponse);

            var result = await _mediator.Send(query, cancellationToken);
            if (result == null)
                return NotFound();

            return Ok(result);
        }

        // POST: api/patient?actingUserId=...
        [HttpPost]
        public async Task<ActionResult<Guid>> Create(
            [FromBody] CreatePatientRequest request,
            [FromQuery] Guid actingUserId,
            CancellationToken cancellationToken)
        {
            var patient = _mapper.Map<Patient>(request);

            var command = new AddAsyncGetIDCommand<Patient>(patient, actingUserId);
            var (success, entityId) = await _mediator.Send(command, cancellationToken);

            if (!success)
                return BadRequest();

            return CreatedAtAction(nameof(GetById), new { id = entityId }, entityId);
        }

        // PUT: api/patient?actingUserId=...
        [HttpPut]
        public async Task<IActionResult> Update(
            [FromBody] UpdatePatientRequest request,
            [FromQuery] Guid actingUserId,
            CancellationToken cancellationToken)
        {
            var patient = _mapper.Map<Patient>(request);

            var command = new UpdateCommand<Patient>(patient, actingUserId);
            var affectedRows = await _mediator.Send(command, cancellationToken);

            if (affectedRows == -1)
                return NotFound();

            return NoContent();
        }

        // PATCH: api/patient/{id}/status?actingUserId=...&status=1
        // مفيش DELETE فعلي خالص هنا — Patient تاريخ مريض، الإلغاء بيتم بس عن طريق ChangeStatus (Soft Delete)
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
