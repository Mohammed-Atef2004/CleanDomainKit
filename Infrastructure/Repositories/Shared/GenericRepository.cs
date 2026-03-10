using System.Linq.Expressions;
using Domain.Common;
using Domain.Interfaces.Repositories;
using Infrastructure.Presistence.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories.Shared
{


    public class GenericRepository<T> : IGenericRepository<T> where T : class
    {
        protected readonly AppDbContext _context;
        protected readonly DbSet<T> _dbSet;

        public GenericRepository(AppDbContext context)
        {
            _context = context;
            _dbSet = context.Set<T>();
        }

        // Always NoTracking for better read performance
        public virtual IQueryable<T> EntityQuery => _dbSet.AsNoTracking();

        public async Task AddAsync(T entity) => await _dbSet.AddAsync(entity);

        public void Update(T entity) => _dbSet.Update(entity);

        public void Delete(T entity) => _dbSet.Remove(entity);

        public async Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null)
        {
            return predicate != null
                ? await _dbSet.CountAsync(predicate)
                : await _dbSet.CountAsync();
        }
    }
}