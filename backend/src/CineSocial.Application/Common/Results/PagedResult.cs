namespace CineSocial.Application.Common.Results;

/// <summary>
/// Represents a paginated result
/// </summary>
public class PagedResult<TValue> : Result<TValue>
{
    protected internal PagedResult(
        TValue? value,
        bool isSuccess,
        Error error,
        int page,
        int pageSize,
        int totalCount)
        : base(value, isSuccess, error)
    {
        Page = page;
        PageSize = pageSize;
        TotalCount = totalCount;
    }

    public int Page { get; }
    public int PageSize { get; }
    public int TotalCount { get; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasPreviousPage => Page > 1;
    public bool HasNextPage => Page < TotalPages;

    public static PagedResult<TValue> Success(
        TValue value,
        int page,
        int pageSize,
        int totalCount)
    {
        return new PagedResult<TValue>(value, true, Error.None, page, pageSize, totalCount);
    }

    public static new PagedResult<TValue> Failure(Error error)
    {
        return new PagedResult<TValue>(default, false, error, 0, 0, 0);
    }

    public static PagedResult<TValue> Create(
        TValue value,
        int page,
        int pageSize,
        int totalCount)
    {
        if (page < 1)
        {
            return Failure(Error.Validation("Pagination.InvalidPage", "Page must be greater than 0"));
        }

        if (pageSize < 1)
        {
            return Failure(Error.Validation("Pagination.InvalidPageSize", "PageSize must be greater than 0"));
        }

        return Success(value, page, pageSize, totalCount);
    }

    // Map for transforming paged results
    public new PagedResult<TResult> Map<TResult>(Func<TValue, TResult> mapper)
    {
        return IsSuccess
            ? new PagedResult<TResult>(mapper(Value), true, Error.None, Page, PageSize, TotalCount)
            : new PagedResult<TResult>(default, false, Error, Page, PageSize, TotalCount);
    }
}
