using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NeuroStrokeCare.Core.Features.BaseService.Commands.Models;
using NeuroStrokeCare.Core.Features.BaseService.Queries.Models;
using NeuroStrokeCare.Core.Features.FollowUpNote.Dtos;
using NeuroStrokeCare.Data.Constants;
using NeuroStrokeCare.Data.Entities;
using NeuroStrokeCare.Data.Enums;
using NeuroStrokeCare.Data.PageModel;

namespace NeuroStrokeCare.api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FollowUpNoteController : ControllerBase
    {
        #region Fields
        private readonly IMediator _mediator;
        private readonly IMapper _mapper;
        #endregion

        #region Constructor
        public FollowUpNoteController(IMediator mediator, IMapper mapper)
        {
            _mediator = mediator;
            _mapper = mapper;
        }
        #endregion

        #region Selectors
        private static readonly System.Linq.Expressions.Expression<Func<FollowUpNote, FollowUpNoteResponse>> ToResponse =
            n => new FollowUpNoteResponse
            {
                Id = n.Id,
                AdmissionId = n.AdmissionId,
                Content = n.Content,
                NoteType = n.NoteType,
                AuthorRole = n.AuthorRole,
                CreatedBy = n.CreatedBy,
                CreatedAt = n.CreatedAt
            };
        #endregion

        // GET: api/followupnote?admissionId=
        [HttpGet]
        public async Task<ActionResult<List<FollowUpNoteResponse>>> GetAll(
            [FromQuery] Guid? admissionId,
            CancellationToken cancellationToken)
        {
            var query = new GetListWithFiltersQuery<FollowUpNote, FollowUpNoteResponse>(
                filter: n => n.CurrentState == (int)CurrentStatusType.Active &&
                             (admissionId == null || n.AdmissionId == admissionId),
                selector: ToResponse,
                orderBy: n => n.CreatedAt);

            var result = await _mediator.Send(query, cancellationToken);
            return Ok(result);
        }

        // GET: api/followupnote/paged?pageNumber=1&pageSize=10&admissionId=
        [HttpGet("paged")]
        public async Task<ActionResult<PagedResult<FollowUpNoteResponse>>> GetPaged(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] Guid? admissionId = null,
            CancellationToken cancellationToken = default)
        {
            var query = new GetPagedListQuery<FollowUpNote, FollowUpNoteResponse>(
                filter: n => n.CurrentState == (int)CurrentStatusType.Active &&
                             (admissionId == null || n.AdmissionId == admissionId),
                selector: ToResponse,
                orderBy: n => n.CreatedAt,
                pageNumber: pageNumber,
                pageSize: pageSize);

            var result = await _mediator.Send(query, cancellationToken);
            return Ok(result);
        }

        // GET: api/followupnote/{id}
        [HttpGet("{id:guid}")]
        public async Task<ActionResult<FollowUpNoteResponse>> GetById(Guid id, CancellationToken cancellationToken)
        {
            // FINAL RELEASE-CANDIDATE PASS (section 6, "Soft delete integrity"): CONFIRMED BUG,
            // FIXED HERE. This GetById used to filter on Id alone, so a soft-deleted row
            // (CurrentState != Active) was still returned as a normal 200 through this exact
            // same endpoint any ordinary clinical read uses - there was no separate "admin can
            // still see deleted records" endpoint being bypassed here, this WAS the only read
            // path, and it did not distinguish. Adding the CurrentState check makes a
            // soft-deleted record 404 here, consistent with GetAll/paged (which already filter
            // on CurrentState) and with how every write endpoint already treats "not found".
            var query = new GetByIdWithFiltersQuery<FollowUpNote, FollowUpNoteResponse>(
                filter: n => n.Id == id && n.CurrentState == (int)CurrentStatusType.Active,
                selector: ToResponse);

            var result = await _mediator.Send(query, cancellationToken);
            if (result == null)
                return NotFound();

            return Ok(result);
        }

        // POST: api/followupnote?actingUserId=...
        // مفتوح لأي دور إكلينيكي (دكاترة + تمريض) - AnyClinical
        [Authorize(Roles = Roles.AnyClinical)]
        [HttpPost]
        public async Task<ActionResult<Guid>> Create(
            [FromBody] CreateFollowUpNoteRequest request,
            [FromQuery] Guid actingUserId,
            CancellationToken cancellationToken)
        {
            var note = _mapper.Map<FollowUpNote>(request);

            var command = new AddAsyncGetIDCommand<FollowUpNote>(note, actingUserId);
            var (success, entityId) = await _mediator.Send(command, cancellationToken);

            if (!success)
                return BadRequest();

            return CreatedAtAction(nameof(GetById), new { id = entityId }, entityId);
        }

        // PATCH: api/followupnote/{id}/status?actingUserId=...&status=0
        // إلغاء ملاحظة اتكتبت غلط - مفيش Update ولا DELETE عمدًا، السجل الطبي ثابت
        [Authorize(Roles = Roles.AnyClinical)]
        [HttpPatch("{id:guid}/status")]
        public async Task<IActionResult> ChangeStatus(
            Guid id,
            [FromQuery] Guid actingUserId,
            [FromQuery] int status,
            CancellationToken cancellationToken)
        {
            var command = new ChangeStatusCommand<FollowUpNote>(id, actingUserId, status);
            await _mediator.Send(command, cancellationToken);
            return NoContent();
        }
    }
}
