using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using NeuroStrokeCare.Data.Entities;
using NeuroStrokeCare.infrastructure.Context;
using NeuroStrokeCare.infrastructure.Intertfaces;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace NeuroStrokeCare.infrastructure.Repositories
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly NeuroFlowDbContext _DBContext;
        private readonly ConcurrentDictionary<Type, object> _repositories = new ConcurrentDictionary<Type, object>();
        private IDbContextTransaction? _transaction;
        private readonly ILoggerFactory _logger;


        public UnitOfWork(NeuroFlowDbContext DBContext, ILoggerFactory logger)
        {
            _DBContext = DBContext;
            _logger = logger;
        }
        public IGenericRepository<T> Repository<T>() where T : BaseEntity
        {
            return (IGenericRepository<T>)_repositories.GetOrAdd(
                typeof(T), _ => new GenericRepository<T>(_DBContext, _logger.CreateLogger<GenericRepository<T>>()));

        }
      
        public async Task BeginTransactionAsync()
        {
            _transaction = await _DBContext.Database.BeginTransactionAsync();
        }

        public async Task CommitAsync()
        {
            await _DBContext.SaveChangesAsync();

            if (_transaction != null)
            {
                await _transaction.CommitAsync();
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }
        public async Task RollbackAsync()
        {
            if (_transaction != null)
            {
                await _transaction.RollbackAsync();
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }

        public async ValueTask DisposeAsync()
        {
            if (_transaction != null)
                await _transaction.DisposeAsync();
            await _DBContext.DisposeAsync();
        }

        public async Task<int> SaveChangesAsync()
        {
            return await _DBContext.SaveChangesAsync();
        }
    }
}
