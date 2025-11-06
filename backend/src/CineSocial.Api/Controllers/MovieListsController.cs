using CineSocial.Application.Features.MovieLists.Commands.AddMovieToList;
using CineSocial.Application.Features.MovieLists.Commands.CreateMovieList;
using CineSocial.Application.Features.MovieLists.Commands.DeleteMovieList;
using CineSocial.Application.Features.MovieLists.Commands.FavoriteMovieList;
using CineSocial.Application.Features.MovieLists.Commands.RemoveMovieFromList;
using CineSocial.Application.Features.MovieLists.Commands.UnfavoriteMovieList;
using CineSocial.Application.Features.MovieLists.Commands.UpdateMovieList;
using CineSocial.Application.Features.MovieLists.Queries.GetMovieListDetail;
using CineSocial.Application.Features.MovieLists.Queries.GetMyMovieLists;
using CineSocial.Application.Features.MovieLists.Queries.GetPublicMovieLists;
using CineSocial.Application.Features.MovieLists.Queries.GetUserMovieLists;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace CineSocial.Api.Controllers;

[ApiController]
[Route("api/movie-lists")]
public class MovieListsController : ControllerBase
{
    private readonly ISender _sender;

    public MovieListsController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>
    /// Get user's movie lists (public only for others, all for own profile)
    /// </summary>
    [HttpGet("user/{userId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<List<MovieListSummaryDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetUserMovieLists(Guid userId, CancellationToken cancellationToken = default)
    {
        var query = new GetUserMovieListsQuery(userId);
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

        return Ok(new ApiResponse<List<MovieListSummaryDto>>
        {
            Success = true,
            Message = "User movie lists retrieved successfully",
            Data = result.Value
        });
    }

    /// <summary>
    /// Get my movie lists
    /// </summary>
    [HttpGet("my-lists")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<List<MovieListSummaryDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMyMovieLists(CancellationToken cancellationToken = default)
    {
        var query = new GetMyMovieListsQuery();
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

        return Ok(new ApiResponse<List<MovieListSummaryDto>>
        {
            Success = true,
            Message = "Movie lists retrieved successfully",
            Data = result.Value
        });
    }

    /// <summary>
    /// Get public movie lists
    /// </summary>
    [HttpGet("public")]
    [ProducesResponseType(typeof(PagedApiResponse<List<PublicMovieListDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPublicMovieLists(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var query = new GetPublicMovieListsQuery(page, pageSize);
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

        return Ok(new PagedApiResponse<List<PublicMovieListDto>>
        {
            Success = true,
            Message = "Public movie lists retrieved successfully",
            Data = result.Value,
            Page = result.Page,
            PageSize = result.PageSize,
            TotalCount = result.TotalCount,
            TotalPages = result.TotalPages
        });
    }

    /// <summary>
    /// Get movie list detail
    /// </summary>
    [HttpGet("{listId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<MovieListDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMovieListDetail(
        Guid listId,
        CancellationToken cancellationToken = default)
    {
        var query = new GetMovieListDetailQuery(listId);
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

        return Ok(new ApiResponse<MovieListDetailDto>
        {
            Success = true,
            Message = "Movie list detail retrieved successfully",
            Data = result.Value
        });
    }

    /// <summary>
    /// Create a new movie list
    /// </summary>
    [HttpPost]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<MovieListResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CreateMovieList(
        [FromBody] CreateMovieListRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var command = new CreateMovieListCommand(request.Name, request.Description, request.IsPublic);
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

        return Ok(new ApiResponse<MovieListResponse>
        {
            Success = true,
            Message = "Movie list created successfully",
            Data = result.Value
        });
    }

    /// <summary>
    /// Update a movie list
    /// </summary>
    [HttpPut("{listId:guid}")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UpdateMovieList(
        Guid listId,
        [FromBody] UpdateMovieListRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var command = new UpdateMovieListCommand(listId, request.Name, request.Description, request.IsPublic);
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
            Message = "Movie list updated successfully"
        });
    }

    /// <summary>
    /// Delete a movie list
    /// </summary>
    [HttpDelete("{listId:guid}")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> DeleteMovieList(
        Guid listId,
        CancellationToken cancellationToken = default)
    {
        var command = new DeleteMovieListCommand(listId);
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
            Message = "Movie list deleted successfully"
        });
    }

    /// <summary>
    /// Add a movie to a list
    /// </summary>
    [HttpPost("{listId:guid}/movies/{movieId:guid}")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> AddMovieToList(
        Guid listId,
        Guid movieId,
        CancellationToken cancellationToken = default)
    {
        var command = new AddMovieToListCommand(listId, movieId);
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
            Message = "Movie added to list successfully"
        });
    }

    /// <summary>
    /// Remove a movie from a list
    /// </summary>
    [HttpDelete("{listId:guid}/movies/{movieId:guid}")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RemoveMovieFromList(
        Guid listId,
        Guid movieId,
        CancellationToken cancellationToken = default)
    {
        var command = new RemoveMovieFromListCommand(listId, movieId);
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
            Message = "Movie removed from list successfully"
        });
    }

    /// <summary>
    /// Favorite a movie list
    /// </summary>
    [HttpPost("{listId:guid}/favorite")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> FavoriteMovieList(
        Guid listId,
        CancellationToken cancellationToken = default)
    {
        var command = new FavoriteMovieListCommand(listId);
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
            Message = "Movie list favorited successfully"
        });
    }

    /// <summary>
    /// Unfavorite a movie list
    /// </summary>
    [HttpDelete("{listId:guid}/favorite")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UnfavoriteMovieList(
        Guid listId,
        CancellationToken cancellationToken = default)
    {
        var command = new UnfavoriteMovieListCommand(listId);
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
            Message = "Movie list unfavorited successfully"
        });
    }
}

// DTOs for API
public class CreateMovieListRequestDto
{
    [Required]
    public string Name { get; set; } = "My Favorite Movies";
    public string? Description { get; set; }
    public bool IsPublic { get; set; } = true;
}

public class UpdateMovieListRequestDto
{
    [Required]
    public string Name { get; set; } = "Updated List Name";
    public string? Description { get; set; }
    public bool IsPublic { get; set; } = true;
}
