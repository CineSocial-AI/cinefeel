using CineSocial.Application.Common.Interfaces;
using CineSocial.Application.Common.Results;
using CineSocial.Domain.Common;
using CineSocial.Domain.Entities.Movie;
using CineSocial.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;

namespace CineSocial.Infrastructure.Repositories;

/// <summary>
/// Unit of Work implementation for managing transactions and repositories
/// </summary>
public class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _context;
    private readonly ICurrentUserService? _currentUserService;
    private readonly ICacheService? _cacheService;
    private readonly ILogger<UnitOfWork>? _logger;
    private readonly Dictionary<Type, object> _repositories;
    private IDbContextTransaction? _transaction;

    public UnitOfWork(
        ApplicationDbContext context,
        ICurrentUserService? currentUserService = null,
        ICacheService? cacheService = null,
        ILogger<UnitOfWork>? logger = null)
    {
        _context = context;
        _currentUserService = currentUserService;
        _cacheService = cacheService;
        _logger = logger;
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
    /// Saves all changes to the database with audit tracking and cache invalidation
    /// </summary>
    public async Task<Result<int>> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Track modified entities before save for cache invalidation
            var modifiedEntities = _context.ChangeTracker.Entries()
                .Where(e => e.State == EntityState.Added ||
                           e.State == EntityState.Modified ||
                           e.State == EntityState.Deleted)
                .Select(e => e.Entity.GetType())
                .Distinct()
                .ToList();

            // Apply audit information
            ApplyAuditInformation();

            var result = await _context.SaveChangesAsync(cancellationToken);

            // Invalidate cache after successful save
            if (_cacheService != null && result > 0)
            {
                await InvalidateCacheForModifiedEntities(modifiedEntities, cancellationToken);
            }

            return Result.Success(result);
        }
        catch (Exception ex)
        {
            return Result.Failure<int>(Error.Failure("Database.SaveFailed", ex.Message));
        }
    }

    /// <summary>
    /// Invalidates cache for modified entity types
    /// </summary>
    private async Task InvalidateCacheForModifiedEntities(List<Type> modifiedEntityTypes, CancellationToken cancellationToken)
    {
        try
        {
            foreach (var entityType in modifiedEntityTypes)
            {
                // Invalidate search cache if movies or people are modified
                if (entityType == typeof(MovieEntity) || entityType.Name.Contains("Movie"))
                {
                    _logger?.LogDebug("Invalidating movie-related cache due to {EntityType} modification", entityType.Name);
                    await _cacheService!.RemoveByPrefixAsync("query:searchall", cancellationToken);
                    await _cacheService!.RemoveByPrefixAsync("query:getmoviedetail", cancellationToken);
                }

                if (entityType == typeof(Person))
                {
                    _logger?.LogDebug("Invalidating person-related cache due to Person modification");
                    await _cacheService!.RemoveByPrefixAsync("query:searchpeople", cancellationToken);
                    await _cacheService!.RemoveByPrefixAsync("query:searchall", cancellationToken);
                }

                // Invalidate comment/rate cache
                if (entityType.Name.Contains("Comment") || entityType.Name.Contains("Rate"))
                {
                    _logger?.LogDebug("Invalidating social cache due to {EntityType} modification", entityType.Name);
                    // These are handled by their specific commands
                }
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error invalidating cache for modified entities");
            // Don't throw - cache invalidation failure shouldn't break the save operation
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
