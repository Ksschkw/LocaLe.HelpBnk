using System.Linq.Expressions;
using LocaLe.EscrowApi.Data;
using LocaLe.EscrowApi.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace LocaLe.EscrowApi.Repositories
{
    public class Repository<T> : IRepository<T> where T : class
    {
        protected readonly EscrowContext _context;
        protected readonly DbSet<T> _dbSet;

        public Repository(EscrowContext context)
        {
            _context = context;
            _dbSet = context.Set<T>();
        }

        public virtual async Task<T?> GetByIdAsync(Guid id)
        {
            return await _dbSet.FindAsync(id);
        }

        public virtual async Task<IEnumerable<T>> GetAllAsync()
        {
            return await _dbSet.ToListAsync();
        }

        public virtual async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate)
        {
            return await _dbSet.Where(predicate).ToListAsync();
        }

        public virtual async Task AddAsync(T entity)
        {
            await _dbSet.AddAsync(entity);
        }

        public virtual void Update(T entity)
        {
            _dbSet.Update(entity);
        }

        public virtual void Remove(T entity)
        {
            _dbSet.Remove(entity);
        }

        public virtual async Task<bool> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync() > 0;
        }

        // Transaction Support
        public virtual async Task<IDisposable> BeginTransactionAsync()
        {
            return await _context.Database.BeginTransactionAsync();
        }

        public virtual async Task CommitTransactionAsync()
        {
            if (_context.Database.CurrentTransaction != null)
                await _context.Database.CurrentTransaction.CommitAsync();
        }

        public virtual async Task RollbackTransactionAsync()
        {
            if (_context.Database.CurrentTransaction != null)
                await _context.Database.CurrentTransaction.RollbackAsync();
        }
    }
}
