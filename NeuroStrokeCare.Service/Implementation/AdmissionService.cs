using NeuroStrokeCare.Service.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;
using NeuroStrokeCare.Data.Entities;
namespace NeuroStrokeCare.Service.Implementation
{
    public class AdmissionService : BaseService<Admission, Admission> , IAdmissionService
    {
        public AdmissionService(IServiceProvider serviceProvider) : base(serviceProvider)
        {

        }
    }
}
