using CineSocial.Application.Common.Results;
using CineSocial.Application.Features.Auth.Commands.Register;
using MediatR;

namespace CineSocial.Application.Features.Auth.Commands.Login;

public record LoginCommand(
    string UsernameOrEmail,
    string Password
) : IRequest<Result<AuthResponse>>;
