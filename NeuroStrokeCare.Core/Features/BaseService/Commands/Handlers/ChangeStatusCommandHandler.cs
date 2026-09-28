using MediatR;
using NeuroStrokeCare.Core.Features.BaseService.Commands.Handlers;
using NeuroStrokeCare.Core.Features.BaseService.Commands.Models;
using NeuroStrokeCare.Data.Entities;
using NeuroStrokeCare.infrastructure.Intertfaces;
using System;
using System.Collections.Generic;
using System.Text;


namespace NeuroStrokeCare.Core.Features.BaseService.Commands.Handlers
{
    public class ChangeStatusCommandHandler<T> : IRequestHandler<ChangeStatusCommand<T>, int>  where T : BaseEntity
    {
        #region Fields
        private readonly IUnitOfWork _unitOfWork;
        #endregion


        #region constructor
        public ChangeStatusCommandHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }
        #endregion

        #region Methods
        public async Task<int> Handle(ChangeStatusCommand<T> request, CancellationToken cancellationToken)
        {

            return await _unitOfWork.Repository<T>().ChangeStatus(request.Id, request.UserId, request.Status, cancellationToken);

        }
        #endregion 
    }
}
