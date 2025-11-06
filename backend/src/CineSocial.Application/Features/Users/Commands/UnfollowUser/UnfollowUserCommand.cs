using CineSocial.Application.Common.Results;
using MediatR;

namespace CineSocial.Application.Features.Users.Commands.UnfollowUser;

public record UnfollowUserCommand(Guid TargetUserId) : IRequest<Result>;
