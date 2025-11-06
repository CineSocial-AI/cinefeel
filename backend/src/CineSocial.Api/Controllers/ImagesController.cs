using CineSocial.Application.Common.Results;
using CineSocial.Application.Features.Images.Queries.GetImage;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CineSocial.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ImagesController : ControllerBase
{
    private readonly ISender _sender;

    public ImagesController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>
    /// Get an image by its ID
    /// </summary>
    /// <param name="imageId">The unique identifier of the image</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Image file with appropriate content type</returns>
    [HttpGet("{imageId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetImage(Guid imageId, CancellationToken cancellationToken = default)
    {
        var query = new GetImageQuery(imageId);
        var result = await _sender.Send(query, cancellationToken);

        if (result.IsFailure)
        {
            return StatusCode(result.Error.GetHttpStatusCode(), result.ToApiResponse());
        }

        var image = result.Value;

        // Return the image file with appropriate content type
        return File(image.Data, image.ContentType, image.FileName);
    }
}
