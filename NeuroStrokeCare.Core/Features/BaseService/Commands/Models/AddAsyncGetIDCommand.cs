using MediatR;
using System;
using System.Collections.Generic;
using NeuroStrokeCare.Data.Entities;

using System.Text;

namespace NeuroStrokeCare.Core.Features.BaseService.Commands.Models
{
    public class AddAsyncGetIDCommand<T> : IRequest<(bool Success, Guid EntityId)> where T : BaseEntity
    {
        public T Entity { get; set; }
        public Guid CreatedById { get; set; }
        public AddAsyncGetIDCommand(T entity, Guid createdById)
        {
            Entity = entity;
            CreatedById = createdById;
        }   
    
    }
}
