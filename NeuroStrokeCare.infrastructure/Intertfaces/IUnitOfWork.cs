using NeuroStrokeCare.Data.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace NeuroStrokeCare.infrastructure.Intertfaces
{
    public interface IUnitOfWork : IAsyncDisposable
    {
        IGenericRepository<T> Repository<T>() where T : BaseEntity;
        Task BeginTransactionAsync();
        Task CommitAsync();
        Task RollbackAsync();
        Task<int> SaveChangesAsync();

    }

}
