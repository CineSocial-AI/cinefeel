using CineSocial.Application.Common.Interfaces;
using CineSocial.Application.Common.Results;
using CineSocial.Domain.Entities.Social;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CineSocial.Application.Features.MovieLists.Commands.UpdateMovieList;

public class UpdateMovieListCommandHandler : IRequestHandler<UpdateMovieListCommand, Result>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public UpdateMovieListCommandHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<Result> Handle(UpdateMovieListCommand request, CancellationToken cancellationToken)
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
                return Error.Forbidden("MovieList.Forbidden", "You don't have permission to update this list");
            }

            // For watchlist, only allow updating description and isPublic, not name
            if (movieList.IsWatchlist && request.Name != movieList.Name)
            {
                return Error.Validation("MovieList.CannotRenameWatchlist", "Cannot change the name of the default watchlist");
            }

            movieList.Name = request.Name;
            movieList.Description = request.Description;
            movieList.IsPublic = request.IsPublic;
            movieList.UpdatedAt = DateTime.UtcNow;

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
