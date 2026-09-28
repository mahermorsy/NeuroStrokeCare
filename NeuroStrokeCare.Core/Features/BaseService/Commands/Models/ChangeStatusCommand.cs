using MediatR;
using NeuroStrokeCare.Data.Entities;
using NeuroStrokeCare.Data.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace NeuroStrokeCare.Core.Features.BaseService.Commands.Models
{
    public class ChangeStatusCommand<T> : IRequest<int> where T : BaseEntity
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public int Status { get; set; } = (int)CurrentStatusType.Active;

        public ChangeStatusCommand(Guid id, Guid Userid, int status = (int)CurrentStatusType.Active)
        {
            Id = id;
            UserId = Userid;
            Status = status;
        }
    }
}
