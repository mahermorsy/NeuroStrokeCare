using AutoMapper;
using NeuroStrokeCare.Core.Features.NIHSSAssessment.Dtos;
using Entities = NeuroStrokeCare.Data.Entities;

namespace NeuroStrokeCare.Core.Mapping
{
    public class NIHSSAssessmentProfile : Profile
    {
        public NIHSSAssessmentProfile()
        {
            CreateMap<CreateNIHSSAssessmentRequest, Entities.NIHSSAssessment>();
            CreateMap<UpdateNIHSSAssessmentRequest, Entities.NIHSSAssessment>();
            CreateMap<Entities.NIHSSAssessment, NIHSSAssessmentResponse>();
        }
    }
}
