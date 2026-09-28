using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NeuroStrokeCare.Core.Features.BaseService.Commands.Models;
using NeuroStrokeCare.Core.Features.BaseService.Queries.Models;
using NeuroStrokeCare.Core.Features.GCSAssessment.Dtos;
using NeuroStrokeCare.Data.Entities.Assessments;
using NeuroStrokeCare.Data.Constants;
using NeuroStrokeCare.Data.Enums;
using NeuroStrokeCare.Data.PageModel;

namespace NeuroStrokeCare.api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class GCSAssessmentController : ControllerBase
    {
        #region Fields
        private readonly IMediator _mediator;
        private readonly IMapper _mapper;
        #endregion

        #region Constructor
        public GCSAssessmentController(IMediator mediator, IMapper mapper)
        {
            _mediator = mediator;
            _mapper = mapper;
        }
        #endregion

        #region Selectors
        private static readonly System.Linq.Expressions.Expression<Func<GCSAssessment, GCSAssessmentResponse>> ToResponse =
            g => new GCSAssessmentResponse
            {
                Id = g.Id,
                AdmissionId = g.AdmissionId,
                AssessedById = g.AssessedById,
                AssessedAt = g.AssessedAt,
                EyeResponse = g.EyeResponse,
                VerbalResponse = g.VerbalResponse,
                MotorResponse = g.MotorResponse,
                TotalScore = g.TotalScore,
                Severity = g.Severity
            };
        #endregion

        // GET: api/gcsassessment
        [HttpGet]
        public async Task<ActionResult<List<GCSAssessmentResponse>>> GetAll(CancellationToken cancellationToken)
        {
            var query = new GetListWithFiltersQuery<GCSAssessment, GCSAssessmentResponse>(
                filter: g => g.CurrentState == (int)CurrentStatusType.Active,
                selector: ToResponse,
                orderBy: g => g.AssessedAt);

            var result = await _mediator.Send(query, cancellationToken);
            return Ok(result);
        }

        // GET: api/gcsassessment/paged?pageNumber=1&pageSize=10&admissionId=
        [HttpGet("paged")]
        public async Task<ActionResult<PagedResult<GCSAssessmentResponse>>> GetPaged(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] Guid? admissionId = null,
            CancellationToken cancellationToken = default)
        {
            var query = new GetPagedListQuery<GCSAssessment, GCSAssessmentResponse>(
                filter: g => g.CurrentState == (int)CurrentStatusType.Active &&
                             (admissionId == null || g.AdmissionId == admissionId),
                selector: ToResponse,
                orderBy: g => g.AssessedAt,
                pageNumber: pageNumber,
                pageSize: pageSize);

            var result = await _mediator.Send(query, cancellationToken);
            return Ok(result);
        }

        // GET: api/gcsassessment/{id}
        [HttpGet("{id:guid}")]
        public async Task<ActionResult<GCSAssessmentResponse>> GetById(Guid id, CancellationToken cancellationToken)
        {
            var query = new GetByIdWithFiltersQuery<GCSAssessment, GCSAssessmentResponse>(
                filter: g => g.Id == id,
                selector: ToResponse);

            var result = await _mediator.Send(query, cancellationToken);
            if (result == null)
                return NotFound();

            return Ok(result);
        }

        // POST: api/gcsassessment?actingUserId=...
        [Authorize(Roles = Roles.AnyDoctor)]
        [HttpPost]
        public async Task<ActionResult<Guid>> Create(
            [FromBody] CreateGCSAssessmentRequest request,
            [FromQuery] Guid actingUserId,
            CancellationToken cancellationToken)
        {
            var assessment = _mapper.Map<GCSAssessment>(request);
            assessment.AssessedById = actingUserId;

            var command = new AddAsyncGetIDCommand<GCSAssessment>(assessment, actingUserId);
            var (success, entityId) = await _mediator.Send(command, cancellationToken);

            if (!success)
                return BadRequest();

            return CreatedAtAction(nameof(GetById), new { id = entityId }, entityId);
        }

        // PUT: api/gcsassessment?actingUserId=...
        [Authorize(Roles = Roles.AnyDoctor)]
        [HttpPut]
        public async Task<IActionResult> Update(
            [FromBody] UpdateGCSAssessmentRequest request,
            [FromQuery] Guid actingUserId,
            CancellationToken cancellationToken)
        {
            var assessment = _mapper.Map<GCSAssessment>(request);
            assessment.AssessedById = actingUserId;

            var command = new UpdateCommand<GCSAssessment>(assessment, actingUserId);
            var affectedRows = await _mediator.Send(command, cancellationToken);

            if (affectedRows == -1)
                return NotFound();

            return NoContent();
        }

        // PATCH: api/gcsassessment/{id}/status?actingUserId=...&status=1
        [Authorize(Roles = Roles.AnyDoctor)]
        [HttpPatch("{id:guid}/status")]
        public async Task<IActionResult> ChangeStatus(
            Guid id,
            [FromQuery] Guid actingUserId,
            [FromQuery] int status,
            CancellationToken cancellationToken)
        {
            var command = new ChangeStatusCommand<GCSAssessment>(id, actingUserId, status);
            await _mediator.Send(command, cancellationToken);
            return NoContent();
        }
    }
}
