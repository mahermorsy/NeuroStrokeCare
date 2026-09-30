using AutoMapper;
using NeuroStrokeCare.Core.Features.FollowUpNote.Dtos;
using Entities = NeuroStrokeCare.Data.Entities;

namespace NeuroStrokeCare.Core.Mapping
{
    public class FollowUpNoteProfile : Profile
    {
        public FollowUpNoteProfile()
        {
            CreateMap<CreateFollowUpNoteRequest, Entities.FollowUpNote>();
            CreateMap<Entities.FollowUpNote, FollowUpNoteResponse>();
        }
    }
}
