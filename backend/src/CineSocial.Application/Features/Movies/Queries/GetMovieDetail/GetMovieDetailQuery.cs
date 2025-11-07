using CineSocial.Application.Common.Interfaces;
using CineSocial.Application.Common.Results;
using MediatR;

namespace CineSocial.Application.Features.Movies.Queries.GetMovieDetail;

public record GetMovieDetailQuery(Guid Id) : IRequest<Result<MovieDetailDto>>, ICacheableQuery
{
    /// <summary>
    /// Movie details cached for 1 hour (TMDB data rarely changes)
    /// </summary>
    public TimeSpan? CacheDuration => TimeSpan.FromHours(1);

    public string CacheKeyPrefix => "GetMovieDetail";
};

public record MovieDetailDto
{
    public Guid Id { get; init; }
    public int TmdbId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string? OriginalTitle { get; init; }
    public string? Overview { get; init; }
    public DateTime? ReleaseDate { get; init; }
    public int? Runtime { get; init; }
    public decimal? Budget { get; init; }
    public decimal? Revenue { get; init; }
    public string? PosterPath { get; init; }
    public string? BackdropPath { get; init; }
    public string? ImdbId { get; init; }
    public string? OriginalLanguage { get; init; }
    public double? Popularity { get; init; }
    public double? VoteAverage { get; init; }
    public int? VoteCount { get; init; }
    public string? Status { get; init; }
    public string? Tagline { get; init; }
    public string? Homepage { get; init; }
    public bool Adult { get; init; }

    public List<GenreDto> Genres { get; init; } = new();
    public List<CastDto> Cast { get; init; } = new();
    public List<CrewDto> Crew { get; init; } = new();
    public List<ProductionCompanyDto> ProductionCompanies { get; init; } = new();
    public List<CountryDto> Countries { get; init; } = new();
    public List<LanguageDto> Languages { get; init; } = new();
    public List<KeywordDto> Keywords { get; init; } = new();
    public List<VideoDto> Videos { get; init; } = new();
    public List<ImageDto> Images { get; init; } = new();
    public List<CollectionDto> Collections { get; init; } = new();
}

public record GenreDto
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
}

public record CastDto
{
    public Guid PersonId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Character { get; init; }
    public int? CastOrder { get; init; }
    public string? ProfilePath { get; init; }
}

public record CrewDto
{
    public Guid PersonId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Job { get; init; }
    public string? Department { get; init; }
    public string? ProfilePath { get; init; }
}

public record ProductionCompanyDto
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? LogoPath { get; init; }
    public string? OriginCountry { get; init; }
}

public record CountryDto
{
    public int Id { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
}

public record LanguageDto
{
    public int Id { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
}

public record KeywordDto
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
}

public record VideoDto
{
    public Guid Id { get; init; }
    public string VideoKey { get; init; } = string.Empty;
    public string? Name { get; init; }
    public string? Site { get; init; }
    public string? Type { get; init; }
    public bool Official { get; init; }
}

public record ImageDto
{
    public Guid Id { get; init; }
    public string FilePath { get; init; } = string.Empty;
    public string? ImageType { get; init; }
    public string? Language { get; init; }
    public double? VoteAverage { get; init; }
    public int? VoteCount { get; init; }
    public int? Width { get; init; }
    public int? Height { get; init; }
}

public record CollectionDto
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? PosterPath { get; init; }
    public string? BackdropPath { get; init; }
}
