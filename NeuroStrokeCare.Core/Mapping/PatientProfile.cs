using AutoMapper;
using NeuroStrokeCare.Core.Features.Patient.Dtos;
using NeuroStrokeCare.Data.Entities;

namespace NeuroStrokeCare.Core.Mapping
{
    public class PatientProfile : Profile
    {
        public PatientProfile()
        {
            CreateMap<CreatePatientRequest, Patient>();
            CreateMap<UpdatePatientRequest, Patient>();
            CreateMap<Patient, PatientResponse>();
        }
    }
}
