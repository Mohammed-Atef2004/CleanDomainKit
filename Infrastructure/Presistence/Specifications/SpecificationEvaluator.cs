using Domain.SharedKernel;
using Domain.Specifications;
using Infrastructure.Persistence.Specifications;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Presistence.Specifications
{
    public static class SpecificationEvaluator<TEntity> where TEntity : Entity<Guid>
    {
        public static IQueryable<TEntity> GetQuery(
            IQueryable<TEntity> inputQuery,
            ISpecification<TEntity> spec)
        {
            var query = inputQuery;

            // Apply Criteria
            if (spec.Criteria != null)
            {
                query = query.Where(spec.Criteria);
            }
               
           
            // Apply Includes
            if (spec.Includes.Any())
            {
                foreach (var include in spec.Includes)
                {
                    query = query.Include(include);
                }
            }
               

            // Apply Ordering
            if (spec.Orderings.Any())
            {
                IOrderedQueryable<TEntity>? orderedQuery = null;

                for (int i = 0; i < spec.Orderings.Count; i++)
                {
                    var (keySelector, isDesc) = spec.Orderings[i];

                    if (i == 0)
                    {
                        orderedQuery = isDesc
                            ? query.OrderByDescending(keySelector)
                            : query.OrderBy(keySelector);
                    }
                    else
                    {
                        orderedQuery = isDesc
                            ? orderedQuery!.ThenByDescending(keySelector)
                            : orderedQuery!.ThenBy(keySelector);
                    }
                }

                query = orderedQuery!;
            }

            // Apply Paging
            if (spec.IsPagingEnabled)
            {
                query = query.Skip(spec.Skip!.Value)
                             .Take(spec.Take!.Value);
            }

            return query;
        }
    }
}
