using MediatR;
using NeuroStrokeCare.Core.Features.BaseService.Queries.Models;
using NeuroStrokeCare.Data.Entities;
using NeuroStrokeCare.infrastructure.Intertfaces;
using System;
using System.Collections.Generic;
using System.Text;


namespace NeuroStrokeCare.Core.Features.BaseService.Queries.Handlers
{
    public class GetListWithFiltersQueryHandler<T, Tresult> : IRequestHandler<GetListWithFiltersQuery<T, Tresult> , List<Tresult>> where T : BaseEntity
    {
        #region Fields
        private readonly IUnitOfWork _unitOfWork;
        #endregion  


        #region constructor
        public GetListWithFiltersQueryHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }
        #endregion

        #region Methods
        public async Task<List<Tresult>> Handle(GetListWithFiltersQuery<T, Tresult> request, CancellationToken cancellationToken)
        {
            return await _unitOfWork.Repository<T>().GetListAsync(request.Filter, request.Selector, request.OrderBy, request.IsDescending, cancellationToken);
        }
        #endregion
    }
}
