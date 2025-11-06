using CineSocial.Application.Common.Interfaces;
using CineSocial.Application.Common.Results;
using CineSocial.Domain.Entities.Social;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CineSocial.Application.Features.MovieLists.Commands.DeleteMovieList;

public class DeleteMovieListCommandHandler : IRequestHandler<DeleteMovieListCommand, Result>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public DeleteMovieListCommandHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<Result> Handle(DeleteMovieListCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var userId = _currentUserService.UserId;
            if (!userId.HasValue)
            {
                return Error.Unauthorized("User.Unauthorized", "User is not authenticated");
            }

            var movieList = await _unitOfWork.Repository<MovieList>()
                .Query()
                .FirstOrDefaultAsync(ml => ml.Id == request.MovieListId, cancellationToken);

            if (movieList == null)
            {
                return Error.NotFound("MovieList.NotFound", $"Movie list with ID {request.MovieListId} not found");
            }

            if (movieList.UserId != userId.Value)
            {
                return Error.Forbidden("MovieList.Forbidden", "You don't have permission to delete this list");
            }

            if (movieList.IsWatchlist)
            {
                return Error.Validation("MovieList.CannotDeleteWatchlist", "Cannot delete the default watchlist");
            }

            _unitOfWork.Repository<MovieList>().Delete(movieList);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }
        catch (OperationCanceledException)
        {
            return Error.Failure("MovieList.RequestCancelled", "Request was cancelled");
        }
    }
}
