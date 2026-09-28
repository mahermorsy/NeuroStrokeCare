using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NeuroStrokeCare.Core.Features.BaseService.Commands.Models;
using NeuroStrokeCare.Core.Features.BaseService.Queries.Models;
using NeuroStrokeCare.Core.Features.BradenAssessment.Dtos;
using NeuroStrokeCare.Data.Entities.Assessments;
using NeuroStrokeCare.Data.Constants;
using NeuroStrokeCare.Data.Enums;
using NeuroStrokeCare.Data.PageModel;

namespace NeuroStrokeCare.api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BradenAssessmentController : ControllerBase
    {
        #region Fields
        private readonly IMediator _mediator;
        private readonly IMapper _mapper;
        #endregion

        #region Constructor
        public BradenAssessmentController(IMediator mediator, IMapper mapper)
        {
            _mediator = mediator;
            _mapper = mapper;
        }
        #endregion

        #region Selectors
        private static readonly System.Linq.Expressions.Expression<Func<BradenAssessment, BradenAssessmentResponse>> ToResponse =
            b => new BradenAssessmentResponse
            {
                Id = b.Id,
                AdmissionId = b.AdmissionId,
                AssessedById = b.AssessedById,
                AssessedAt = b.AssessedAt,
                SensoryPerception = b.SensoryPerception,
                Moisture = b.Moisture,
                Activity = b.Activity,
                Mobility = b.Mobility,
                Nutrition = b.Nutrition,
                FrictionShear = b.FrictionShear,
                TotalScore = b.TotalScore,
                RiskLevel = b.RiskLevel
            };
        #endregion

        // GET: api/bradenassessment
        [HttpGet]
        public async Task<ActionResult<List<BradenAssessmentResponse>>> GetAll(CancellationToken cancellationToken)
        {
            var query = new GetListWithFiltersQuery<BradenAssessment, BradenAssessmentResponse>(
                filter: b => b.CurrentState == (int)CurrentStatusType.Active,
                selector: ToResponse,
                orderBy: b => b.AssessedAt);

            var result = await _mediator.Send(query, cancellationToken);
            return Ok(result);
        }

        // GET: api/bradenassessment/paged?pageNumber=1&pageSize=10&admissionId=
        [HttpGet("paged")]
        public async Task<ActionResult<PagedResult<BradenAssessmentResponse>>> GetPaged(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] Guid? admissionId = null,
            CancellationToken cancellationToken = default)
        {
            var query = new GetPagedListQuery<BradenAssessment, BradenAssessmentResponse>(
                filter: b => b.CurrentState == (int)CurrentStatusType.Active &&
                             (admissionId == null || b.AdmissionId == admissionId),
                selector: ToResponse,
                orderBy: b => b.AssessedAt,
                pageNumber: pageNumber,
                pageSize: pageSize);

            var result = await _mediator.Send(query, cancellationToken);
            return Ok(result);
        }

        // GET: api/bradenassessment/{id}
        [HttpGet("{id:guid}")]
        public async Task<ActionResult<BradenAssessmentResponse>> GetById(Guid id, CancellationToken cancellationToken)
        {
            var query = new GetByIdWithFiltersQuery<BradenAssessment, BradenAssessmentResponse>(
                filter: b => b.Id == id,
                selector: ToResponse);

            var result = await _mediator.Send(query, cancellationToken);
            if (result == null)
                return NotFound();

            return Ok(result);
        }

        // POST: api/bradenassessment?actingUserId=...
        [Authorize(Roles = Roles.AnyNurse)]
        [HttpPost]
        public async Task<ActionResult<Guid>> Create(
            [FromBody] CreateBradenAssessmentRequest request,
            [FromQuery] Guid actingUserId,
            CancellationToken cancellationToken)
        {
            var assessment = _mapper.Map<BradenAssessment>(request);
            assessment.AssessedById = actingUserId;

            var command = new AddAsyncGetIDCommand<BradenAssessment>(assessment, actingUserId);
            var (success, entityId) = await _mediator.Send(command, cancellationToken);

            if (!success)
                return BadRequest();

            return CreatedAtAction(nameof(GetById), new { id = entityId }, entityId);
        }

        // PUT: api/bradenassessment?actingUserId=...
        [Authorize(Roles = Roles.AnyNurse)]
        [HttpPut]
        public async Task<IActionResult> Update(
            [FromBody] UpdateBradenAssessmentRequest request,
            [FromQuery] Guid actingUserId,
            CancellationToken cancellationToken)
        {
            var assessment = _mapper.Map<BradenAssessment>(request);
            assessment.AssessedById = actingUserId;

            var command = new UpdateCommand<BradenAssessment>(assessment, actingUserId);
            var affectedRows = await _mediator.Send(command, cancellationToken);

            if (affectedRows == -1)
                return NotFound();

            return NoContent();
        }

        // PATCH: api/bradenassessment/{id}/status?actingUserId=...&status=1
        [Authorize(Roles = Roles.AnyNurse)]
        [HttpPatch("{id:guid}/status")]
        public async Task<IActionResult> ChangeStatus(
            Guid id,
            [FromQuery] Guid actingUserId,
            [FromQuery] int status,
            CancellationToken cancellationToken)
        {
            var command = new ChangeStatusCommand<BradenAssessment>(id, actingUserId, status);
            await _mediator.Send(command, cancellationToken);
            return NoContent();
        }
    }
}
