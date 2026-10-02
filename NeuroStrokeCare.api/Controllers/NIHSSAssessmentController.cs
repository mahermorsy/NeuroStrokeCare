using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NeuroStrokeCare.Core.Features.BaseService.Commands.Models;
using NeuroStrokeCare.Core.Features.BaseService.Queries.Models;
using NeuroStrokeCare.Core.Features.NIHSSAssessment.Dtos;
using NeuroStrokeCare.Data.Entities;
using NeuroStrokeCare.Data.Constants;
using NeuroStrokeCare.Data.Enums;
using NeuroStrokeCare.Data.PageModel;

namespace NeuroStrokeCare.api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class NIHSSAssessmentController : ControllerBase
    {
        #region Fields
        private readonly IMediator _mediator;
        private readonly IMapper _mapper;
        #endregion

        #region Constructor
        public NIHSSAssessmentController(IMediator mediator, IMapper mapper)
        {
            _mediator = mediator;
            _mapper = mapper;
        }
        #endregion

        #region Selectors
        private static readonly System.Linq.Expressions.Expression<Func<NIHSSAssessment, NIHSSAssessmentResponse>> ToResponse =
            n => new NIHSSAssessmentResponse
            {
                Id = n.Id,
                AdmissionId = n.AdmissionId,
                AssessedById = n.AssessedById,
                AssessedAt = n.AssessedAt,
                LOC = n.LOC,
                LOCQuestions = n.LOCQuestions,
                LOCCommands = n.LOCCommands,
                BestGaze = n.BestGaze,
                Visual = n.Visual,
                FacialPalsy = n.FacialPalsy,
                MotorArmLeft = n.MotorArmLeft,
                MotorArmRight = n.MotorArmRight,
                MotorLegLeft = n.MotorLegLeft,
                MotorLegRight = n.MotorLegRight,
                LimbAtaxia = n.LimbAtaxia,
                Sensory = n.Sensory,
                BestLanguage = n.BestLanguage,
                Dysarthria = n.Dysarthria,
                Extinction = n.Extinction,
                TotalScore = n.TotalScore,
                Severity = n.Severity
            };
        #endregion

        // GET: api/nihssassessment
        [HttpGet]
        public async Task<ActionResult<List<NIHSSAssessmentResponse>>> GetAll(CancellationToken cancellationToken)
        {
            var query = new GetListWithFiltersQuery<NIHSSAssessment, NIHSSAssessmentResponse>(
                filter: n => n.CurrentState == (int)CurrentStatusType.Active,
                selector: ToResponse,
                orderBy: n => n.AssessedAt);

            var result = await _mediator.Send(query, cancellationToken);
            return Ok(result);
        }

        // GET: api/nihssassessment/paged?pageNumber=1&pageSize=10&admissionId=
        [HttpGet("paged")]
        public async Task<ActionResult<PagedResult<NIHSSAssessmentResponse>>> GetPaged(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] Guid? admissionId = null,
            CancellationToken cancellationToken = default)
        {
            var query = new GetPagedListQuery<NIHSSAssessment, NIHSSAssessmentResponse>(
                filter: n => n.CurrentState == (int)CurrentStatusType.Active &&
                             (admissionId == null || n.AdmissionId == admissionId),
                selector: ToResponse,
                orderBy: n => n.AssessedAt,
                pageNumber: pageNumber,
                pageSize: pageSize);

            var result = await _mediator.Send(query, cancellationToken);
            return Ok(result);
        }

        // GET: api/nihssassessment/{id}
        [HttpGet("{id:guid}")]
        public async Task<ActionResult<NIHSSAssessmentResponse>> GetById(Guid id, CancellationToken cancellationToken)
        {
            // FINAL RELEASE-CANDIDATE PASS (section 6, "Soft delete integrity"): CONFIRMED BUG,
            // FIXED HERE. This GetById used to filter on Id alone, so a soft-deleted row
            // (CurrentState != Active) was still returned as a normal 200 through this exact
            // same endpoint any ordinary clinical read uses - there was no separate "admin can
            // still see deleted records" endpoint being bypassed here, this WAS the only read
            // path, and it did not distinguish. Adding the CurrentState check makes a
            // soft-deleted record 404 here, consistent with GetAll/paged (which already filter
            // on CurrentState) and with how every write endpoint already treats "not found".
            var query = new GetByIdWithFiltersQuery<NIHSSAssessment, NIHSSAssessmentResponse>(
                filter: n => n.Id == id && n.CurrentState == (int)CurrentStatusType.Active,
                selector: ToResponse);

            var result = await _mediator.Send(query, cancellationToken);
            if (result == null)
                return NotFound();

            return Ok(result);
        }

        // POST: api/nihssassessment?actingUserId=...
        [Authorize(Roles = Roles.AnyDoctor)]
        [HttpPost]
        public async Task<ActionResult<Guid>> Create(
            [FromBody] CreateNIHSSAssessmentRequest request,
            [FromQuery] Guid actingUserId,
            CancellationToken cancellationToken)
        {
            var assessment = _mapper.Map<NIHSSAssessment>(request);
            assessment.AssessedById = actingUserId;

            var command = new AddAsyncGetIDCommand<NIHSSAssessment>(assessment, actingUserId);
            var (success, entityId) = await _mediator.Send(command, cancellationToken);

            if (!success)
                return BadRequest();

            return CreatedAtAction(nameof(GetById), new { id = entityId }, entityId);
        }

        // PUT: api/nihssassessment?actingUserId=...
        [Authorize(Roles = Roles.AnyDoctor)]
        [HttpPut]
        public async Task<IActionResult> Update(
            [FromBody] UpdateNIHSSAssessmentRequest request,
            [FromQuery] Guid actingUserId,
            CancellationToken cancellationToken)
        {
            var assessment = _mapper.Map<NIHSSAssessment>(request);
            assessment.AssessedById = actingUserId;

            var command = new UpdateCommand<NIHSSAssessment>(assessment, actingUserId);
            var affectedRows = await _mediator.Send(command, cancellationToken);

            if (affectedRows == -1)
                return NotFound();

            return NoContent();
        }

        // PATCH: api/nihssassessment/{id}/status?actingUserId=...&status=1
        // مفيش DELETE هنا — تقييم طبي، الإلغاء بيتم بس عن طريق ChangeStatus (Soft Delete)
        [Authorize(Roles = Roles.AnyDoctor)]
        [HttpPatch("{id:guid}/status")]
        public async Task<IActionResult> ChangeStatus(
            Guid id,
            [FromQuery] Guid actingUserId,
            [FromQuery] int status,
            CancellationToken cancellationToken)
        {
            var command = new ChangeStatusCommand<NIHSSAssessment>(id, actingUserId, status);
            await _mediator.Send(command, cancellationToken);
            return NoContent();
        }
    }
}
