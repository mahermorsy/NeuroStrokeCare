using AutoMapper;
using NeuroStrokeCare.Core.Features.GCSAssessment.Dtos;
using Entities = NeuroStrokeCare.Data.Entities.Assessments;

namespace NeuroStrokeCare.Core.Mapping
{
    public class GCSAssessmentProfile : Profile
    {
        public GCSAssessmentProfile()
        {
            CreateMap<CreateGCSAssessmentRequest, Entities.GCSAssessment>();
            CreateMap<UpdateGCSAssessmentRequest, Entities.GCSAssessment>();
            CreateMap<Entities.GCSAssessment, GCSAssessmentResponse>();
        }
    }
}
