using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

using System.Linq.Expressions;

namespace Domain.Interfaces.Repositories;

public interface IGenericRepository<T> where T : class
{
    // The key to deferred execution
    IQueryable<T> EntityQuery { get; }

    // Basic Persistence
    Task AddAsync(T entity);
    void Update(T entity);
    void Delete(T entity);

    // Helpers for simple cases
    Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null);
}
