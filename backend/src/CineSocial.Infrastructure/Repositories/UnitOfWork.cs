using CineSocial.Application.Common.Interfaces;
using CineSocial.Application.Common.Results;
using CineSocial.Domain.Common;
using CineSocial.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Storage;

namespace CineSocial.Infrastructure.Repositories;

/// <summary>
/// Unit of Work implementation for managing transactions and repositories
/// </summary>
public class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _context;
    private readonly ICurrentUserService? _currentUserService;
    private readonly Dictionary<Type, object> _repositories;
    private IDbContextTransaction? _transaction;

    public UnitOfWork(ApplicationDbContext context, ICurrentUserService? currentUserService = null)
    {
        _context = context;
        _currentUserService = currentUserService;
        _repositories = new Dictionary<Type, object>();
    }

    /// <summary>
    /// Gets a repository for the specified entity type
    /// </summary>
    public IRepository<T> Repository<T>() where T : BaseEntity
    {
        var type = typeof(T);

        if (_repositories.ContainsKey(type))
        {
            return (IRepository<T>)_repositories[type];
        }

        var repositoryInstance = new Repository<T>(_context);
        _repositories.Add(type, repositoryInstance);

        return repositoryInstance;
    }

    /// <summary>
    /// Saves all changes to the database with audit tracking
    /// </summary>
    public async Task<Result<int>> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Apply audit information
            ApplyAuditInformation();

            var result = await _context.SaveChangesAsync(cancellationToken);
            return Result.Success(result);
        }
        catch (Exception ex)
        {
            return Result.Failure<int>(Error.Failure("Database.SaveFailed", ex.Message));
        }
    }

    /// <summary>
    /// Begins a database transaction
    /// </summary>
    public async Task<Result> BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (_transaction != null)
            {
                return Result.Failure(Error.Conflict("Transaction.AlreadyStarted", "A transaction is already in progress"));
            }

            _transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure(Error.Failure("Transaction.BeginFailed", ex.Message));
        }
    }

    /// <summary>
    /// Commits the current transaction
    /// </summary>
    public async Task<Result> CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (_transaction == null)
            {
                return Result.Failure(Error.Failure("Transaction.NotStarted", "No transaction is in progress"));
            }

            await _transaction.CommitAsync(cancellationToken);
            await _transaction.DisposeAsync();
            _transaction = null;

            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure(Error.Failure("Transaction.CommitFailed", ex.Message));
        }
    }

    /// <summary>
    /// Rolls back the current transaction
    /// </summary>
    public async Task<Result> RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (_transaction == null)
            {
                return Result.Failure(Error.Failure("Transaction.NotStarted", "No transaction is in progress"));
            }

            await _transaction.RollbackAsync(cancellationToken);
            await _transaction.DisposeAsync();
            _transaction = null;

            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure(Error.Failure("Transaction.RollbackFailed", ex.Message));
        }
    }

    /// <summary>
    /// Applies audit information to entities
    /// </summary>
    private void ApplyAuditInformation()
    {
        var entries = _context.ChangeTracker.Entries<BaseEntity>();
        var currentUserId = _currentUserService?.UserId;
        var now = DateTime.UtcNow;

        foreach (var entry in entries)
        {
            switch (entry.State)
            {
                case Microsoft.EntityFrameworkCore.EntityState.Added:
                    entry.Entity.CreatedAt = now;
                    entry.Entity.UpdatedAt = now;

                    if (entry.Entity is BaseAuditableEntity auditableEntity && currentUserId.HasValue)
                    {
                        auditableEntity.CreatedBy = currentUserId.Value;
                        auditableEntity.UpdatedBy = currentUserId.Value;
                    }
                    break;

                case Microsoft.EntityFrameworkCore.EntityState.Modified:
                    entry.Entity.UpdatedAt = now;

                    if (entry.Entity is BaseAuditableEntity modifiedAuditableEntity && currentUserId.HasValue)
                    {
                        modifiedAuditableEntity.UpdatedBy = currentUserId.Value;
                    }

                    // Handle soft delete
                    if (entry.Entity.IsDeleted && entry.Entity.DeletedAt == null)
                    {
                        entry.Entity.DeletedAt = now;

                        if (entry.Entity is BaseAuditableEntity deletedAuditableEntity && currentUserId.HasValue)
                        {
                            deletedAuditableEntity.DeletedBy = currentUserId.Value;
                        }
                    }
                    break;
            }
        }
    }

    /// <summary>
    /// Disposes the Unit of Work and releases resources
    /// </summary>
    public void Dispose()
    {
        _transaction?.Dispose();
        _repositories.Clear();
        GC.SuppressFinalize(this);
    }
}
