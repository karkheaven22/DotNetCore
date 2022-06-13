using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System;
using System.Collections.Specialized;

namespace DotNetCore.Models
{
    public interface IDatabaseTransaction<T> : IDisposable
    {
        void Commit();

        void Rollback();
    }

    public class EntityDbTransaction<T> : IDatabaseTransaction<T> where T : DbContext
    {
        private readonly IDbContextTransaction _transaction;

        public EntityDbTransaction(T context)
        {
            //context.Configuration.EnsureTransactionsForFunctionsAndCommands = true;
            _transaction = context.Database.BeginTransaction();
        }

        public void Commit()
        {
            _transaction.Commit();
        }

        public void Rollback()
        {
            _transaction.Rollback();
        }

        public void Dispose()
        {
            _transaction.Dispose();
            GC.SuppressFinalize(this);
        }
    }

    public interface INotifyRefresh : INotifyCollectionChanged
    {
        void OnRefresh();
    }
}
