using CineSocial.Application.Common.Results;
using MediatR;

namespace CineSocial.Application.Features.Rates.Commands.DeleteRate;

public record DeleteRateCommand(Guid MovieId) : IRequest<Result>;
