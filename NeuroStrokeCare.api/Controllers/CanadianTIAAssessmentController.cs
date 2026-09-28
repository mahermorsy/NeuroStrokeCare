using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NeuroStrokeCare.Core.Features.BaseService.Commands.Models;
using NeuroStrokeCare.Core.Features.BaseService.Queries.Models;
using NeuroStrokeCare.Core.Features.CanadianTIAAssessment.Dtos;
using NeuroStrokeCare.Data.Entities;
using NeuroStrokeCare.Data.Constants;
using NeuroStrokeCare.Data.Enums;
using NeuroStrokeCare.Data.PageModel;

namespace NeuroStrokeCare.api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CanadianTIAAssessmentController : ControllerBase
    {
        #region Fields
        private readonly IMediator _mediator;
        private readonly IMapper _mapper;
        #endregion

        #region Constructor
        public CanadianTIAAssessmentController(IMediator mediator, IMapper mapper)
        {
            _mediator = mediator;
            _mapper = mapper;
        }
        #endregion

        #region Selectors
        private static readonly System.Linq.Expressions.Expression<Func<CanadianTIAAssessment, CanadianTIAAssessmentResponse>> ToResponse =
            c => new CanadianTIAAssessmentResponse
            {
                Id = c.Id,
                AdmissionId = c.AdmissionId,
                AssessedById = c.AssessedById,
                AssessedAt = c.AssessedAt,
                FirstTIAInLifetime = c.FirstTIAInLifetime,
                SymptomsOver10Min = c.SymptomsOver10Min,
                HistoryCarotidStenosis = c.HistoryCarotidStenosis,
                OnAntiplateletTherapy = c.OnAntiplateletTherapy,
                GaitDisturbance = c.GaitDisturbance,
                UnilateralWeakness = c.UnilateralWeakness,
                HistoryOfVertigo = c.HistoryOfVertigo,
                DiastolicBPOver110 = c.DiastolicBPOver110,
                DysarthriaOrAphasia = c.DysarthriaOrAphasia,
                AtrialFibrillationOnECG = c.AtrialFibrillationOnECG,
                InfarctionOnCT = c.InfarctionOnCT,
                PlateletOver400 = c.PlateletOver400,
                GlucoseOver15 = c.GlucoseOver15,
                TotalScore = c.TotalScore,
                RiskLevel = c.RiskLevel
            };
        #endregion

        // GET: api/canadiantiaassessment
        [HttpGet]
        public async Task<ActionResult<List<CanadianTIAAssessmentResponse>>> GetAll(CancellationToken cancellationToken)
        {
            var query = new GetListWithFiltersQuery<CanadianTIAAssessment, CanadianTIAAssessmentResponse>(
                filter: c => c.CurrentState == (int)CurrentStatusType.Active,
                selector: ToResponse,
                orderBy: c => c.AssessedAt);

            var result = await _mediator.Send(query, cancellationToken);
            return Ok(result);
        }

        // GET: api/canadiantiaassessment/paged?pageNumber=1&pageSize=10&admissionId=
        [HttpGet("paged")]
        public async Task<ActionResult<PagedResult<CanadianTIAAssessmentResponse>>> GetPaged(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] Guid? admissionId = null,
            CancellationToken cancellationToken = default)
        {
            var query = new GetPagedListQuery<CanadianTIAAssessment, CanadianTIAAssessmentResponse>(
                filter: c => c.CurrentState == (int)CurrentStatusType.Active &&
                             (admissionId == null || c.AdmissionId == admissionId),
                selector: ToResponse,
                orderBy: c => c.AssessedAt,
                pageNumber: pageNumber,
                pageSize: pageSize);

            var result = await _mediator.Send(query, cancellationToken);
            return Ok(result);
        }

        // GET: api/canadiantiaassessment/{id}
        [HttpGet("{id:guid}")]
        public async Task<ActionResult<CanadianTIAAssessmentResponse>> GetById(Guid id, CancellationToken cancellationToken)
        {
            var query = new GetByIdWithFiltersQuery<CanadianTIAAssessment, CanadianTIAAssessmentResponse>(
                filter: c => c.Id == id,
                selector: ToResponse);

            var result = await _mediator.Send(query, cancellationToken);
            if (result == null)
                return NotFound();

            return Ok(result);
        }

        // POST: api/canadiantiaassessment?actingUserId=...
        [Authorize(Roles = Roles.AnyDoctor)]
        [HttpPost]
        public async Task<ActionResult<Guid>> Create(
            [FromBody] CreateCanadianTIAAssessmentRequest request,
            [FromQuery] Guid actingUserId,
            CancellationToken cancellationToken)
        {
            var assessment = _mapper.Map<CanadianTIAAssessment>(request);
            assessment.AssessedById = actingUserId;

            var command = new AddAsyncGetIDCommand<CanadianTIAAssessment>(assessment, actingUserId);
            var (success, entityId) = await _mediator.Send(command, cancellationToken);

            if (!success)
                return BadRequest();

            return CreatedAtAction(nameof(GetById), new { id = entityId }, entityId);
        }

        // PUT: api/canadiantiaassessment?actingUserId=...
        [Authorize(Roles = Roles.AnyDoctor)]
        [HttpPut]
        public async Task<IActionResult> Update(
            [FromBody] UpdateCanadianTIAAssessmentRequest request,
            [FromQuery] Guid actingUserId,
            CancellationToken cancellationToken)
        {
            var assessment = _mapper.Map<CanadianTIAAssessment>(request);
            assessment.AssessedById = actingUserId;

            var command = new UpdateCommand<CanadianTIAAssessment>(assessment, actingUserId);
            var affectedRows = await _mediator.Send(command, cancellationToken);

            if (affectedRows == -1)
                return NotFound();

            return NoContent();
        }

        // PATCH: api/canadiantiaassessment/{id}/status?actingUserId=...&status=1
        [Authorize(Roles = Roles.AnyDoctor)]
        [HttpPatch("{id:guid}/status")]
        public async Task<IActionResult> ChangeStatus(
            Guid id,
            [FromQuery] Guid actingUserId,
            [FromQuery] int status,
            CancellationToken cancellationToken)
        {
            var command = new ChangeStatusCommand<CanadianTIAAssessment>(id, actingUserId, status);
            await _mediator.Send(command, cancellationToken);
            return NoContent();
        }
    }
}
