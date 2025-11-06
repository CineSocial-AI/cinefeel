namespace CineSocial.Application.Common.Results;

/// <summary>
/// Extension methods for Result types
/// </summary>
public static class ResultExtensions
{
    /// <summary>
    /// Converts a Result to an HTTP response representation
    /// </summary>
    public static object ToApiResponse(this Result result)
    {
        if (result.IsSuccess)
        {
            return new { success = true };
        }

        return new
        {
            success = false,
            error = new
            {
                code = result.Error.Code,
                description = result.Error.Description,
                type = result.Error.Type.ToString()
            }
        };
    }

    /// <summary>
    /// Converts a Result{T} to an HTTP response representation
    /// </summary>
    public static object ToApiResponse<TValue>(this Result<TValue> result)
    {
        if (result.IsSuccess)
        {
            return new
            {
                success = true,
                data = result.Value
            };
        }

        return new
        {
            success = false,
            error = new
            {
                code = result.Error.Code,
                description = result.Error.Description,
                type = result.Error.Type.ToString()
            }
        };
    }

    /// <summary>
    /// Converts a PagedResult{T} to an HTTP response representation
    /// </summary>
    public static object ToApiResponse<TValue>(this PagedResult<TValue> result)
    {
        if (result.IsSuccess)
        {
            return new
            {
                success = true,
                data = result.Value,
                pagination = new
                {
                    page = result.Page,
                    pageSize = result.PageSize,
                    totalCount = result.TotalCount,
                    totalPages = result.TotalPages,
                    hasPreviousPage = result.HasPreviousPage,
                    hasNextPage = result.HasNextPage
                }
            };
        }

        return new
        {
            success = false,
            error = new
            {
                code = result.Error.Code,
                description = result.Error.Description,
                type = result.Error.Type.ToString()
            }
        };
    }

    /// <summary>
    /// Ensures the result is successful, otherwise throws an exception
    /// </summary>
    public static TValue Ensure<TValue>(this Result<TValue> result)
    {
        if (result.IsFailure)
        {
            throw new InvalidOperationException($"Result failed: {result.Error.Description}");
        }

        return result.Value;
    }

    /// <summary>
    /// Executes an action on success
    /// </summary>
    public static Result OnSuccess(this Result result, Action action)
    {
        if (result.IsSuccess)
        {
            action();
        }
        return result;
    }

    /// <summary>
    /// Executes an action on failure
    /// </summary>
    public static Result OnFailure(this Result result, Action<Error> action)
    {
        if (result.IsFailure)
        {
            action(result.Error);
        }
        return result;
    }

    /// <summary>
    /// Combines multiple results into a single result
    /// </summary>
    public static Result Combine(params Result[] results)
    {
        foreach (var result in results)
        {
            if (result.IsFailure)
            {
                return result;
            }
        }
        return Result.Success();
    }

    /// <summary>
    /// Converts a Task{Result} to Result
    /// </summary>
    public static async Task<Result> ToResultAsync(this Task<Result> resultTask)
    {
        return await resultTask;
    }

    /// <summary>
    /// Converts a Task{Result{T}} to Result{T}
    /// </summary>
    public static async Task<Result<TValue>> ToResultAsync<TValue>(this Task<Result<TValue>> resultTask)
    {
        return await resultTask;
    }
}
