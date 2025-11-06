using CineSocial.Application.Common.Results;
using MediatR;

namespace CineSocial.Application.Features.Search.Queries.SearchPeople;

public record SearchPeopleQuery(
    string Query,
    int Page = 1,
    int PageSize = 20
) : IRequest<PagedResult<List<PersonSearchResultDto>>>;

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
