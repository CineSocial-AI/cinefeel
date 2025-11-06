using CineSocial.Application.Common.Interfaces;
using CineSocial.Application.Common.Results;
using CineSocial.Domain.Entities.Movie;
using CineSocial.Domain.Entities.Social;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CineSocial.Application.Features.Movies.Commands.FavoriteMovie;

public class FavoriteMovieCommandHandler : IRequestHandler<FavoriteMovieCommand, Result>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public FavoriteMovieCommandHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<Result> Handle(FavoriteMovieCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var userId = _currentUserService.UserId;
            if (!userId.HasValue)
            {
                return Error.Unauthorized("User.Unauthorized", "User is not authenticated");
            }

            // Check if movie exists
            var movieExists = await _unitOfWork.Repository<MovieEntity>()
                .Query()
                .AnyAsync(m => m.Id == request.MovieId, cancellationToken);

            if (!movieExists)
            {
                return Error.NotFound("Movie.NotFound", $"Movie with ID {request.MovieId} not found");
            }

            // Check if already favorited
            var existingFavorite = await _unitOfWork.Repository<MovieFavorite>()
                .Query()
                .FirstOrDefaultAsync(mf => mf.UserId == userId.Value && mf.MovieId == request.MovieId, cancellationToken);

            if (existingFavorite != null)
            {
                return Error.Validation("Movie.AlreadyFavorited", "You have already favorited this movie");
            }

            var favorite = new MovieFavorite
            {
                Id = Guid.NewGuid(),
                UserId = userId.Value,
                MovieId = request.MovieId,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.Repository<MovieFavorite>().AddAsync(favorite);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }
        catch (OperationCanceledException)
        {
            return Error.Failure("Movie.RequestCancelled", "Request was cancelled");
        }
    }
}
