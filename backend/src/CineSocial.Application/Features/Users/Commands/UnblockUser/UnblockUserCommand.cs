using CineSocial.Application.Common.Results;
using MediatR;

namespace CineSocial.Application.Features.Users.Commands.UnblockUser;

public record UnblockUserCommand(Guid TargetUserId) : IRequest<Result>;
