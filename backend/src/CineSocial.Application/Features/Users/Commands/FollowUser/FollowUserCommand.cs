using CineSocial.Application.Common.Results;
using MediatR;

namespace CineSocial.Application.Features.Users.Commands.FollowUser;

public record FollowUserCommand(Guid TargetUserId) : IRequest<Result<FollowResponse>>;

public record FollowResponse
{
    public bool IsNew { get; init; }
    public string Message { get; init; } = string.Empty;
}
