using CineSocial.Application.Features.Rates.Commands.DeleteRate;
using CineSocial.Application.Features.Rates.Commands.RateMovie;
using CineSocial.Application.Features.Rates.Queries.GetMovieRates;
using CineSocial.Application.Features.Rates.Queries.GetMovieRatesSummary;
using CineSocial.Application.Features.Rates.Queries.GetUserMovieRate;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace CineSocial.Api.Controllers;

[ApiController]
[Route("api/movies/{movieId:guid}/rates")]
public class RatesController : ControllerBase
{
    private readonly ISender _sender;

    public RatesController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>
    /// Rate a movie (0-10). Creates new rate or updates existing one.
    /// </summary>
    /// <param name="movieId">Movie ID (example: 5f3c76cd-b0bb-44f2-bfae-25a4b716a901)</param>
    /// <param name="request">Rating request body</param>
    [HttpPost]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<RateResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RateMovie(
        [FromRoute] Guid movieId = default,
        [FromBody] RateMovieRequestDto? request = null,
        CancellationToken cancellationToken = default)
    {
        var command = new RateMovieCommand(movieId, request.Rating);
        var result = await _sender.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            return StatusCode(result.Error.GetHttpStatusCode(), new ApiResponse<object>
            {
                Success = false,
                Message = result.Error.Description,
                Error = result.Error.Code
            });
        }

        var response = new RateResponseDto
        {
            RateId = result.Value.RateId,
            MovieId = result.Value.MovieId,
            UserId = result.Value.UserId,
            Rating = result.Value.Rating,
            IsNew = result.Value.IsNew,
            CreatedAt = result.Value.CreatedAt,
            UpdatedAt = result.Value.UpdatedAt
        };

        return Ok(new ApiResponse<RateResponseDto>
        {
            Success = true,
            Message = result.Value.IsNew ? "Movie rated successfully" : "Rating updated successfully",
            Data = response
        });
    }

    /// <summary>
    /// Delete your rate for a movie
    /// </summary>
    /// <param name="movieId">Movie ID (example: 5f3c76cd-b0bb-44f2-bfae-25a4b716a901)</param>
    [HttpDelete]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> DeleteRate(
        [FromRoute] Guid movieId = default,
        CancellationToken cancellationToken = default)
    {
        var command = new DeleteRateCommand(movieId);
        var result = await _sender.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            return StatusCode(result.Error.GetHttpStatusCode(), new ApiResponse<object>
            {
                Success = false,
                Message = result.Error.Description,
                Error = result.Error.Code
            });
        }

        return Ok(new ApiResponse<object>
        {
            Success = true,
            Message = "Rating deleted successfully"
        });
    }

    /// <summary>
    /// Get all rates for a movie (paginated)
    /// </summary>
    /// <param name="movieId">Movie ID (example: 5f3c76cd-b0bb-44f2-bfae-25a4b716a901)</param>
    [HttpGet]
    [ProducesResponseType(typeof(PagedApiResponse<List<RateDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMovieRates(
        [FromRoute] Guid movieId = default,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var query = new GetMovieRatesQuery(movieId, page, pageSize);
        var result = await _sender.Send(query, cancellationToken);

        if (result.IsFailure)
        {
            return StatusCode(result.Error.GetHttpStatusCode(), new ApiResponse<object>
            {
                Success = false,
                Message = result.Error.Description,
                Error = result.Error.Code
            });
        }

        return Ok(new PagedApiResponse<List<RateDto>>
        {
            Success = true,
            Message = "Rates retrieved successfully",
            Data = result.Value,
            Page = result.Page,
            PageSize = result.PageSize,
            TotalCount = result.TotalCount,
            TotalPages = result.TotalPages
        });
    }

    /// <summary>
    /// Get your own rate for a movie
    /// </summary>
    /// <param name="movieId">Movie ID (example: 5f3c76cd-b0bb-44f2-bfae-25a4b716a901)</param>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<RateDto?>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetUserMovieRate(
        [FromRoute] Guid movieId = default,
        CancellationToken cancellationToken = default)
    {
        var query = new GetUserMovieRateQuery(movieId);
        var result = await _sender.Send(query, cancellationToken);

        if (result.IsFailure)
        {
            return StatusCode(result.Error.GetHttpStatusCode(), new ApiResponse<object>
            {
                Success = false,
                Message = result.Error.Description,
                Error = result.Error.Code
            });
        }

        return Ok(new ApiResponse<RateDto?>
        {
            Success = true,
            Message = result.Value == null ? "No rating found" : "Rating retrieved successfully",
            Data = result.Value
        });
    }

    /// <summary>
    /// Get rating summary for a movie (average, total, distribution)
    /// </summary>
    /// <param name="movieId">Movie ID (example: 5f3c76cd-b0bb-44f2-bfae-25a4b716a901)</param>
    [HttpGet("summary")]
    [ProducesResponseType(typeof(ApiResponse<MovieRatesSummary>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMovieRatesSummary(
        [FromRoute] Guid movieId = default,
        CancellationToken cancellationToken = default)
    {
        var query = new GetMovieRatesSummaryQuery(movieId);
        var result = await _sender.Send(query, cancellationToken);

        if (result.IsFailure)
        {
            return StatusCode(result.Error.GetHttpStatusCode(), new ApiResponse<object>
            {
                Success = false,
                Message = result.Error.Description,
                Error = result.Error.Code
            });
        }

        return Ok(new ApiResponse<MovieRatesSummary>
        {
            Success = true,
            Message = "Rating summary retrieved successfully",
            Data = result.Value
        });
    }
}

// DTOs for API
public class RateMovieRequestDto
{
    /// <summary>
    /// Rating value (0-10, max 1 decimal place)
    /// </summary>
    [Required]
    [Range(0, 10)]
    public decimal Rating { get; set; } = 8.5m;
}

public class RateResponseDto
{
    public Guid RateId { get; set; }
    public Guid MovieId { get; set; }
    public Guid UserId { get; set; }
    public decimal Rating { get; set; }
    public bool IsNew { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class PagedApiResponse<T>
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public T? Data { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
    public string? Error { get; set; }
}
