using AutoMapper;
using NeuroStrokeCare.Core.Features.ICHAssessment.Dtos;
using Entities = NeuroStrokeCare.Data.Entities;

namespace NeuroStrokeCare.Core.Mapping
{
    public class ICHAssessmentProfile : Profile
    {
        public ICHAssessmentProfile()
        {
            CreateMap<CreateICHAssessmentRequest, Entities.ICHAssessment>();
            CreateMap<UpdateICHAssessmentRequest, Entities.ICHAssessment>();
            CreateMap<Entities.ICHAssessment, ICHAssessmentResponse>();
        }
    }
}
