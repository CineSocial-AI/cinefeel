using CineSocial.Application.Common.Interfaces;
using CineSocial.Application.Common.Results;
using MediatR;

namespace CineSocial.Application.Features.Search.Queries.SearchPeople;

public record SearchPeopleQuery(
    string Query,
    int Page = 1,
    int PageSize = 20
) : IRequest<PagedResult<List<PersonSearchResultDto>>>, ICacheableQuery
{
    /// <summary>
    /// Search results cached for 15 minutes
    /// </summary>
    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(15);

    /// <summary>
    /// Cache key prefix for invalidation when people are added/updated
    /// </summary>
    public string CacheKeyPrefix => "SearchPeople";
};

public record PersonSearchResultDto
{
    public Guid Id { get; init; }
    public int TmdbId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? ProfilePath { get; init; }
    public double? Popularity { get; init; }
    public string? KnownForDepartment { get; init; }
    public int MovieCount { get; init; }
}
