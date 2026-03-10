
using System.Numerics;

namespace Domain.Interfaces.Repositories
{
    public interface IUnitOfWork : IDisposable
    {
     


        Task<int> CompleteAsync(CancellationToken cancellationToken = default);
        Task<int> RollbackAsync(CancellationToken cancellationToken = default);
    }
}