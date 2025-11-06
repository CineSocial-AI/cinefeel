using CineSocial.Application.Common.Interfaces;
using CineSocial.Application.Common.Results;
using CineSocial.Domain.Entities.Social;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CineSocial.Application.Features.Movies.Commands.UnfavoriteMovie;

public class UnfavoriteMovieCommandHandler : IRequestHandler<UnfavoriteMovieCommand, Result>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public UnfavoriteMovieCommandHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<Result> Handle(UnfavoriteMovieCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var userId = _currentUserService.UserId;
            if (!userId.HasValue)
            {
                return Error.Unauthorized("User.Unauthorized", "User is not authenticated");
            }

            var favorite = await _unitOfWork.Repository<MovieFavorite>()
                .Query()
                .FirstOrDefaultAsync(mf => mf.UserId == userId.Value && mf.MovieId == request.MovieId, cancellationToken);

            if (favorite == null)
            {
                return Error.NotFound("Movie.NotFavorited", "You have not favorited this movie");
            }

            _unitOfWork.Repository<MovieFavorite>().HardDelete(favorite);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }
        catch (OperationCanceledException)
        {
            return Error.Failure("Movie.RequestCancelled", "Request was cancelled");
        }
    }
}
