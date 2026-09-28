using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using NeuroStrokeCare.Core.Features.BaseService.Commands.Models;
using NeuroStrokeCare.Core.Features.BaseService.Queries.Models;
using NeuroStrokeCare.Core.Features.DoorTiming.Dtos;
using NeuroStrokeCare.Data.Entities;
using NeuroStrokeCare.Data.Enums;
using NeuroStrokeCare.Data.PageModel;

namespace NeuroStrokeCare.api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DoorTimingController : ControllerBase
    {
        #region Fields
        private readonly IMediator _mediator;
        private readonly IMapper _mapper;
        #endregion

        #region Constructor
        public DoorTimingController(IMediator mediator, IMapper mapper)
        {
            _mediator = mediator;
            _mapper = mapper;
        }
        #endregion

        #region Selectors
        private static readonly System.Linq.Expressions.Expression<Func<DoorTiming, DoorTimingResponse>> ToResponse =
            d => new DoorTimingResponse
            {
                Id = d.Id,
                AdmissionId = d.AdmissionId,
                Er_StrokeArrival = d.Er_StrokeArrival,
                DoorToCT = d.DoorToCT,
                DoorToNeedle = d.DoorToNeedle,
                DoorToGroin = d.DoorToGroin,
                CTRecordedById = d.CTRecordedById,
                NeedleRecordedById = d.NeedleRecordedById,
                GroinRecordedById = d.GroinRecordedById,
                MinutesToCT = d.MinutesToCT,
                MinutesToNeedle = d.MinutesToNeedle,
                MinutesToGroin = d.MinutesToGroin,
                CTDelayed = d.CTDelayed,
                NeedleDelayed = d.NeedleDelayed
            };
        #endregion

        // GET: api/doortiming
        [HttpGet]
        public async Task<ActionResult<List<DoorTimingResponse>>> GetAll(CancellationToken cancellationToken)
        {
            var query = new GetListWithFiltersQuery<DoorTiming, DoorTimingResponse>(
                filter: d => d.CurrentState == (int)CurrentStatusType.Active,
                selector: ToResponse,
                orderBy: d => d.Er_StrokeArrival);

            var result = await _mediator.Send(query, cancellationToken);
            return Ok(result);
        }

        // GET: api/doortiming/paged?pageNumber=1&pageSize=10&admissionId=
        [HttpGet("paged")]
        public async Task<ActionResult<PagedResult<DoorTimingResponse>>> GetPaged(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] Guid? admissionId = null,
            CancellationToken cancellationToken = default)
        {
            var query = new GetPagedListQuery<DoorTiming, DoorTimingResponse>(
                filter: d => d.CurrentState == (int)CurrentStatusType.Active &&
                             (admissionId == null || d.AdmissionId == admissionId),
                selector: ToResponse,
                orderBy: d => d.Er_StrokeArrival,
                pageNumber: pageNumber,
                pageSize: pageSize);

            var result = await _mediator.Send(query, cancellationToken);
            return Ok(result);
        }

        // GET: api/doortiming/{id}
        [HttpGet("{id:guid}")]
        public async Task<ActionResult<DoorTimingResponse>> GetById(Guid id, CancellationToken cancellationToken)
        {
            var query = new GetByIdWithFiltersQuery<DoorTiming, DoorTimingResponse>(
                filter: d => d.Id == id,
                selector: ToResponse);

            var result = await _mediator.Send(query, cancellationToken);
            if (result == null)
                return NotFound();

            return Ok(result);
        }

        // POST: api/doortiming?actingUserId=...
        [HttpPost]
        public async Task<ActionResult<Guid>> Create(
            [FromBody] CreateDoorTimingRequest request,
            [FromQuery] Guid actingUserId,
            CancellationToken cancellationToken)
        {
            var doorTiming = _mapper.Map<DoorTiming>(request);

            var command = new AddAsyncGetIDCommand<DoorTiming>(doorTiming, actingUserId);
            var (success, entityId) = await _mediator.Send(command, cancellationToken);

            if (!success)
                return BadRequest();

            return CreatedAtAction(nameof(GetById), new { id = entityId }, entityId);
        }

        // PUT: api/doortiming?actingUserId=...
        // ملحوظة: تسجيل توقيت لقطة واحدة (مثلاً CT بس) هيبقاله Endpoint خاص بعدين،
        // دلوقتي التحديث بيستبدل كل الحقول مع بعض
        [HttpPut]
        public async Task<IActionResult> Update(
            [FromBody] UpdateDoorTimingRequest request,
            [FromQuery] Guid actingUserId,
            CancellationToken cancellationToken)
        {
            var doorTiming = _mapper.Map<DoorTiming>(request);

            var command = new UpdateCommand<DoorTiming>(doorTiming, actingUserId);
            var affectedRows = await _mediator.Send(command, cancellationToken);

            if (affectedRows == -1)
                return NotFound();

            return NoContent();
        }

        // PATCH: api/doortiming/{id}/status?actingUserId=...&status=1
        // مفيش DELETE هنا — تاريخ طبي، الإلغاء بيتم بس عن طريق ChangeStatus (Soft Delete)
        [HttpPatch("{id:guid}/status")]
        public async Task<IActionResult> ChangeStatus(
            Guid id,
            [FromQuery] Guid actingUserId,
            [FromQuery] int status,
            CancellationToken cancellationToken)
        {
            var command = new ChangeStatusCommand<DoorTiming>(id, actingUserId, status);
            await _mediator.Send(command, cancellationToken);
            return NoContent();
        }
    }
}
