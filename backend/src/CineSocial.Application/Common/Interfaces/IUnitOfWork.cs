using CineSocial.Application.Common.Results;
using CineSocial.Domain.Common;

namespace CineSocial.Application.Common.Interfaces;

/// <summary>
/// Unit of Work pattern for managing transactions and repositories
/// </summary>
public interface IUnitOfWork : IDisposable
{
    /// <summary>
    /// Gets a repository for the specified entity type
    /// </summary>
    IRepository<T> Repository<T>() where T : BaseEntity;

    /// <summary>
    /// Saves all changes to the database
    /// </summary>
    Task<Result<int>> SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Begins a database transaction
    /// </summary>
    Task<Result> BeginTransactionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Commits the current transaction
    /// </summary>
    Task<Result> CommitTransactionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Rolls back the current transaction
    /// </summary>
    Task<Result> RollbackTransactionAsync(CancellationToken cancellationToken = default);
}
