using AutoMapper;
using NeuroStrokeCare.Core.Features.DoorTiming.Dtos;
using Entities = NeuroStrokeCare.Data.Entities;

namespace NeuroStrokeCare.Core.Mapping
{
    public class DoorTimingProfile : Profile
    {
        public DoorTimingProfile()
        {
            CreateMap<CreateDoorTimingRequest, Entities.DoorTiming>();
            CreateMap<UpdateDoorTimingRequest, Entities.DoorTiming>();
            CreateMap<Entities.DoorTiming, DoorTimingResponse>();
        }
    }
}
