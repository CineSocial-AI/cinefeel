using CineSocial.Application.Common.Results;
using CineSocial.Application.Features.Movies.Queries.GetMovies;
using CineSocial.Application.Features.Search.Queries.SearchAll;
using CineSocial.Application.Features.Search.Queries.SearchPeople;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CineSocial.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SearchController : ControllerBase
{
    private readonly ISender _sender;

    public SearchController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>
    /// Search for movies only
    /// </summary>
    [HttpGet("movies")]
    [ProducesResponseType(typeof(PagedApiResponse<List<MovieDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> SearchMovies(
        [FromQuery] string query,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var searchQuery = new GetMoviesQuery(page, pageSize, query);
        var result = await _sender.Send(searchQuery, cancellationToken);

        if (result.IsFailure)
        {
            return StatusCode(result.Error.GetHttpStatusCode(), result.ToApiResponse());
        }

        return Ok(result.ToApiResponse());
    }

    /// <summary>
    /// Search for people (cast/crew) only
    /// </summary>
    [HttpGet("people")]
    [ProducesResponseType(typeof(PagedApiResponse<List<PersonSearchResultDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> SearchPeople(
        [FromQuery] string query,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var searchQuery = new SearchPeopleQuery(query, page, pageSize);
        var result = await _sender.Send(searchQuery, cancellationToken);

        if (result.IsFailure)
        {
            return StatusCode(result.Error.GetHttpStatusCode(), result.ToApiResponse());
        }

        return Ok(result.ToApiResponse());
    }

    /// <summary>
    /// Search for both movies and people
    /// </summary>
    [HttpGet("all")]
    [ProducesResponseType(typeof(ApiResponse<SearchAllResultDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> SearchAll(
        [FromQuery] string query,
        CancellationToken cancellationToken = default)
    {
        var searchQuery = new SearchAllQuery(query);
        var result = await _sender.Send(searchQuery, cancellationToken);

        if (result.IsFailure)
        {
            return StatusCode(result.Error.GetHttpStatusCode(), result.ToApiResponse());
        }

        return Ok(result.ToApiResponse());
    }
}
