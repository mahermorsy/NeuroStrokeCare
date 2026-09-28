using AutoMapper;
using NeuroStrokeCare.Core.Features.GUSSAssessment.Dtos;
using Entities = NeuroStrokeCare.Data.Entities.Assessments;

namespace NeuroStrokeCare.Core.Mapping
{
    public class GUSSAssessmentProfile : Profile
    {
        public GUSSAssessmentProfile()
        {
            CreateMap<CreateGUSSAssessmentRequest, Entities.GUSSAssessment>();
            CreateMap<UpdateGUSSAssessmentRequest, Entities.GUSSAssessment>();
            CreateMap<Entities.GUSSAssessment, GUSSAssessmentResponse>();
        }
    }
}
