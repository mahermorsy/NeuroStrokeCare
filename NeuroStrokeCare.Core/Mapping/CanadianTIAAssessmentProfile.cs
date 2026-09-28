using AutoMapper;
using NeuroStrokeCare.Core.Features.CanadianTIAAssessment.Dtos;
using Entities = NeuroStrokeCare.Data.Entities;

namespace NeuroStrokeCare.Core.Mapping
{
    public class CanadianTIAAssessmentProfile : Profile
    {
        public CanadianTIAAssessmentProfile()
        {
            CreateMap<CreateCanadianTIAAssessmentRequest, Entities.CanadianTIAAssessment>();
            CreateMap<UpdateCanadianTIAAssessmentRequest, Entities.CanadianTIAAssessment>();
            CreateMap<Entities.CanadianTIAAssessment, CanadianTIAAssessmentResponse>();
        }
    }
}
