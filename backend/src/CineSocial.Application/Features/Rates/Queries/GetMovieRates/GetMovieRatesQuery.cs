using CineSocial.Application.Common.Results;
using MediatR;

namespace CineSocial.Application.Features.Rates.Queries.GetMovieRates;

public record GetMovieRatesQuery(
    Guid MovieId,
    int Page = 1,
    int PageSize = 20
) : IRequest<PagedResult<List<RateDto>>>;

public record RateDto
{
    public Guid RateId { get; init; }
    public Guid UserId { get; init; }
    public string Username { get; init; } = string.Empty;
    public decimal Rating { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}

public record MovieRatesSummary
{
    public Guid MovieId { get; init; }
    public decimal AverageRating { get; init; }
    public int TotalRates { get; init; }
    public Dictionary<int, int> RatingDistribution { get; init; } = new();
}
