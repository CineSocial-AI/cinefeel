using CineSocial.Application.Common.Results;
using MediatR;

namespace CineSocial.Application.Features.Auth.Commands.Register;

public record RegisterCommand(
    string Username,
    string Email,
    string Password
) : IRequest<Result<AuthResponse>>;

public record AuthResponse
{
    public Guid UserId { get; init; }
    public string Username { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string Role { get; init; } = string.Empty;
    public string Token { get; init; } = string.Empty;
}
