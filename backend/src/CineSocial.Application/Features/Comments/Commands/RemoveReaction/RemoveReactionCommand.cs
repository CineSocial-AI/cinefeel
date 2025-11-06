using CineSocial.Application.Common.Results;
using MediatR;

namespace CineSocial.Application.Features.Comments.Commands.RemoveReaction;

public record RemoveReactionCommand(Guid CommentId) : IRequest<Result>;
