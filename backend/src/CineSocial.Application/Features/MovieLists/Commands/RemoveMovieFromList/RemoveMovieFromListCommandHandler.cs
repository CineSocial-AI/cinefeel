using CineSocial.Application.Common.Interfaces;
using CineSocial.Application.Common.Results;
using CineSocial.Domain.Entities.Social;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CineSocial.Application.Features.MovieLists.Commands.RemoveMovieFromList;

public class RemoveMovieFromListCommandHandler : IRequestHandler<RemoveMovieFromListCommand, Result>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public RemoveMovieFromListCommandHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<Result> Handle(RemoveMovieFromListCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var userId = _currentUserService.UserId;
            if (!userId.HasValue)
            {
                return Error.Unauthorized("User.Unauthorized", "User is not authenticated");
            }

            // Check if movie list exists and belongs to user
            var movieList = await _unitOfWork.Repository<MovieList>()
                .Query()
                .FirstOrDefaultAsync(ml => ml.Id == request.MovieListId, cancellationToken);

            if (movieList == null)
            {
                return Error.NotFound("MovieList.NotFound", $"Movie list with ID {request.MovieListId} not found");
            }

            if (movieList.UserId != userId.Value)
            {
                return Error.Forbidden("MovieList.Forbidden", "You don't have permission to modify this list");
            }

            // Find the movie list item
            var movieListItem = await _unitOfWork.Repository<MovieListItem>()
                .Query()
                .FirstOrDefaultAsync(mli => mli.MovieListId == request.MovieListId && mli.MovieId == request.MovieId, cancellationToken);

            if (movieListItem == null)
            {
                return Error.NotFound("MovieList.MovieNotInList", "Movie is not in this list");
            }

            _unitOfWork.Repository<MovieListItem>().HardDelete(movieListItem);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }
        catch (OperationCanceledException)
        {
            return Error.Failure("MovieList.RequestCancelled", "Request was cancelled");
        }
    }
}
