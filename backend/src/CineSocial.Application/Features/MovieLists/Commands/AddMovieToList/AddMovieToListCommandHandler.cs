using CineSocial.Application.Common.Interfaces;
using CineSocial.Application.Common.Results;
using CineSocial.Domain.Entities.Movie;
using CineSocial.Domain.Entities.Social;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CineSocial.Application.Features.MovieLists.Commands.AddMovieToList;

public class AddMovieToListCommandHandler : IRequestHandler<AddMovieToListCommand, Result>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public AddMovieToListCommandHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<Result> Handle(AddMovieToListCommand request, CancellationToken cancellationToken)
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

            // Check if movie exists
            var movieExists = await _unitOfWork.Repository<MovieEntity>()
                .Query()
                .AnyAsync(m => m.Id == request.MovieId, cancellationToken);

            if (!movieExists)
            {
                return Error.NotFound("Movie.NotFound", $"Movie with ID {request.MovieId} not found");
            }

            // Check if movie already in list
            var existingItem = await _unitOfWork.Repository<MovieListItem>()
                .Query()
                .FirstOrDefaultAsync(mli => mli.MovieListId == request.MovieListId && mli.MovieId == request.MovieId, cancellationToken);

            if (existingItem != null)
            {
                return Error.Validation("MovieList.MovieAlreadyInList", "Movie is already in this list");
            }

            // Get current max order
            var maxOrder = await _unitOfWork.Repository<MovieListItem>()
                .Query()
                .Where(mli => mli.MovieListId == request.MovieListId)
                .Select(mli => (int?)mli.Order)
                .MaxAsync(cancellationToken) ?? -1;

            var movieListItem = new MovieListItem
            {
                Id = Guid.NewGuid(),
                MovieListId = request.MovieListId,
                MovieId = request.MovieId,
                Order = maxOrder + 1,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.Repository<MovieListItem>().AddAsync(movieListItem);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }
        catch (OperationCanceledException)
        {
            return Error.Failure("MovieList.RequestCancelled", "Request was cancelled");
        }
    }
}
