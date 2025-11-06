using System.Linq.Expressions;
using CineSocial.Application.Common.Results;
using CineSocial.Domain.Common;

namespace CineSocial.Application.Common.Interfaces;

/// <summary>
/// Generic repository interface for data access operations
/// </summary>
public interface IRepository<T> where T : BaseEntity
{
    // Query
    IQueryable<T> Query();
    IQueryable<T> QueryNoTracking();

    // Get by Id
    Task<Result<T>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Result<T>> GetByIdAsync(Guid id, params Expression<Func<T, object>>[] includes);

    // Get single
    Task<Result<T>> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);
    Task<Result<T>> SingleOrDefaultAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);

    // Get multiple
    Task<Result<List<T>>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Result<List<T>>> FindAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);

    // Pagination
    Task<PagedResult<List<T>>> GetPagedAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<PagedResult<List<T>>> GetPagedAsync(
        Expression<Func<T, bool>> predicate,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    // Check existence
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> AnyAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);

    // Count
    Task<int> CountAsync(CancellationToken cancellationToken = default);
    Task<int> CountAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);

    // Add
    Task<Result<T>> AddAsync(T entity, CancellationToken cancellationToken = default);
    Task<Result<List<T>>> AddRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default);

    // Update
    Result<T> Update(T entity);
    Result<List<T>> UpdateRange(IEnumerable<T> entities);

    // Delete (Soft delete)
    Result Delete(T entity);
    Result DeleteRange(IEnumerable<T> entities);

    // Hard delete
    Result HardDelete(T entity);
    Result HardDeleteRange(IEnumerable<T> entities);
}
