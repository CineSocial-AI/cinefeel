using CineSocial.Application.Common.Interfaces;
using CineSocial.Application.Common.Results;
using MediatR;

namespace CineSocial.Application.Features.Search.Queries.SearchAll;

public record SearchAllQuery(
    string Query,
    int Page = 1,
    int PageSize = 20
) : IRequest<Result<SearchAllResultDto>>, ICacheableQuery
{
    /// <summary>
    /// Search results cached for 15 minutes
    /// </summary>
    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(15);

    /// <summary>
    /// Cache key prefix for invalidation when movies/people are added/updated
    /// </summary>
    public string CacheKeyPrefix => "SearchAll";
};

public record SearchAllResultDto
{
    public List<MovieSearchItemDto> Movies { get; init; } = new();
    public List<PersonSearchItemDto> People { get; init; } = new();
}

public record MovieSearchItemDto
{
    public Guid Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string? PosterPath { get; init; }
    public double? VoteAverage { get; init; }
    public DateTime? ReleaseDate { get; init; }
    public string ResultType { get; init; } = "Movie";
}

public record PersonSearchItemDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? ProfilePath { get; init; }
    public string? KnownForDepartment { get; init; }
    public double? Popularity { get; init; }
    public string ResultType { get; init; } = "Person";
}
