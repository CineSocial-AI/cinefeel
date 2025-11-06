using CineSocial.Application.Common.Interfaces;
using CineSocial.Application.Common.Results;
using CineSocial.Domain.Entities.Social;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CineSocial.Application.Features.Movies.Queries.GetFavoriteMovies;

public class GetFavoriteMoviesQueryHandler : IRequestHandler<GetFavoriteMoviesQuery, PagedResult<List<FavoriteMovieDto>>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public GetFavoriteMoviesQueryHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<PagedResult<List<FavoriteMovieDto>>> Handle(GetFavoriteMoviesQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var userId = _currentUserService.UserId;
            if (!userId.HasValue)
            {
                return PagedResult<List<FavoriteMovieDto>>.Failure(Error.Unauthorized(
                    "User.Unauthorized",
                    "User is not authenticated"
                ));
            }

            var query = _unitOfWork.Repository<MovieFavorite>()
                .Query()
                .Include(mf => mf.Movie)
                .Where(mf => mf.UserId == userId.Value)
                .OrderByDescending(mf => mf.CreatedAt);

            var totalCount = await query.CountAsync(cancellationToken);

            var favorites = await query
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(mf => new FavoriteMovieDto
                {
                    MovieId = mf.MovieId,
                    Title = mf.Movie.Title,
                    PosterPath = mf.Movie.PosterPath,
                    VoteAverage = mf.Movie.VoteAverage,
                    ReleaseDate = mf.Movie.ReleaseDate,
                    FavoritedAt = mf.CreatedAt
                })
                .ToListAsync(cancellationToken);

            return PagedResult<List<FavoriteMovieDto>>.Success(
                favorites,
                request.Page,
                request.PageSize,
                totalCount
            );
        }
        catch (OperationCanceledException)
        {
            return PagedResult<List<FavoriteMovieDto>>.Failure(Error.Failure(
                "Movie.RequestCancelled",
                "Request was cancelled"
            ));
        }
    }
}
