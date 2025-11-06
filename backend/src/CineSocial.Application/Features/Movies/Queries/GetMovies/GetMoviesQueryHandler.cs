using CineSocial.Application.Common.Interfaces;
using CineSocial.Application.Common.Results;
using CineSocial.Domain.Entities.Movie;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CineSocial.Application.Features.Movies.Queries.GetMovies;

public class GetMoviesQueryHandler : IRequestHandler<GetMoviesQuery, PagedResult<List<MovieDto>>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetMoviesQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<PagedResult<List<MovieDto>>> Handle(GetMoviesQuery request, CancellationToken cancellationToken)
    {
        var query = _unitOfWork.Repository<MovieEntity>()
            .Query()
            .Include(m => m.MovieGenres)
                .ThenInclude(mg => mg.Genre)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.ToLower();
            query = query.Where(m =>
                m.Title.ToLower().Contains(search) ||
                (m.OriginalTitle != null && m.OriginalTitle.ToLower().Contains(search)));
        }

        if (request.GenreId.HasValue)
        {
            query = query.Where(m => m.MovieGenres.Any(mg => mg.GenreId == request.GenreId.Value));
        }

        if (request.Year.HasValue)
        {
            query = query.Where(m => m.ReleaseDate.HasValue && m.ReleaseDate.Value.Year == request.Year.Value);
        }

        query = request.SortBy switch
        {
            MovieSortBy.ReleaseDate => query.OrderByDescending(m => m.ReleaseDate),
            MovieSortBy.VoteAverage => query.OrderByDescending(m => m.VoteAverage),
            MovieSortBy.Title => query.OrderBy(m => m.Title),
            _ => query.OrderByDescending(m => m.Popularity)
        };

        var totalCount = await query.CountAsync(cancellationToken);

        var movies = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(m => new MovieDto
            {
                Id = m.Id,
                TmdbId = m.TmdbId,
                Title = m.Title,
                OriginalTitle = m.OriginalTitle,
                Overview = m.Overview,
                ReleaseDate = m.ReleaseDate,
                PosterPath = m.PosterPath,
                BackdropPath = m.BackdropPath,
                VoteAverage = m.VoteAverage,
                VoteCount = m.VoteCount,
                Popularity = m.Popularity,
                Genres = m.MovieGenres.Select(mg => mg.Genre.Name).ToList()
            })
            .ToListAsync(cancellationToken);

        return PagedResult<List<MovieDto>>.Success(movies, request.Page, request.PageSize, totalCount);
    }
}
