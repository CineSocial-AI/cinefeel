using CineSocial.Application.Common.Results;
using MediatR;

namespace CineSocial.Application.Features.People.Queries.GetPersonDetail;

public record GetPersonDetailQuery(Guid Id) : IRequest<Result<PersonDetailDto>>;

public record PersonDetailDto
{
    public Guid Id { get; init; }
    public int TmdbId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Biography { get; init; }
    public DateTime? Birthday { get; init; }
    public DateTime? Deathday { get; init; }
    public string? PlaceOfBirth { get; init; }
    public string? ProfilePath { get; init; }
    public double? Popularity { get; init; }
    public int? Gender { get; init; }
    public string? KnownForDepartment { get; init; }
    public string? ImdbId { get; init; }
    public List<PersonMovieCreditDto> CastCredits { get; init; } = new();
    public List<PersonMovieCreditDto> CrewCredits { get; init; } = new();
}

public record PersonMovieCreditDto
{
    public Guid MovieId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string? PosterPath { get; init; }
    public DateTime? ReleaseDate { get; init; }
    public double? VoteAverage { get; init; }
    public string? Character { get; init; }
    public string? Job { get; init; }
    public string? Department { get; init; }
}
