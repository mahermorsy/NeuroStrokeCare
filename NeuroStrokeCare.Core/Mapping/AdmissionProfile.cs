using AutoMapper;
using NeuroStrokeCare.Core.Features.Admission.Dtos;
using Entities = NeuroStrokeCare.Data.Entities;

namespace NeuroStrokeCare.Core.Mapping
{
    public class AdmissionProfile : Profile
    {
        public AdmissionProfile()
        {
            CreateMap<CreateAdmissionRequest, Entities.Admission>();
            CreateMap<UpdateAdmissionRequest, Entities.Admission>();
            CreateMap<Entities.Admission, AdmissionResponse>();
        }
    }
}
