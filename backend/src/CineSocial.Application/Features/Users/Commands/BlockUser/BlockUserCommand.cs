using CineSocial.Application.Common.Results;
using MediatR;

namespace CineSocial.Application.Features.Users.Commands.BlockUser;

public record BlockUserCommand(Guid TargetUserId) : IRequest<Result<BlockResponse>>;

public record BlockResponse
{
    public bool IsNew { get; init; }
    public string Message { get; init; } = string.Empty;
}
