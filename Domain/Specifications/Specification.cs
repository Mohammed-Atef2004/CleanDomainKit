using Domain.SharedKernel;
using Domain.Specifications;
using MassTransit;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Infrastructure.Persistence.Specifications
{
    /// <summary>
    /// Base Specification class used to encapsulate query logic.
    /// Designed for use with EF Core and DDD patterns.
    /// </summary>
    /// <typeparam name="TEntity">The entity type</typeparam>
    public abstract class Specification<TEntity> where TEntity : Entity<Guid>, ISpecification

    {
        // Backing fields (private to ensure controlled mutation)
        private readonly List<Expression<Func<TEntity, object>>> _includes = new();
        private readonly List<(Expression<Func<TEntity, object>> KeySelector, bool IsDescending)> _orderings = new();

        /// <summary>
        /// Initializes a new specification with optional filtering criteria.
        /// If no criteria is provided, a default "true" expression is used.
        /// </summary>
        protected Specification(Expression<Func<TEntity, bool>>? criteria = null)
        {
            Criteria = criteria ?? (_ => true);
        }

        /// <summary>
        /// Filtering condition for the query.
        /// Always non-null to simplify query building.
        /// </summary>
        public Expression<Func<TEntity, bool>> Criteria { get; }

        /// <summary>
        /// Navigation properties to include (for eager loading).
        /// Exposed as read-only to prevent external modification.
        /// </summary>
        public IReadOnlyList<Expression<Func<TEntity, object>>> Includes => _includes;

        /// <summary>
        /// Ordering expressions (supports multiple OrderBy / ThenBy).
        /// </summary>
        public IReadOnlyList<(Expression<Func<TEntity, object>> KeySelector, bool IsDescending)> Orderings => _orderings;

        /// <summary>
        /// Number of records to skip (for pagination).
        /// </summary>
        public int? Skip { get; private set; }

        /// <summary>
        /// Number of records to take (for pagination).
        /// </summary>
        public int? Take { get; private set; }

        /// <summary>
        /// Indicates whether pagination should be applied.
        /// </summary>
        public bool IsPagingEnabled => Skip.HasValue && Take.HasValue;

        #region Protected Helpers (for derived specifications)

        /// <summary>
        /// Adds a navigation property to be eagerly loaded.
        /// </summary>
        protected void AddInclude(Expression<Func<TEntity, object>> includeExpression)
        {
            _includes.Add(includeExpression);
        }

        /// <summary>
        /// Adds an ascending order expression.
        /// Multiple calls will result in ThenBy chaining.
        /// </summary>
        protected void AddOrderBy(Expression<Func<TEntity, object>> keySelector)
        {
            _orderings.Add((keySelector, false));
        }

        /// <summary>
        /// Adds a descending order expression.
        /// Multiple calls will result in ThenByDescending chaining.
        /// </summary>
        protected void AddOrderByDescending(Expression<Func<TEntity, object>> keySelector)
        {
            _orderings.Add((keySelector, true));
        }

        /// <summary>
        /// Applies pagination to the query.
        /// </summary>
        protected void ApplyPaging(int skip, int take)
        {
            Skip = skip;
            Take = take;
        }

        #endregion
    }
}