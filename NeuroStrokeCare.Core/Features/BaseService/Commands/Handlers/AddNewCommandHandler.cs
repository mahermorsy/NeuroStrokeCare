using MediatR;
using NeuroStrokeCare.Core.Features.BaseService.Commands.Models;
using NeuroStrokeCare.Data.Entities;
using NeuroStrokeCare.infrastructure.Intertfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace NeuroStrokeCare.Core.Features.BaseService.Commands.Handlers
{
    public class AddNewCommandHandler<T> : IRequestHandler<AddNewCommand<T>, bool> where T : BaseEntity 
    {
        #region Fields
        private readonly IUnitOfWork _unitOfWork;
        #endregion


        #region constructor
        public AddNewCommandHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }
        #endregion

        #region Methods
        public async Task<bool> Handle(AddNewCommand<T> request, CancellationToken cancellationToken)
        {

            return await _unitOfWork.Repository<T>().AddAsync(request.Entity, request.CreatedById, cancellationToken);

        }
        #endregion 
    }
}
