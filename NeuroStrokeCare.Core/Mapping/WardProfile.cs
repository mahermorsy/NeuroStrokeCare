using AutoMapper;
using NeuroStrokeCare.Core.Features.Ward.Dtos;
using NeuroStrokeCare.Data.Entities;

namespace NeuroStrokeCare.Core.Mapping
{
    public class WardProfile : Profile
    {
        public WardProfile()
        {
            CreateMap<CreateWardRequest, Ward>();
            CreateMap<UpdateWardRequest, Ward>();
            CreateMap<Ward, WardResponse>();
        }
    }
}
