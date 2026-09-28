using AutoMapper;
using NeuroStrokeCare.Core.Features.BradenAssessment.Dtos;
using Entities = NeuroStrokeCare.Data.Entities.Assessments;

namespace NeuroStrokeCare.Core.Mapping
{
    public class BradenAssessmentProfile : Profile
    {
        public BradenAssessmentProfile()
        {
            CreateMap<CreateBradenAssessmentRequest, Entities.BradenAssessment>();
            CreateMap<UpdateBradenAssessmentRequest, Entities.BradenAssessment>();
            CreateMap<Entities.BradenAssessment, BradenAssessmentResponse>();
        }
    }
}
