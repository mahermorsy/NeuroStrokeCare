using MediatR;
using NeuroStrokeCare.Core.Features.BaseService.Queries.Models;
using NeuroStrokeCare.Data.Entities;
using NeuroStrokeCare.infrastructure.Intertfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace NeuroStrokeCare.Core.Features.BaseService.Queries.Handlers
{
    public class GetFirstOrDefaultQueryHandler<T> : IRequestHandler<GetFirstOrDefaultQuery<T>, T?> where T : BaseEntity
    {
        #region Fields
        private readonly IUnitOfWork _unitOfWork;
        #endregion


        #region constructor
        public GetFirstOrDefaultQueryHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }
        #endregion

        #region Methods
        public async Task<T?> Handle(GetFirstOrDefaultQuery<T> request, CancellationToken cancellationToken)
        {
            return await _unitOfWork.Repository<T>().GetFirstOrDefault(request.Filter);
        }
        #endregion
    }
}
