using MediatR;
using NeuroStrokeCare.Core.Features.BaseService.Commands.Models;
using NeuroStrokeCare.Core.Features.BaseService.Queries.Models;
using NeuroStrokeCare.Data.Entities;
using NeuroStrokeCare.infrastructure.Intertfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace NeuroStrokeCare.Core.Features.BaseService.Commands.Handlers
{
    public class UpdateFieldCommandHandler<T> : IRequestHandler<UpdateFieldCommand<T>, bool> where T : BaseEntity
    {
        #region Fields
        private readonly IUnitOfWork _unitOfWork;
        #endregion


        #region constructor
        public UpdateFieldCommandHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }
        #endregion

        #region Methods
        public async Task<bool> Handle(UpdateFieldCommand<T> request, CancellationToken cancellationToken)
        {

            return await _unitOfWork.Repository<T>().UpdateFieldsync(request.ID , request.updateAction, cancellationToken);
           
        }
        #endregion
    }
}
