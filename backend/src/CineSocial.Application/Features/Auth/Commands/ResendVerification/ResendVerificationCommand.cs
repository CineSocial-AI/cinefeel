using CineSocial.Application.Common.Results;
using MediatR;

namespace CineSocial.Application.Features.Auth.Commands.ResendVerification;

public record ResendVerificationCommand(string Email) : IRequest<Result<Unit>>;
