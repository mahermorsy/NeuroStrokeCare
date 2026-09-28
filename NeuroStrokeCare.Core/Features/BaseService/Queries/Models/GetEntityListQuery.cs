using MediatR;
using NeuroStrokeCare.Data.Entities;
using System.Collections.Generic;

namespace NeuroStrokeCare.Core.Features.BaseService.Queries.Models
{
    public class GetEntityListQuery<T> : IRequest<List<T>> where T : BaseEntity
    {

    }
}