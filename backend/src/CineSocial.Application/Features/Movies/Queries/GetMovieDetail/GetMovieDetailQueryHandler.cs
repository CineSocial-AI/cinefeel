using CineSocial.Application.Common.Interfaces;
using CineSocial.Application.Common.Results;
using CineSocial.Domain.Entities.Movie;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CineSocial.Application.Features.Movies.Queries.GetMovieDetail;

public class GetMovieDetailQueryHandler : IRequestHandler<GetMovieDetailQuery, Result<MovieDetailDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetMovieDetailQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<MovieDetailDto>> Handle(GetMovieDetailQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var movie = await _unitOfWork.Repository<MovieEntity>()
                .Query()
                .Include(m => m.MovieGenres).ThenInclude(mg => mg.Genre)
                .Include(m => m.MovieCasts).ThenInclude(mc => mc.Person)
                .Include(m => m.MovieCrews).ThenInclude(mc => mc.Person)
                .FirstOrDefaultAsync(m => m.Id == request.Id, cancellationToken);

            if (movie is null)
            {
                return Error.NotFound("Movie.NotFound", $"Movie with ID {request.Id} not found");
            }

        var dto = new MovieDetailDto
        {
            Id = movie.Id,
            TmdbId = movie.TmdbId,
            Title = movie.Title,
            OriginalTitle = movie.OriginalTitle,
            Overview = movie.Overview,
            ReleaseDate = movie.ReleaseDate,
            Runtime = movie.Runtime,
            Budget = movie.Budget,
            Revenue = movie.Revenue,
            PosterPath = movie.PosterPath,
            BackdropPath = movie.BackdropPath,
            ImdbId = movie.ImdbId,
            OriginalLanguage = movie.OriginalLanguage,
            Popularity = movie.Popularity,
            VoteAverage = movie.VoteAverage,
            VoteCount = movie.VoteCount,
            Status = movie.Status,
            Tagline = movie.Tagline,
            Homepage = movie.Homepage,
            Adult = movie.Adult,

            Genres = movie.MovieGenres.Select(mg => new GenreDto
            {
                Id = mg.GenreId,
                Name = mg.Genre.Name
            }).ToList(),

            Cast = movie.MovieCasts
                .OrderBy(mc => mc.CastOrder)
                .Take(10) // Limit to top 10 cast members
                .Select(mc => new CastDto
                {
                    PersonId = mc.PersonId,
                    Name = mc.Person.Name,
                    Character = mc.Character,
                    CastOrder = mc.CastOrder,
                    ProfilePath = mc.Person.ProfilePath
                }).ToList(),

            Crew = movie.MovieCrews
                .Where(mc => mc.Job == "Director" || mc.Job == "Producer" || mc.Job == "Writer")
                .Select(mc => new CrewDto
                {
                    PersonId = mc.PersonId,
                    Name = mc.Person.Name,
                    Job = mc.Job,
                    Department = mc.Department,
                    ProfilePath = mc.Person.ProfilePath
                }).ToList(),

            ProductionCompanies = new List<ProductionCompanyDto>(),
            Countries = new List<CountryDto>(),
            Languages = new List<LanguageDto>(),
            Keywords = new List<KeywordDto>(),
            Videos = new List<VideoDto>(),
            Images = new List<ImageDto>(),
            Collections = new List<CollectionDto>()
        };

            return Result.Success(dto);
        }
        catch (OperationCanceledException)
        {
            return Error.Failure("Movie.RequestCancelled", "Request was cancelled");
        }
    }
}
