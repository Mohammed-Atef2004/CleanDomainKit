using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Specifications
{
    public interface ISpecification<TEntity>
    {
        Expression<Func<TEntity, bool>> Criteria { get; }
        IReadOnlyList<Expression<Func<TEntity, object>>> Includes { get; }
        IReadOnlyList<(Expression<Func<TEntity, object>> KeySelector, bool IsDescending)> Orderings { get; }
        int? Skip { get; }
        int? Take { get; }
        bool IsPagingEnabled { get; }
    }
}
