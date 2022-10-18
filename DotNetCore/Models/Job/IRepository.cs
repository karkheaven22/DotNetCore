using Microsoft.EntityFrameworkCore;
using System;

namespace DotNetCore.Models
{
    public interface IRepository<T> : IDisposable
    {
        IDatabaseTransaction<T> BeginTransaction();

        int Save();
    }

    public class Repository<T> : IRepository<T> where T : DbContext
    {
        private readonly T _context;

        public Repository(T context)
        {
            _context = context;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            _context.Dispose();
        }

        public IDatabaseTransaction<T> BeginTransaction()
        {
            return new EntityDbTransaction<T>(_context);
        }

        public int Save()
        {
            return _context.SaveChanges();
        }
    }
}