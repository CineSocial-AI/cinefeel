using CineSocial.Application.Common.Interfaces;
using CineSocial.Application.Common.Results;
using CineSocial.Domain.Entities.Social;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CineSocial.Application.Features.MovieLists.Commands.FavoriteMovieList;

public class FavoriteMovieListCommandHandler : IRequestHandler<FavoriteMovieListCommand, Result>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public FavoriteMovieListCommandHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<Result> Handle(FavoriteMovieListCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var userId = _currentUserService.UserId;
            if (!userId.HasValue)
            {
                return Error.Unauthorized("User.Unauthorized", "User is not authenticated");
            }

            // Check if movie list exists
            var movieList = await _unitOfWork.Repository<MovieList>()
                .Query()
                .FirstOrDefaultAsync(ml => ml.Id == request.MovieListId, cancellationToken);

            if (movieList == null)
            {
                return Error.NotFound("MovieList.NotFound", $"Movie list with ID {request.MovieListId} not found");
            }

            // Cannot favorite own list
            if (movieList.UserId == userId.Value)
            {
                return Error.Validation("MovieList.CannotFavoriteOwnList", "You cannot favorite your own list");
            }

            // Check if list is public
            if (!movieList.IsPublic)
            {
                return Error.Forbidden("MovieList.NotPublic", "Cannot favorite a private list");
            }

            // Check if already favorited
            var existingFavorite = await _unitOfWork.Repository<MovieListFavorite>()
                .Query()
                .FirstOrDefaultAsync(mlf => mlf.MovieListId == request.MovieListId && mlf.UserId == userId.Value, cancellationToken);

            if (existingFavorite != null)
            {
                return Error.Validation("MovieList.AlreadyFavorited", "You have already favorited this list");
            }

            var favorite = new MovieListFavorite
            {
                Id = Guid.NewGuid(),
                UserId = userId.Value,
                MovieListId = request.MovieListId,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.Repository<MovieListFavorite>().AddAsync(favorite);

            // Update favorite count
            movieList.FavoriteCount++;
            _unitOfWork.Repository<MovieList>().Update(movieList);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }
        catch (OperationCanceledException)
        {
            return Error.Failure("MovieList.RequestCancelled", "Request was cancelled");
        }
    }
}
