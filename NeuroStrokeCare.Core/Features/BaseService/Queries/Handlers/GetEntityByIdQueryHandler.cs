using MediatR;
using NeuroStrokeCare.Core.Features.BaseService.Queries.Models;
using NeuroStrokeCare.Data.Entities;
using NeuroStrokeCare.infrastructure.Intertfaces;
using System.Threading;
using System.Threading.Tasks;

namespace NeuroStrokeCare.Core.Features.BaseService.Queries.Handlers
{
    public class GetEntityByIdQueryHandler<T> : IRequestHandler<GetEntityByIdQuery<T>, T?> where T : BaseEntity
    {
        #region Fields
        private readonly IUnitOfWork _unitOfWork;
        #endregion  


        #region constructor
        public GetEntityByIdQueryHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }
        #endregion

        #region Methods
        public async Task<T?> Handle(GetEntityByIdQuery<T> request, CancellationToken cancellationToken)
        {
            return await _unitOfWork.Repository<T>().GetByIdAsync(request.Id, cancellationToken);
        }
        #endregion  
    }
}
