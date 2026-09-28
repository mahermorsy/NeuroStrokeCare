using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NeuroStrokeCare.Core.Features.BaseService.Commands.Models;
using NeuroStrokeCare.Core.Features.BaseService.Queries.Models;
using NeuroStrokeCare.Core.Features.MorseAssessment.Dtos;
using NeuroStrokeCare.Data.Entities.Assessments;
using NeuroStrokeCare.Data.Constants;
using NeuroStrokeCare.Data.Enums;
using NeuroStrokeCare.Data.PageModel;

namespace NeuroStrokeCare.api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MorseAssessmentController : ControllerBase
    {
        #region Fields
        private readonly IMediator _mediator;
        private readonly IMapper _mapper;
        #endregion

        #region Constructor
        public MorseAssessmentController(IMediator mediator, IMapper mapper)
        {
            _mediator = mediator;
            _mapper = mapper;
        }
        #endregion

        #region Selectors
        private static readonly System.Linq.Expressions.Expression<Func<MorseAssessment, MorseAssessmentResponse>> ToResponse =
            m => new MorseAssessmentResponse
            {
                Id = m.Id,
                AdmissionId = m.AdmissionId,
                AssessedById = m.AssessedById,
                AssessedAt = m.AssessedAt,
                HistoryOfFalling = m.HistoryOfFalling,
                SecondaryDiagnosis = m.SecondaryDiagnosis,
                AmbulatoryAid = m.AmbulatoryAid,
                IVOrHeparinLock = m.IVOrHeparinLock,
                Gait = m.Gait,
                MentalStatus = m.MentalStatus,
                TotalScore = m.TotalScore,
                RiskLevel = m.RiskLevel
            };
        #endregion

        // GET: api/morseassessment
        [HttpGet]
        public async Task<ActionResult<List<MorseAssessmentResponse>>> GetAll(CancellationToken cancellationToken)
        {
            var query = new GetListWithFiltersQuery<MorseAssessment, MorseAssessmentResponse>(
                filter: m => m.CurrentState == (int)CurrentStatusType.Active,
                selector: ToResponse,
                orderBy: m => m.AssessedAt);

            var result = await _mediator.Send(query, cancellationToken);
            return Ok(result);
        }

        // GET: api/morseassessment/paged?pageNumber=1&pageSize=10&admissionId=
        [HttpGet("paged")]
        public async Task<ActionResult<PagedResult<MorseAssessmentResponse>>> GetPaged(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] Guid? admissionId = null,
            CancellationToken cancellationToken = default)
        {
            var query = new GetPagedListQuery<MorseAssessment, MorseAssessmentResponse>(
                filter: m => m.CurrentState == (int)CurrentStatusType.Active &&
                             (admissionId == null || m.AdmissionId == admissionId),
                selector: ToResponse,
                orderBy: m => m.AssessedAt,
                pageNumber: pageNumber,
                pageSize: pageSize);

            var result = await _mediator.Send(query, cancellationToken);
            return Ok(result);
        }

        // GET: api/morseassessment/{id}
        [HttpGet("{id:guid}")]
        public async Task<ActionResult<MorseAssessmentResponse>> GetById(Guid id, CancellationToken cancellationToken)
        {
            var query = new GetByIdWithFiltersQuery<MorseAssessment, MorseAssessmentResponse>(
                filter: m => m.Id == id,
                selector: ToResponse);

            var result = await _mediator.Send(query, cancellationToken);
            if (result == null)
                return NotFound();

            return Ok(result);
        }

        // POST: api/morseassessment?actingUserId=...
        [Authorize(Roles = Roles.AnyNurse)]
        [HttpPost]
        public async Task<ActionResult<Guid>> Create(
            [FromBody] CreateMorseAssessmentRequest request,
            [FromQuery] Guid actingUserId,
            CancellationToken cancellationToken)
        {
            var assessment = _mapper.Map<MorseAssessment>(request);
            assessment.AssessedById = actingUserId;

            var command = new AddAsyncGetIDCommand<MorseAssessment>(assessment, actingUserId);
            var (success, entityId) = await _mediator.Send(command, cancellationToken);

            if (!success)
                return BadRequest();

            return CreatedAtAction(nameof(GetById), new { id = entityId }, entityId);
        }

        // PUT: api/morseassessment?actingUserId=...
        [Authorize(Roles = Roles.AnyNurse)]
        [HttpPut]
        public async Task<IActionResult> Update(
            [FromBody] UpdateMorseAssessmentRequest request,
            [FromQuery] Guid actingUserId,
            CancellationToken cancellationToken)
        {
            var assessment = _mapper.Map<MorseAssessment>(request);
            assessment.AssessedById = actingUserId;

            var command = new UpdateCommand<MorseAssessment>(assessment, actingUserId);
            var affectedRows = await _mediator.Send(command, cancellationToken);

            if (affectedRows == -1)
                return NotFound();

            return NoContent();
        }

        // PATCH: api/morseassessment/{id}/status?actingUserId=...&status=1
        [Authorize(Roles = Roles.AnyNurse)]
        [HttpPatch("{id:guid}/status")]
        public async Task<IActionResult> ChangeStatus(
            Guid id,
            [FromQuery] Guid actingUserId,
            [FromQuery] int status,
            CancellationToken cancellationToken)
        {
            var command = new ChangeStatusCommand<MorseAssessment>(id, actingUserId, status);
            await _mediator.Send(command, cancellationToken);
            return NoContent();
        }
    }
}
