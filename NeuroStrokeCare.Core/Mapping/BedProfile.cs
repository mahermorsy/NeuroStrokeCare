using AutoMapper;
using NeuroStrokeCare.Core.Features.Bed.Dtos;
using NeuroStrokeCare.Data.Entities;

namespace NeuroStrokeCare.Core.Mapping
{
    public class BedProfile : Profile
    {
        public BedProfile()
        {
            CreateMap<CreateBedRequest, Bed>();
            CreateMap<UpdateBedRequest, Bed>();
            CreateMap<Bed, BedResponse>();
        }
    }
}
