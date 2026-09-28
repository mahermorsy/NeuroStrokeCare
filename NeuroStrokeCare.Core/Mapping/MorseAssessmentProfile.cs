using AutoMapper;
using NeuroStrokeCare.Core.Features.MorseAssessment.Dtos;
using Entities = NeuroStrokeCare.Data.Entities.Assessments;

namespace NeuroStrokeCare.Core.Mapping
{
    public class MorseAssessmentProfile : Profile
    {
        public MorseAssessmentProfile()
        {
            CreateMap<CreateMorseAssessmentRequest, Entities.MorseAssessment>();
            CreateMap<UpdateMorseAssessmentRequest, Entities.MorseAssessment>();
            CreateMap<Entities.MorseAssessment, MorseAssessmentResponse>();
        }
    }
}
