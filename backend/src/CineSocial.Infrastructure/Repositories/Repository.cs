using System.Linq.Expressions;
using CineSocial.Application.Common.Interfaces;
using CineSocial.Application.Common.Results;
using CineSocial.Domain.Common;
using CineSocial.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CineSocial.Infrastructure.Repositories;

public class Repository<T> : IRepository<T> where T : BaseEntity
{
    protected readonly ApplicationDbContext _context;
    protected readonly DbSet<T> _dbSet;

    public Repository(ApplicationDbContext context)
    {
        _context = context;
        _dbSet = context.Set<T>();
    }

    // Query
    public IQueryable<T> Query() => _dbSet.Where(e => !e.IsDeleted);

    public IQueryable<T> QueryNoTracking() => _dbSet.AsNoTracking().Where(e => !e.IsDeleted);

    // Get by Id
    public async Task<Result<T>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _dbSet
            .FirstOrDefaultAsync(e => e.Id == id && !e.IsDeleted, cancellationToken);

        return entity is not null
            ? Result.Success(entity)
            : Result.Failure<T>(Error.NotFound($"{typeof(T).Name}.NotFound", $"{typeof(T).Name} with ID {id} not found"));
    }

    public async Task<Result<T>> GetByIdAsync(Guid id, params Expression<Func<T, object>>[] includes)
    {
        IQueryable<T> query = _dbSet;

        foreach (var include in includes)
        {
            query = query.Include(include);
        }

        var entity = await query.FirstOrDefaultAsync(e => e.Id == id && !e.IsDeleted);

        return entity is not null
            ? Result.Success(entity)
            : Result.Failure<T>(Error.NotFound($"{typeof(T).Name}.NotFound", $"{typeof(T).Name} with ID {id} not found"));
    }

    // Get single
    public async Task<Result<T>> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
    {
        var entity = await _dbSet
            .Where(e => !e.IsDeleted)
            .FirstOrDefaultAsync(predicate, cancellationToken);

        return entity is not null
            ? Result.Success(entity)
            : Result.Failure<T>(Error.NotFound($"{typeof(T).Name}.NotFound", $"{typeof(T).Name} not found"));
    }

    public async Task<Result<T>> SingleOrDefaultAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
    {
        try
        {
            var entity = await _dbSet
                .Where(e => !e.IsDeleted)
                .SingleOrDefaultAsync(predicate, cancellationToken);

            return entity is not null
                ? Result.Success(entity)
                : Result.Failure<T>(Error.NotFound($"{typeof(T).Name}.NotFound", $"{typeof(T).Name} not found"));
        }
        catch (InvalidOperationException)
        {
            return Result.Failure<T>(Error.Conflict($"{typeof(T).Name}.MultipleFound", "Multiple entities found"));
        }
    }

    // Get multiple
    public async Task<Result<List<T>>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var entities = await _dbSet
            .Where(e => !e.IsDeleted)
            .ToListAsync(cancellationToken);

        return Result.Success(entities);
    }

    public async Task<Result<List<T>>> FindAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
    {
        var entities = await _dbSet
            .Where(e => !e.IsDeleted)
            .Where(predicate)
            .ToListAsync(cancellationToken);

        return Result.Success(entities);
    }

    // Pagination
    public async Task<PagedResult<List<T>>> GetPagedAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var totalCount = await _dbSet.CountAsync(e => !e.IsDeleted, cancellationToken);

        var entities = await _dbSet
            .Where(e => !e.IsDeleted)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return PagedResult<List<T>>.Create(entities, page, pageSize, totalCount);
    }

    public async Task<PagedResult<List<T>>> GetPagedAsync(
        Expression<Func<T, bool>> predicate,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _dbSet.Where(e => !e.IsDeleted).Where(predicate);

        var totalCount = await query.CountAsync(cancellationToken);

        var entities = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return PagedResult<List<T>>.Create(entities, page, pageSize, totalCount);
    }

    // Check existence
    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbSet.AnyAsync(e => e.Id == id && !e.IsDeleted, cancellationToken);
    }

    public async Task<bool> AnyAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
    {
        return await _dbSet.AnyAsync(e => !e.IsDeleted && predicate.Compile()(e), cancellationToken);
    }

    // Count
    public async Task<int> CountAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet.CountAsync(e => !e.IsDeleted, cancellationToken);
    }

    public async Task<int> CountAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
    {
        return await _dbSet.CountAsync(e => !e.IsDeleted && predicate.Compile()(e), cancellationToken);
    }

    // Add
    public async Task<Result<T>> AddAsync(T entity, CancellationToken cancellationToken = default)
    {
        try
        {
            await _dbSet.AddAsync(entity, cancellationToken);
            return Result.Success(entity);
        }
        catch (Exception ex)
        {
            return Result.Failure<T>(Error.Failure($"{typeof(T).Name}.AddFailed", ex.Message));
        }
    }

    public async Task<Result<List<T>>> AddRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default)
    {
        try
        {
            var entityList = entities.ToList();
            await _dbSet.AddRangeAsync(entityList, cancellationToken);
            return Result.Success(entityList);
        }
        catch (Exception ex)
        {
            return Result.Failure<List<T>>(Error.Failure($"{typeof(T).Name}.AddRangeFailed", ex.Message));
        }
    }

    // Update
    public Result<T> Update(T entity)
    {
        try
        {
            _dbSet.Update(entity);
            return Result.Success(entity);
        }
        catch (Exception ex)
        {
            return Result.Failure<T>(Error.Failure($"{typeof(T).Name}.UpdateFailed", ex.Message));
        }
    }

    public Result<List<T>> UpdateRange(IEnumerable<T> entities)
    {
        try
        {
            var entityList = entities.ToList();
            _dbSet.UpdateRange(entityList);
            return Result.Success(entityList);
        }
        catch (Exception ex)
        {
            return Result.Failure<List<T>>(Error.Failure($"{typeof(T).Name}.UpdateRangeFailed", ex.Message));
        }
    }

    // Delete (Soft delete)
    public Result Delete(T entity)
    {
        try
        {
            entity.IsDeleted = true;
            entity.DeletedAt = DateTime.UtcNow;
            _dbSet.Update(entity);
            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure(Error.Failure($"{typeof(T).Name}.DeleteFailed", ex.Message));
        }
    }

    public Result DeleteRange(IEnumerable<T> entities)
    {
        try
        {
            var now = DateTime.UtcNow;
            foreach (var entity in entities)
            {
                entity.IsDeleted = true;
                entity.DeletedAt = now;
            }
            _dbSet.UpdateRange(entities);
            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure(Error.Failure($"{typeof(T).Name}.DeleteRangeFailed", ex.Message));
        }
    }

    // Hard delete
    public Result HardDelete(T entity)
    {
        try
        {
            _dbSet.Remove(entity);
            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure(Error.Failure($"{typeof(T).Name}.HardDeleteFailed", ex.Message));
        }
    }

    public Result HardDeleteRange(IEnumerable<T> entities)
    {
        try
        {
            _dbSet.RemoveRange(entities);
            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure(Error.Failure($"{typeof(T).Name}.HardDeleteRangeFailed", ex.Message));
        }
    }
}
