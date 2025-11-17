using CineSocial.Application.Common.Results;
using CineSocial.Application.Features.Images.Queries.GetImage;
using CineSocial.Domain.Enums;
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
    /// <returns>Image file with appropriate content type or redirect to cloud URL</returns>
    [HttpGet("{imageId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status302Found)]
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

        // If image is stored in cloud (Cloudinary, R2, etc.), redirect to cloud URL
        if (image.Provider != StorageProvider.Database && !string.IsNullOrWhiteSpace(image.CloudUrl))
        {
            return Redirect(image.CloudUrl);
        }

        // Legacy: Return image from database
        if (image.Data == null || image.Data.Length == 0)
        {
            return NotFound(new ApiResponse<object>
            {
                Success = false,
                Message = "Image data not found"
            });
        }

        return File(image.Data, image.ContentType, image.FileName);
    }
}
