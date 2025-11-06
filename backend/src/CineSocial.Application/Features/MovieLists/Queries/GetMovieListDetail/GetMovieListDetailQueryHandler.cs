using CineSocial.Application.Common.Interfaces;
using CineSocial.Application.Common.Results;
using CineSocial.Domain.Entities.Social;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CineSocial.Application.Features.MovieLists.Queries.GetMovieListDetail;

public class GetMovieListDetailQueryHandler : IRequestHandler<GetMovieListDetailQuery, Result<MovieListDetailDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public GetMovieListDetailQueryHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<Result<MovieListDetailDto>> Handle(GetMovieListDetailQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var userId = _currentUserService.UserId;

            var movieList = await _unitOfWork.Repository<MovieList>()
                .Query()
                .Include(ml => ml.User)
                .Include(ml => ml.Items).ThenInclude(mli => mli.Movie)
                .FirstOrDefaultAsync(ml => ml.Id == request.MovieListId, cancellationToken);

            if (movieList == null)
            {
                return Error.NotFound("MovieList.NotFound", $"Movie list with ID {request.MovieListId} not found");
            }

            // Check permissions
            bool isOwner = userId.HasValue && movieList.UserId == userId.Value;
            if (!movieList.IsPublic && !isOwner)
            {
                return Error.Forbidden("MovieList.Private", "This list is private");
            }

            // Check if current user favorited this list
            bool isFavorited = false;
            if (userId.HasValue)
            {
                isFavorited = await _unitOfWork.Repository<MovieListFavorite>()
                    .Query()
                    .AnyAsync(mlf => mlf.MovieListId == request.MovieListId && mlf.UserId == userId.Value, cancellationToken);
            }

            var dto = new MovieListDetailDto
            {
                MovieListId = movieList.Id,
                UserId = movieList.UserId,
                Username = movieList.User.Username,
                Name = movieList.Name,
                Description = movieList.Description,
                IsPublic = movieList.IsPublic,
                IsWatchlist = movieList.IsWatchlist,
                FavoriteCount = movieList.FavoriteCount,
                IsFavorited = isFavorited,
                IsOwner = isOwner,
                CreatedAt = movieList.CreatedAt,
                Movies = movieList.Items
                    .OrderBy(mli => mli.Order)
                    .Select(mli => new MovieInListDto
                    {
                        MovieId = mli.MovieId,
                        Title = mli.Movie.Title,
                        PosterPath = mli.Movie.PosterPath,
                        VoteAverage = mli.Movie.VoteAverage,
                        ReleaseDate = mli.Movie.ReleaseDate,
                        Order = mli.Order
                    })
                    .ToList()
            };

            return Result.Success(dto);
        }
        catch (OperationCanceledException)
        {
            return Error.Failure("MovieList.RequestCancelled", "Request was cancelled");
        }
    }
}
