using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NeuroStrokeCare.Data.Constants;
using NeuroStrokeCare.Core.Features.BaseService.Commands.Models;
using NeuroStrokeCare.Core.Features.BaseService.Queries.Models;
using NeuroStrokeCare.Core.Features.Bed.Dtos;
using NeuroStrokeCare.Data.Entities;
using NeuroStrokeCare.Data.Enums;
using NeuroStrokeCare.Data.PageModel;

namespace NeuroStrokeCare.api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BedController : ControllerBase
    {
        #region Fields
        private readonly IMediator _mediator;
        private readonly IMapper _mapper;
        #endregion

        #region Constructor
        public BedController(IMediator mediator, IMapper mapper)
        {
            _mediator = mediator;
            _mapper = mapper;
        }
        #endregion

        #region Selectors
        private static readonly System.Linq.Expressions.Expression<Func<Bed, BedResponse>> ToResponse =
            b => new BedResponse
            {
                Id = b.Id,
                WardId = b.WardId,
                BedNumber = b.BedNumber,
                Status = b.Status
            };
        #endregion

        // GET: api/bed
        [HttpGet]
        public async Task<ActionResult<List<BedResponse>>> GetAll(CancellationToken cancellationToken)
        {
            var query = new GetListWithFiltersQuery<Bed, BedResponse>(
                filter: b => b.CurrentState == (int)CurrentStatusType.Active,
                selector: ToResponse,
                orderBy: b => b.BedNumber);

            var result = await _mediator.Send(query, cancellationToken);
            return Ok(result);
        }

        // GET: api/bed/paged?pageNumber=1&pageSize=10&wardId=
        [HttpGet("paged")]
        public async Task<ActionResult<PagedResult<BedResponse>>> GetPaged(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] Guid? wardId = null,
            CancellationToken cancellationToken = default)
        {
            var query = new GetPagedListQuery<Bed, BedResponse>(
                filter: b => b.CurrentState == (int)CurrentStatusType.Active &&
                             (wardId == null || b.WardId == wardId),
                selector: ToResponse,
                orderBy: b => b.BedNumber,
                pageNumber: pageNumber,
                pageSize: pageSize);

            var result = await _mediator.Send(query, cancellationToken);
            return Ok(result);
        }

        // GET: api/bed/{id}
        [HttpGet("{id:guid}")]
        public async Task<ActionResult<BedResponse>> GetById(Guid id, CancellationToken cancellationToken)
        {
            var query = new GetByIdWithFiltersQuery<Bed, BedResponse>(
                filter: b => b.Id == id,
                selector: ToResponse);

            var result = await _mediator.Send(query, cancellationToken);
            if (result == null)
                return NotFound();

            return Ok(result);
        }

        // POST: api/bed?actingUserId=...
        [Authorize(Roles = Roles.Admin)]
        [HttpPost]
        public async Task<ActionResult<Guid>> Create(
            [FromBody] CreateBedRequest request,
            [FromQuery] Guid actingUserId,
            CancellationToken cancellationToken)
        {
            var bed = _mapper.Map<Bed>(request);

            var command = new AddAsyncGetIDCommand<Bed>(bed, actingUserId);
            var (success, entityId) = await _mediator.Send(command, cancellationToken);

            if (!success)
                return BadRequest();

            return CreatedAtAction(nameof(GetById), new { id = entityId }, entityId);
        }

        // PUT: api/bed?actingUserId=...
        [Authorize(Roles = Roles.Admin)]
        [HttpPut]
        public async Task<IActionResult> Update(
            [FromBody] UpdateBedRequest request,
            [FromQuery] Guid actingUserId,
            CancellationToken cancellationToken)
        {
            var bed = _mapper.Map<Bed>(request);

            var command = new UpdateCommand<Bed>(bed, actingUserId);
            var affectedRows = await _mediator.Send(command, cancellationToken);

            if (affectedRows == -1)
                return NotFound();

            return NoContent();
        }

        // PATCH: api/bed/{id}/status?actingUserId=...&status=1
        [Authorize(Roles = Roles.Admin)]
        [HttpPatch("{id:guid}/status")]
        public async Task<IActionResult> ChangeStatus(
            Guid id,
            [FromQuery] Guid actingUserId,
            [FromQuery] int status,
            CancellationToken cancellationToken)
        {
            var command = new ChangeStatusCommand<Bed>(id, actingUserId, status);
            await _mediator.Send(command, cancellationToken);
            return NoContent();
        }

        // DELETE: api/bed/{id}
        // مقبول هنا: Bed إعداد مكاني (زي Ward)، مش تقرير طبي أو تاريخ مريض.
        [Authorize(Roles = Roles.Admin)]
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
        {
            var command = new DeleteCommand<Bed>(id);
            var success = await _mediator.Send(command, cancellationToken);

            if (!success)
                return NotFound();

            return NoContent();
        }
    }
}
