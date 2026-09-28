using AutoMapper;
using NeuroStrokeCare.Core.Features.ASPECTSAssessment.Dtos;
using Entities = NeuroStrokeCare.Data.Entities;

namespace NeuroStrokeCare.Core.Mapping
{
    public class ASPECTSAssessmentProfile : Profile
    {
        public ASPECTSAssessmentProfile()
        {
            CreateMap<CreateASPECTSAssessmentRequest, Entities.ASPECTSAssessment>();
            CreateMap<UpdateASPECTSAssessmentRequest, Entities.ASPECTSAssessment>();
            CreateMap<Entities.ASPECTSAssessment, ASPECTSAssessmentResponse>();
        }
    }
}
