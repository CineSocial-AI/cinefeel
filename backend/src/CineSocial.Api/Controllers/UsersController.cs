using CineSocial.Application.Features.Users.Commands.BlockUser;
using CineSocial.Application.Features.Users.Commands.FollowUser;
using CineSocial.Application.Features.Users.Commands.UnblockUser;
using CineSocial.Application.Features.Users.Commands.UnfollowUser;
using CineSocial.Application.Features.Users.Commands.UploadProfileImage;
using CineSocial.Application.Features.Users.Commands.UploadBackgroundImage;
using CineSocial.Application.Features.Users.Queries.GetUserProfile;
using CineSocial.Application.Features.Users.Queries.GetUsers;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CineSocial.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly ISender _sender;

    public UsersController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>
    /// Get all users with pagination and search
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedApiResponse<List<UserDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUsers(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? searchTerm = null,
        CancellationToken cancellationToken = default)
    {
        var query = new GetUsersQuery(page, pageSize, searchTerm);
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

        return Ok(new PagedApiResponse<List<UserDto>>
        {
            Success = true,
            Message = "Users retrieved successfully",
            Data = result.Value,
            Page = result.Page,
            PageSize = result.PageSize,
            TotalCount = result.TotalCount,
            TotalPages = result.TotalPages
        });
    }

    /// <summary>
    /// Get user profile by ID
    /// </summary>
    /// <param name="userId">User ID (example: 00000000-0000-0000-0000-000000000001)</param>
    [HttpGet("{userId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<UserProfileDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetUserProfile(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var query = new GetUserProfileQuery(userId);
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

        return Ok(new ApiResponse<UserProfileDto>
        {
            Success = true,
            Message = "User profile retrieved successfully",
            Data = result.Value
        });
    }

    /// <summary>
    /// Follow a user
    /// </summary>
    /// <param name="userId">User ID to follow</param>
    [HttpPost("{userId:guid}/follow")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<FollowResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> FollowUser(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var command = new FollowUserCommand(userId);
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

        return Ok(new ApiResponse<FollowResponse>
        {
            Success = true,
            Message = result.Value.Message,
            Data = result.Value
        });
    }

    /// <summary>
    /// Unfollow a user
    /// </summary>
    /// <param name="userId">User ID to unfollow</param>
    [HttpDelete("{userId:guid}/follow")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UnfollowUser(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var command = new UnfollowUserCommand(userId);
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
            Message = "Successfully unfollowed user"
        });
    }

    /// <summary>
    /// Block a user
    /// </summary>
    /// <param name="userId">User ID to block</param>
    [HttpPost("{userId:guid}/block")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<BlockResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> BlockUser(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var command = new BlockUserCommand(userId);
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

        return Ok(new ApiResponse<BlockResponse>
        {
            Success = true,
            Message = result.Value.Message,
            Data = result.Value
        });
    }

    /// <summary>
    /// Unblock a user
    /// </summary>
    /// <param name="userId">User ID to unblock</param>
    [HttpDelete("{userId:guid}/block")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UnblockUser(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var command = new UnblockUserCommand(userId);
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
            Message = "Successfully unblocked user"
        });
    }

    /// <summary>
    /// Upload profile image
    /// </summary>
    [HttpPost("profile-image")]
    [Authorize]
    [Consumes("multipart/form-data")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public async Task<IActionResult> UploadProfileImage(
        [FromForm] IFormFile file,
        CancellationToken cancellationToken = default)
    {
        var command = new UploadProfileImageCommand(file);
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

        return Ok(new ApiResponse<UploadProfileImageResponse>
        {
            Success = true,
            Message = result.Value.Message,
            Data = result.Value
        });
    }

    /// <summary>
    /// Upload background image
    /// </summary>
    [HttpPost("background-image")]
    [Authorize]
    [Consumes("multipart/form-data")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public async Task<IActionResult> UploadBackgroundImage(
        [FromForm] IFormFile file,
        CancellationToken cancellationToken = default)
    {
        var command = new UploadBackgroundImageCommand(file);
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

        return Ok(new ApiResponse<UploadBackgroundImageResponse>
        {
            Success = true,
            Message = result.Value.Message,
            Data = result.Value
        });
    }
}
