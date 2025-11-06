using CineSocial.Application.Common.Interfaces;
using CineSocial.Application.Common.Results;
using CineSocial.Domain.Entities.Social;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CineSocial.Application.Features.MovieLists.Commands.UnfavoriteMovieList;

public class UnfavoriteMovieListCommandHandler : IRequestHandler<UnfavoriteMovieListCommand, Result>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public UnfavoriteMovieListCommandHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<Result> Handle(UnfavoriteMovieListCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var userId = _currentUserService.UserId;
            if (!userId.HasValue)
            {
                return Error.Unauthorized("User.Unauthorized", "User is not authenticated");
            }

            var favorite = await _unitOfWork.Repository<MovieListFavorite>()
                .Query()
                .FirstOrDefaultAsync(mlf => mlf.MovieListId == request.MovieListId && mlf.UserId == userId.Value, cancellationToken);

            if (favorite == null)
            {
                return Error.NotFound("MovieList.NotFavorited", "You have not favorited this list");
            }

            _unitOfWork.Repository<MovieListFavorite>().HardDelete(favorite);

            // Update favorite count
            var movieList = await _unitOfWork.Repository<MovieList>()
                .Query()
                .FirstOrDefaultAsync(ml => ml.Id == request.MovieListId, cancellationToken);

            if (movieList != null)
            {
                movieList.FavoriteCount = Math.Max(0, movieList.FavoriteCount - 1);
                _unitOfWork.Repository<MovieList>().Update(movieList);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }
        catch (OperationCanceledException)
        {
            return Error.Failure("MovieList.RequestCancelled", "Request was cancelled");
        }
    }
}
