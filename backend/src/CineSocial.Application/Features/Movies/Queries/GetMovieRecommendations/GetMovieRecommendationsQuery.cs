using MediatR;
using CineSocial.Application.Common.Results;

namespace CineSocial.Application.Features.Movies.Queries.GetMovieRecommendations;

public class GetMovieRecommendationsQuery : IRequest<Result<List<MovieRecommendationDto>>>
{
    public Guid MovieId { get; set; }
    public int Limit { get; set; } = 10;
}
