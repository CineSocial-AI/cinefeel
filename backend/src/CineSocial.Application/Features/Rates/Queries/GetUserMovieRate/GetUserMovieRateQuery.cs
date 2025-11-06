using CineSocial.Application.Common.Results;
using CineSocial.Application.Features.Rates.Queries.GetMovieRates;
using MediatR;

namespace CineSocial.Application.Features.Rates.Queries.GetUserMovieRate;

public record GetUserMovieRateQuery(Guid MovieId) : IRequest<Result<RateDto?>>;
