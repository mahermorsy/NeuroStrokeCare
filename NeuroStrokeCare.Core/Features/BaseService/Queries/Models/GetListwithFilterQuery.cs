using MediatR;
using NeuroStrokeCare.Data.Entities;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace NeuroStrokeCare.Core.Features.BaseService.Queries.Models
{
    public class GetListwithFilterQuery<T> : IRequest<List<T>> where T : BaseEntity
    {

        public Expression<Func<T, bool>> Filter { get; set; }

        public GetListwithFilterQuery(Expression<Func<T, bool>> filter)
        {
            Filter = filter;
        }
    }
}
