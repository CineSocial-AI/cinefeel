using CineSocial.Application.Common.Results;
using CineSocial.Application.Features.Movies.Commands.FavoriteMovie;
using CineSocial.Application.Features.Movies.Commands.UnfavoriteMovie;
using CineSocial.Application.Features.Movies.Queries.GetFavoriteMovies;
using CineSocial.Application.Features.Movies.Queries.GetFilteredMovies;
using CineSocial.Application.Features.Movies.Queries.GetGenres;
using CineSocial.Application.Features.Movies.Queries.GetMovieDetail;
using CineSocial.Application.Features.Movies.Queries.GetMovies;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CineSocial.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MoviesController : ControllerBase
{
    private readonly ISender _sender;

    public MoviesController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    public async Task<IActionResult> GetMovies(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] int? genreId = null,
        [FromQuery] int? year = null,
        [FromQuery] MovieSortBy sortBy = MovieSortBy.Popularity,
        CancellationToken cancellationToken = default)
    {
        var query = new GetMoviesQuery(page, pageSize, search, genreId, year, sortBy);
        var result = await _sender.Send(query, cancellationToken);

        if (result.IsFailure)
        {
            return StatusCode(result.Error.GetHttpStatusCode(), result.ToApiResponse());
        }

        return Ok(result.ToApiResponse());
    }

    /// <summary>
    /// Browse movies with advanced filtering
    /// </summary>
    [HttpGet("browse")]
    [ProducesResponseType(typeof(PagedApiResponse<List<MovieDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> BrowseMovies(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] int? genreId = null,
        [FromQuery] int? year = null,
        [FromQuery] string? decade = null,
        [FromQuery] int? yearFrom = null,
        [FromQuery] int? yearTo = null,
        [FromQuery] double? minRating = null,
        [FromQuery] double? maxRating = null,
        [FromQuery] string? language = null,
        [FromQuery] MovieSortBy sortBy = MovieSortBy.Popularity,
        CancellationToken cancellationToken = default)
    {
        var query = new GetFilteredMoviesQuery(page, pageSize, search, genreId, year, decade, yearFrom, yearTo, minRating, maxRating, language, sortBy);
        var result = await _sender.Send(query, cancellationToken);

        if (result.IsFailure)
        {
            return StatusCode(result.Error.GetHttpStatusCode(), result.ToApiResponse());
        }

        return Ok(result.ToApiResponse());
    }

    /// <summary>
    /// Get all genres
    /// </summary>
    [HttpGet("genres")]
    [ProducesResponseType(typeof(ApiResponse<List<CineSocial.Application.Features.Movies.Queries.GetGenres.GenreDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetGenres(CancellationToken cancellationToken = default)
    {
        var query = new GetGenresQuery();
        var result = await _sender.Send(query, cancellationToken);

        if (result.IsFailure)
        {
            return StatusCode(result.Error.GetHttpStatusCode(), result.ToApiResponse());
        }

        return Ok(result.ToApiResponse());
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetMovieDetail(Guid id, CancellationToken cancellationToken = default)
    {
        var query = new GetMovieDetailQuery(id);
        var result = await _sender.Send(query, cancellationToken);

        if (result.IsFailure)
        {
            return StatusCode(result.Error.GetHttpStatusCode(), result.ToApiResponse());
        }

        return Ok(result.ToApiResponse());
    }

    /// <summary>
    /// Get favorite movies
    /// </summary>
    [HttpGet("favorites")]
    [Authorize]
    [ProducesResponseType(typeof(PagedApiResponse<List<FavoriteMovieDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetFavoriteMovies(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var query = new GetFavoriteMoviesQuery(page, pageSize);
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

        return Ok(new PagedApiResponse<List<FavoriteMovieDto>>
        {
            Success = true,
            Message = "Favorite movies retrieved successfully",
            Data = result.Value,
            Page = result.Page,
            PageSize = result.PageSize,
            TotalCount = result.TotalCount,
            TotalPages = result.TotalPages
        });
    }

    /// <summary>
    /// Add a movie to favorites
    /// </summary>
    [HttpPost("{movieId:guid}/favorite")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> FavoriteMovie(
        Guid movieId,
        CancellationToken cancellationToken = default)
    {
        var command = new FavoriteMovieCommand(movieId);
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
            Message = "Movie added to favorites successfully"
        });
    }

    /// <summary>
    /// Remove a movie from favorites
    /// </summary>
    [HttpDelete("{movieId:guid}/favorite")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UnfavoriteMovie(
        Guid movieId,
        CancellationToken cancellationToken = default)
    {
        var command = new UnfavoriteMovieCommand(movieId);
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
            Message = "Movie removed from favorites successfully"
        });
    }
}
