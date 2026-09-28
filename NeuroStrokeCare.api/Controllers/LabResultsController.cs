using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using NeuroStrokeCare.Core.Features.BaseService.Commands.Models;
using NeuroStrokeCare.Core.Features.BaseService.Queries.Models;
using NeuroStrokeCare.Core.Features.LabResults.Dtos;
using NeuroStrokeCare.Data.Entities;
using NeuroStrokeCare.Data.Enums;
using NeuroStrokeCare.Data.PageModel;

namespace NeuroStrokeCare.api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LabResultsController : ControllerBase
    {
        #region Fields
        private readonly IMediator _mediator;
        private readonly IMapper _mapper;
        #endregion

        #region Constructor
        public LabResultsController(IMediator mediator, IMapper mapper)
        {
            _mediator = mediator;
            _mapper = mapper;
        }
        #endregion

        #region Selectors
        private static readonly System.Linq.Expressions.Expression<Func<LabResults, LabResultsResponse>> ToResponse =
            l => new LabResultsResponse
            {
                Id = l.Id,
                AdmissionId = l.AdmissionId,
                RecordedById = l.RecordedById,
                RecordedAt = l.RecordedAt,
                GlucoseMmol = l.GlucoseMmol,
                INR = l.INR,
                PT = l.PT,
                Platelets = l.Platelets,
                Sodium = l.Sodium,
                Potassium = l.Potassium,
                Creatinine = l.Creatinine,
                Hemoglobin = l.Hemoglobin,
                LDL = l.LDL,
                HbA1c = l.HbA1c,
                aPTT = l.aPTT,
                ALT = l.ALT,
                AST = l.AST,
                ECGAtrialFibrillation = l.ECGAtrialFibrillation,
                INRAlert = l.INRAlert,
                GlucoseAlert = l.GlucoseAlert,
                PlateletsAlert = l.PlateletsAlert
            };
        #endregion

        // GET: api/labresults
        [HttpGet]
        public async Task<ActionResult<List<LabResultsResponse>>> GetAll(CancellationToken cancellationToken)
        {
            var query = new GetListWithFiltersQuery<LabResults, LabResultsResponse>(
                filter: l => l.CurrentState == (int)CurrentStatusType.Active,
                selector: ToResponse,
                orderBy: l => l.RecordedAt);

            var result = await _mediator.Send(query, cancellationToken);
            return Ok(result);
        }

        // GET: api/labresults/paged?pageNumber=1&pageSize=10&admissionId=
        [HttpGet("paged")]
        public async Task<ActionResult<PagedResult<LabResultsResponse>>> GetPaged(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] Guid? admissionId = null,
            CancellationToken cancellationToken = default)
        {
            var query = new GetPagedListQuery<LabResults, LabResultsResponse>(
                filter: l => l.CurrentState == (int)CurrentStatusType.Active &&
                             (admissionId == null || l.AdmissionId == admissionId),
                selector: ToResponse,
                orderBy: l => l.RecordedAt,
                pageNumber: pageNumber,
                pageSize: pageSize);

            var result = await _mediator.Send(query, cancellationToken);
            return Ok(result);
        }

        // GET: api/labresults/{id}
        [HttpGet("{id:guid}")]
        public async Task<ActionResult<LabResultsResponse>> GetById(Guid id, CancellationToken cancellationToken)
        {
            var query = new GetByIdWithFiltersQuery<LabResults, LabResultsResponse>(
                filter: l => l.Id == id,
                selector: ToResponse);

            var result = await _mediator.Send(query, cancellationToken);
            if (result == null)
                return NotFound();

            return Ok(result);
        }

        // POST: api/labresults?actingUserId=...
        [HttpPost]
        public async Task<ActionResult<Guid>> Create(
            [FromBody] CreateLabResultsRequest request,
            [FromQuery] Guid actingUserId,
            CancellationToken cancellationToken)
        {
            var labResults = _mapper.Map<LabResults>(request);
            labResults.RecordedById = actingUserId;

            var command = new AddAsyncGetIDCommand<LabResults>(labResults, actingUserId);
            var (success, entityId) = await _mediator.Send(command, cancellationToken);

            if (!success)
                return BadRequest();

            return CreatedAtAction(nameof(GetById), new { id = entityId }, entityId);
        }

        // PUT: api/labresults?actingUserId=...
        [HttpPut]
        public async Task<IActionResult> Update(
            [FromBody] UpdateLabResultsRequest request,
            [FromQuery] Guid actingUserId,
            CancellationToken cancellationToken)
        {
            var labResults = _mapper.Map<LabResults>(request);
            labResults.RecordedById = actingUserId;

            var command = new UpdateCommand<LabResults>(labResults, actingUserId);
            var affectedRows = await _mediator.Send(command, cancellationToken);

            if (affectedRows == -1)
                return NotFound();

            return NoContent();
        }

        // PATCH: api/labresults/{id}/status?actingUserId=...&status=1
        [HttpPatch("{id:guid}/status")]
        public async Task<IActionResult> ChangeStatus(
            Guid id,
            [FromQuery] Guid actingUserId,
            [FromQuery] int status,
            CancellationToken cancellationToken)
        {
            var command = new ChangeStatusCommand<LabResults>(id, actingUserId, status);
            await _mediator.Send(command, cancellationToken);
            return NoContent();
        }
    }
}
