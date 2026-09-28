using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NeuroStrokeCare.Core.Features.ASPECTSAssessment.Dtos;
using NeuroStrokeCare.Core.Features.BaseService.Commands.Models;
using NeuroStrokeCare.Core.Features.BaseService.Queries.Models;
using NeuroStrokeCare.Data.Entities;
using NeuroStrokeCare.Data.Constants;
using NeuroStrokeCare.Data.Enums;
using NeuroStrokeCare.Data.PageModel;

namespace NeuroStrokeCare.api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ASPECTSAssessmentController : ControllerBase
    {
        #region Fields
        private readonly IMediator _mediator;
        private readonly IMapper _mapper;
        #endregion

        #region Constructor
        public ASPECTSAssessmentController(IMediator mediator, IMapper mapper)
        {
            _mediator = mediator;
            _mapper = mapper;
        }
        #endregion

        #region Selectors
        private static readonly System.Linq.Expressions.Expression<Func<ASPECTSAssessment, ASPECTSAssessmentResponse>> ToResponse =
            a => new ASPECTSAssessmentResponse
            {
                Id = a.Id,
                AdmissionId = a.AdmissionId,
                AssessedById = a.AssessedById,
                AssessedAt = a.AssessedAt,
                C = a.C,
                P = a.P,
                IC = a.IC,
                I = a.I,
                M1 = a.M1,
                M2 = a.M2,
                M3 = a.M3,
                M4 = a.M4,
                M5 = a.M5,
                M6 = a.M6,
                TotalScore = a.TotalScore
            };
        #endregion

        // GET: api/aspectsassessment
        [HttpGet]
        public async Task<ActionResult<List<ASPECTSAssessmentResponse>>> GetAll(CancellationToken cancellationToken)
        {
            var query = new GetListWithFiltersQuery<ASPECTSAssessment, ASPECTSAssessmentResponse>(
                filter: a => a.CurrentState == (int)CurrentStatusType.Active,
                selector: ToResponse,
                orderBy: a => a.AssessedAt);

            var result = await _mediator.Send(query, cancellationToken);
            return Ok(result);
        }

        // GET: api/aspectsassessment/paged?pageNumber=1&pageSize=10&admissionId=
        [HttpGet("paged")]
        public async Task<ActionResult<PagedResult<ASPECTSAssessmentResponse>>> GetPaged(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] Guid? admissionId = null,
            CancellationToken cancellationToken = default)
        {
            var query = new GetPagedListQuery<ASPECTSAssessment, ASPECTSAssessmentResponse>(
                filter: a => a.CurrentState == (int)CurrentStatusType.Active &&
                             (admissionId == null || a.AdmissionId == admissionId),
                selector: ToResponse,
                orderBy: a => a.AssessedAt,
                pageNumber: pageNumber,
                pageSize: pageSize);

            var result = await _mediator.Send(query, cancellationToken);
            return Ok(result);
        }

        // GET: api/aspectsassessment/{id}
        [HttpGet("{id:guid}")]
        public async Task<ActionResult<ASPECTSAssessmentResponse>> GetById(Guid id, CancellationToken cancellationToken)
        {
            var query = new GetByIdWithFiltersQuery<ASPECTSAssessment, ASPECTSAssessmentResponse>(
                filter: a => a.Id == id,
                selector: ToResponse);

            var result = await _mediator.Send(query, cancellationToken);
            if (result == null)
                return NotFound();

            return Ok(result);
        }

        // POST: api/aspectsassessment?actingUserId=...
        [Authorize(Roles = Roles.AnyDoctor)]
        [HttpPost]
        public async Task<ActionResult<Guid>> Create(
            [FromBody] CreateASPECTSAssessmentRequest request,
            [FromQuery] Guid actingUserId,
            CancellationToken cancellationToken)
        {
            var assessment = _mapper.Map<ASPECTSAssessment>(request);
            assessment.AssessedById = actingUserId;

            var command = new AddAsyncGetIDCommand<ASPECTSAssessment>(assessment, actingUserId);
            var (success, entityId) = await _mediator.Send(command, cancellationToken);

            if (!success)
                return BadRequest();

            return CreatedAtAction(nameof(GetById), new { id = entityId }, entityId);
        }

        // PUT: api/aspectsassessment?actingUserId=...
        [Authorize(Roles = Roles.AnyDoctor)]
        [HttpPut]
        public async Task<IActionResult> Update(
            [FromBody] UpdateASPECTSAssessmentRequest request,
            [FromQuery] Guid actingUserId,
            CancellationToken cancellationToken)
        {
            var assessment = _mapper.Map<ASPECTSAssessment>(request);
            assessment.AssessedById = actingUserId;

            var command = new UpdateCommand<ASPECTSAssessment>(assessment, actingUserId);
            var affectedRows = await _mediator.Send(command, cancellationToken);

            if (affectedRows == -1)
                return NotFound();

            return NoContent();
        }

        // PATCH: api/aspectsassessment/{id}/status?actingUserId=...&status=1
        [Authorize(Roles = Roles.AnyDoctor)]
        [HttpPatch("{id:guid}/status")]
        public async Task<IActionResult> ChangeStatus(
            Guid id,
            [FromQuery] Guid actingUserId,
            [FromQuery] int status,
            CancellationToken cancellationToken)
        {
            var command = new ChangeStatusCommand<ASPECTSAssessment>(id, actingUserId, status);
            await _mediator.Send(command, cancellationToken);
            return NoContent();
        }
    }
}
