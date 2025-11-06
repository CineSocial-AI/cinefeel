using CineSocial.Application.Common.Results;
using MediatR;

namespace CineSocial.Application.Features.Comments.Commands.DeleteComment;

public record DeleteCommentCommand(Guid CommentId) : IRequest<Result>;
