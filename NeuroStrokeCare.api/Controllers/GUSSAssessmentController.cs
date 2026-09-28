using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NeuroStrokeCare.Core.Features.BaseService.Commands.Models;
using NeuroStrokeCare.Core.Features.BaseService.Queries.Models;
using NeuroStrokeCare.Core.Features.GUSSAssessment.Dtos;
using NeuroStrokeCare.Data.Entities.Assessments;
using NeuroStrokeCare.Data.Constants;
using NeuroStrokeCare.Data.Enums;
using NeuroStrokeCare.Data.PageModel;

namespace NeuroStrokeCare.api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class GUSSAssessmentController : ControllerBase
    {
        #region Fields
        private readonly IMediator _mediator;
        private readonly IMapper _mapper;
        #endregion

        #region Constructor
        public GUSSAssessmentController(IMediator mediator, IMapper mapper)
        {
            _mediator = mediator;
            _mapper = mapper;
        }
        #endregion

        #region Selectors
        private static readonly System.Linq.Expressions.Expression<Func<GUSSAssessment, GUSSAssessmentResponse>> ToResponse =
            g => new GUSSAssessmentResponse
            {
                Id = g.Id,
                AdmissionId = g.AdmissionId,
                AssessedById = g.AssessedById,
                AssessedAt = g.AssessedAt,
                Vigilance = g.Vigilance,
                VoluntaryCough = g.VoluntaryCough,
                SalivaSwallowSuccessful = g.SalivaSwallowSuccessful,
                NoDrooling = g.NoDrooling,
                NoVoiceChange = g.NoVoiceChange,
                IndirectScore = g.IndirectScore,
                SemisolidScore = g.SemisolidScore,
                LiquidScore = g.LiquidScore,
                SolidScore = g.SolidScore,
                TotalScore = g.TotalScore,
                Severity = g.Severity
            };
        #endregion

        // GET: api/gussassessment
        [HttpGet]
        public async Task<ActionResult<List<GUSSAssessmentResponse>>> GetAll(CancellationToken cancellationToken)
        {
            var query = new GetListWithFiltersQuery<GUSSAssessment, GUSSAssessmentResponse>(
                filter: g => g.CurrentState == (int)CurrentStatusType.Active,
                selector: ToResponse,
                orderBy: g => g.AssessedAt);

            var result = await _mediator.Send(query, cancellationToken);
            return Ok(result);
        }

        // GET: api/gussassessment/paged?pageNumber=1&pageSize=10&admissionId=
        [HttpGet("paged")]
        public async Task<ActionResult<PagedResult<GUSSAssessmentResponse>>> GetPaged(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] Guid? admissionId = null,
            CancellationToken cancellationToken = default)
        {
            var query = new GetPagedListQuery<GUSSAssessment, GUSSAssessmentResponse>(
                filter: g => g.CurrentState == (int)CurrentStatusType.Active &&
                             (admissionId == null || g.AdmissionId == admissionId),
                selector: ToResponse,
                orderBy: g => g.AssessedAt,
                pageNumber: pageNumber,
                pageSize: pageSize);

            var result = await _mediator.Send(query, cancellationToken);
            return Ok(result);
        }

        // GET: api/gussassessment/{id}
        [HttpGet("{id:guid}")]
        public async Task<ActionResult<GUSSAssessmentResponse>> GetById(Guid id, CancellationToken cancellationToken)
        {
            var query = new GetByIdWithFiltersQuery<GUSSAssessment, GUSSAssessmentResponse>(
                filter: g => g.Id == id,
                selector: ToResponse);

            var result = await _mediator.Send(query, cancellationToken);
            if (result == null)
                return NotFound();

            return Ok(result);
        }

        // POST: api/gussassessment?actingUserId=...
        [Authorize(Roles = Roles.AnyNurse)]
        [HttpPost]
        public async Task<ActionResult<Guid>> Create(
            [FromBody] CreateGUSSAssessmentRequest request,
            [FromQuery] Guid actingUserId,
            CancellationToken cancellationToken)
        {
            var assessment = _mapper.Map<GUSSAssessment>(request);
            assessment.AssessedById = actingUserId;

            var command = new AddAsyncGetIDCommand<GUSSAssessment>(assessment, actingUserId);
            var (success, entityId) = await _mediator.Send(command, cancellationToken);

            if (!success)
                return BadRequest();

            return CreatedAtAction(nameof(GetById), new { id = entityId }, entityId);
        }

        // PUT: api/gussassessment?actingUserId=...
        [Authorize(Roles = Roles.AnyNurse)]
        [HttpPut]
        public async Task<IActionResult> Update(
            [FromBody] UpdateGUSSAssessmentRequest request,
            [FromQuery] Guid actingUserId,
            CancellationToken cancellationToken)
        {
            var assessment = _mapper.Map<GUSSAssessment>(request);
            assessment.AssessedById = actingUserId;

            var command = new UpdateCommand<GUSSAssessment>(assessment, actingUserId);
            var affectedRows = await _mediator.Send(command, cancellationToken);

            if (affectedRows == -1)
                return NotFound();

            return NoContent();
        }

        // PATCH: api/gussassessment/{id}/status?actingUserId=...&status=1
        [Authorize(Roles = Roles.AnyNurse)]
        [HttpPatch("{id:guid}/status")]
        public async Task<IActionResult> ChangeStatus(
            Guid id,
            [FromQuery] Guid actingUserId,
            [FromQuery] int status,
            CancellationToken cancellationToken)
        {
            var command = new ChangeStatusCommand<GUSSAssessment>(id, actingUserId, status);
            await _mediator.Send(command, cancellationToken);
            return NoContent();
        }
    }
}
