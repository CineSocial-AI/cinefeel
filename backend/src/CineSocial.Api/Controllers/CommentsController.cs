using CineSocial.Application.Features.Comments.Commands.AddComment;
using CineSocial.Application.Features.Comments.Commands.AddReaction;
using CineSocial.Application.Features.Comments.Commands.DeleteComment;
using CineSocial.Application.Features.Comments.Commands.RemoveReaction;
using CineSocial.Application.Features.Comments.Commands.UpdateComment;
using CineSocial.Application.Features.Comments.Queries.GetMovieComments;
using CineSocial.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace CineSocial.Api.Controllers;

[ApiController]
[Route("api/movies/{movieId:guid}/comments")]
public class CommentsController : ControllerBase
{
    private readonly ISender _sender;

    public CommentsController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>
    /// Add a comment to a movie
    /// </summary>
    /// <param name="movieId">Movie ID (example: 5f3c76cd-b0bb-44f2-bfae-25a4b716a901)</param>
    /// <param name="request">Comment request body</param>
    [HttpPost]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<CommentResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> AddComment(
        [FromRoute] Guid movieId = default,
        [FromBody] AddCommentRequestDto? request = null,
        CancellationToken cancellationToken = default)
    {
        var command = new AddCommentCommand(movieId, request!.Content, request.ParentCommentId);
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

        return Ok(new ApiResponse<CommentResponse>
        {
            Success = true,
            Message = "Comment added successfully",
            Data = result.Value
        });
    }

    /// <summary>
    /// Get all comments for a movie (with nested replies)
    /// </summary>
    /// <param name="movieId">Movie ID (example: 5f3c76cd-b0bb-44f2-bfae-25a4b716a901)</param>
    [HttpGet]
    [ProducesResponseType(typeof(PagedApiResponse<List<CommentWithRepliesDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMovieComments(
        [FromRoute] Guid movieId = default,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] CommentSortBy sortBy = CommentSortBy.Newest,
        CancellationToken cancellationToken = default)
    {
        var query = new GetMovieCommentsQuery(movieId, page, pageSize, sortBy);
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

        return Ok(new PagedApiResponse<List<CommentWithRepliesDto>>
        {
            Success = true,
            Message = "Comments retrieved successfully",
            Data = result.Value,
            Page = result.Page,
            PageSize = result.PageSize,
            TotalCount = result.TotalCount,
            TotalPages = result.TotalPages
        });
    }

    /// <summary>
    /// Update your own comment
    /// </summary>
    [HttpPut("{commentId:guid}")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UpdateComment(
        Guid movieId,
        Guid commentId,
        [FromBody] UpdateCommentRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var command = new UpdateCommentCommand(commentId, request.Content);
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
            Message = "Comment updated successfully"
        });
    }

    /// <summary>
    /// Delete your own comment
    /// </summary>
    [HttpDelete("{commentId:guid}")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> DeleteComment(
        Guid movieId,
        Guid commentId,
        CancellationToken cancellationToken = default)
    {
        var command = new DeleteCommentCommand(commentId);
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
            Message = "Comment deleted successfully"
        });
    }

    /// <summary>
    /// Add a reaction (upvote/downvote) to a comment
    /// </summary>
    [HttpPost("{commentId:guid}/reactions")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<ReactionResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> AddReaction(
        Guid movieId,
        Guid commentId,
        [FromBody] AddReactionRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var command = new AddReactionCommand(commentId, request.ReactionType);
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

        return Ok(new ApiResponse<ReactionResponse>
        {
            Success = true,
            Message = result.Value.IsNew ? "Reaction added successfully" : "Reaction updated successfully",
            Data = result.Value
        });
    }

    /// <summary>
    /// Remove your reaction from a comment
    /// </summary>
    [HttpDelete("{commentId:guid}/reactions")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RemoveReaction(
        Guid movieId,
        Guid commentId,
        CancellationToken cancellationToken = default)
    {
        var command = new RemoveReactionCommand(commentId);
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
            Message = "Reaction removed successfully"
        });
    }
}

// DTOs for API
public class AddCommentRequestDto
{
    /// <summary>
    /// Comment content
    /// </summary>
    [Required]
    public string Content { get; set; } = "This is a great movie!";

    /// <summary>
    /// Parent comment ID (for replies, leave null for root comments)
    /// </summary>
    public Guid? ParentCommentId { get; set; }
}

public class UpdateCommentRequestDto
{
    /// <summary>
    /// Updated comment content
    /// </summary>
    [Required]
    public string Content { get; set; } = "Updated comment text";
}

public class AddReactionRequestDto
{
    /// <summary>
    /// Reaction type (1 = Upvote, -1 = Downvote)
    /// </summary>
    [Required]
    public ReactionType ReactionType { get; set; } = ReactionType.Upvote;
}
