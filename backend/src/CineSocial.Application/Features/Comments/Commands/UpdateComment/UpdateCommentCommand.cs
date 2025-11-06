using CineSocial.Application.Common.Results;
using MediatR;

namespace CineSocial.Application.Features.Comments.Commands.UpdateComment;

public record UpdateCommentCommand(
    Guid CommentId,
    string Content
) : IRequest<Result>;
