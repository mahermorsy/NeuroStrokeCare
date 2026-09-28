using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using NeuroStrokeCare.Core.Features.BaseService.Commands.Models;
using NeuroStrokeCare.Core.Features.BaseService.Queries.Models;
using NeuroStrokeCare.Core.Features.Ward.Dtos;
using NeuroStrokeCare.Data.Entities;
using NeuroStrokeCare.Data.Enums;
using NeuroStrokeCare.Data.PageModel;

namespace NeuroStrokeCare.api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class WardController : ControllerBase
    {
        #region Fields
        private readonly IMediator _mediator;
        private readonly IMapper _mapper;
        #endregion

        #region Constructor
        public WardController(IMediator mediator, IMapper mapper)
        {
            _mediator = mediator;
            _mapper = mapper;
        }
        #endregion

        #region Selectors
        // نفس شكل التحويل لـ WardResponse متكرر في أكتر من Query، فبنعمله Expression واحدة نعيد استخدامها
        private static readonly System.Linq.Expressions.Expression<Func<Ward, WardResponse>> ToResponse =
            w => new WardResponse
            {
                Id = w.Id,
                Code = w.Code,
                Name = w.Name,
                TotalBeds = w.TotalBeds
            };
        #endregion

        // GET: api/ward
        [HttpGet]
        public async Task<ActionResult<List<WardResponse>>> GetAll(CancellationToken cancellationToken)
        {
            var query = new GetListWithFiltersQuery<Ward, WardResponse>(
                filter: w => w.CurrentState == (int)CurrentStatusType.Active,
                selector: ToResponse,
                orderBy: w => w.Name);

            var result = await _mediator.Send(query, cancellationToken);
            return Ok(result);
        }

        // GET: api/ward/paged?pageNumber=1&pageSize=10&search=
        [HttpGet("paged")]
        public async Task<ActionResult<PagedResult<WardResponse>>> GetPaged(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? search = null,
            CancellationToken cancellationToken = default)
        {
            var query = new GetPagedListQuery<Ward, WardResponse>(
                filter: w => w.CurrentState == (int)CurrentStatusType.Active &&
                             (string.IsNullOrEmpty(search) || w.Name.Contains(search) || w.Code.Contains(search)),
                selector: ToResponse,
                orderBy: w => w.Name,
                pageNumber: pageNumber,
                pageSize: pageSize);

            var result = await _mediator.Send(query, cancellationToken);
            return Ok(result);
        }

        // GET: api/ward/{id}
        [HttpGet("{id:guid}")]
        public async Task<ActionResult<WardResponse>> GetById(Guid id, CancellationToken cancellationToken)
        {
            var query = new GetByIdWithFiltersQuery<Ward, WardResponse>(
                filter: w => w.Id == id,
                selector: ToResponse);

            var result = await _mediator.Send(query, cancellationToken);
            if (result == null)
                return NotFound();

            return Ok(result);
        }

        // POST: api/ward?actingUserId=...
        // TODO: actingUserId مؤقتاً بتيجي من الـ Query String لحد ما نضيف الـ Authentication
        // ونجيبها من الـ Claims بتاعة اليوزر المسجل دخول بدل ما تتبعت من برة.
        [HttpPost]
        public async Task<ActionResult<Guid>> Create(
            [FromBody] CreateWardRequest request,
            [FromQuery] Guid actingUserId,
            CancellationToken cancellationToken)
        {
            var ward = _mapper.Map<Ward>(request);

            var command = new AddAsyncGetIDCommand<Ward>(ward, actingUserId);
            var (success, entityId) = await _mediator.Send(command, cancellationToken);

            if (!success)
                return BadRequest();

            return CreatedAtAction(nameof(GetById), new { id = entityId }, entityId);
        }

        // PUT: api/ward?actingUserId=...
        [HttpPut]
        public async Task<IActionResult> Update(
            [FromBody] UpdateWardRequest request,
            [FromQuery] Guid actingUserId,
            CancellationToken cancellationToken)
        {
            var ward = _mapper.Map<Ward>(request);

            var command = new UpdateCommand<Ward>(ward, actingUserId);
            var affectedRows = await _mediator.Send(command, cancellationToken);

            if (affectedRows == -1)
                return NotFound();

            return NoContent();
        }

        // PATCH: api/ward/{id}/status?actingUserId=...&status=1
        // بيستخدم للـ Soft Delete كمان (status = Inactive) بدل الحذف الفعلي
        [HttpPatch("{id:guid}/status")]
        public async Task<IActionResult> ChangeStatus(
            Guid id,
            [FromQuery] Guid actingUserId,
            [FromQuery] int status,
            CancellationToken cancellationToken)
        {
            var command = new ChangeStatusCommand<Ward>(id, actingUserId, status);
            await _mediator.Send(command, cancellationToken);
            return NoContent();
        }

        // DELETE: api/ward/{id}
        // ملحوظة: الحذف هنا فعلي (Hard Delete) — مقبول لأن Ward مش كيان طبي (تقرير/تاريخ مريض)،
        // مجرد إعدادات مكانية (أجنحة/أسرّة). للكيانات الطبية استخدم ChangeStatus بدل كدة.
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
        {
            var command = new DeleteCommand<Ward>(id);
            var success = await _mediator.Send(command, cancellationToken);

            if (!success)
                return NotFound();

            return NoContent();
        }
    }
}
