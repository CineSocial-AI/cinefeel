using CineSocial.Application.Common.Results;
using CineSocial.Application.Features.Rates.Queries.GetMovieRates;
using MediatR;

namespace CineSocial.Application.Features.Rates.Queries.GetMovieRatesSummary;

public record GetMovieRatesSummaryQuery(Guid MovieId) : IRequest<Result<MovieRatesSummary>>;
