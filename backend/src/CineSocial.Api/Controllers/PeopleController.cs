using CineSocial.Application.Common.Results;
using CineSocial.Application.Features.People.Queries.GetPersonDetail;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CineSocial.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PeopleController : ControllerBase
{
    private readonly ISender _sender;

    public PeopleController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>
    /// Get person details including filmography
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<PersonDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPersonDetail(Guid id, CancellationToken cancellationToken = default)
    {
        var query = new GetPersonDetailQuery(id);
        var result = await _sender.Send(query, cancellationToken);

        if (result.IsFailure)
        {
            return StatusCode(result.Error.GetHttpStatusCode(), result.ToApiResponse());
        }

        return Ok(result.ToApiResponse());
    }
}
