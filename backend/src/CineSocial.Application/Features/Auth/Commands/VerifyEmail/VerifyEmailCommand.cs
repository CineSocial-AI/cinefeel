using CineSocial.Application.Common.Results;
using MediatR;

namespace CineSocial.Application.Features.Auth.Commands.VerifyEmail;

public record VerifyEmailCommand(string Token) : IRequest<Result<string>>;
