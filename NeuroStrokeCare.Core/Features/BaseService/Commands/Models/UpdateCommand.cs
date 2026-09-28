using MediatR;
using NeuroStrokeCare.Data.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace NeuroStrokeCare.Core.Features.BaseService.Commands.Models
{
    public class UpdateCommand<T> : IRequest<int> where T : BaseEntity
    {
        public T Entity { get; set; }
        public Guid UserId { get; set; }
        public UpdateCommand(T entity, Guid userId)
        {
            Entity = entity;
            UserId = userId;
        }   
    }
}
