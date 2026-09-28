using AutoMapper;
using NeuroStrokeCare.Core.Features.LabResults.Dtos;
using Entities = NeuroStrokeCare.Data.Entities;

namespace NeuroStrokeCare.Core.Mapping
{
    public class LabResultsProfile : Profile
    {
        public LabResultsProfile()
        {
            CreateMap<CreateLabResultsRequest, Entities.LabResults>();
            CreateMap<UpdateLabResultsRequest, Entities.LabResults>();
            CreateMap<Entities.LabResults, LabResultsResponse>();
        }
    }
}
