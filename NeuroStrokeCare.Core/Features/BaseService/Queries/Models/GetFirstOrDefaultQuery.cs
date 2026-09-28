using MediatR;
using NeuroStrokeCare.Data.Entities;
using System;
using System.Linq.Expressions;

namespace NeuroStrokeCare.Core.Features.BaseService.Queries.Models
{
    public class GetFirstOrDefaultQuery<T> : IRequest<T?> where T : BaseEntity
    {
        public Expression<Func<T, bool>> Filter { get; set; }

        public GetFirstOrDefaultQuery(Expression<Func<T, bool>> filter)
        {
            Filter = filter;
        }
    }
}