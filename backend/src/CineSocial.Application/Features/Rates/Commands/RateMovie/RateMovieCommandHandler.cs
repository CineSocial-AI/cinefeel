using CineSocial.Application.Common.Interfaces;
using CineSocial.Application.Common.Results;
using CineSocial.Domain.Entities.Movie;
using CineSocial.Domain.Entities.Social;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CineSocial.Application.Features.Rates.Commands.RateMovie;

public class RateMovieCommandHandler : IRequestHandler<RateMovieCommand, Result<RateResponse>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public RateMovieCommandHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<Result<RateResponse>> Handle(RateMovieCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        if (userId == null)
        {
            return Result.Failure<RateResponse>(Error.Unauthorized(
                "Rate.Unauthorized",
                "User must be authenticated to rate movies"
            ));
        }

        // Check if movie exists
        var movieExists = await _unitOfWork.Repository<MovieEntity>()
            .Query()
            .AnyAsync(m => m.Id == request.MovieId, cancellationToken);

        if (!movieExists)
        {
            return Result.Failure<RateResponse>(Error.NotFound(
                "Rate.MovieNotFound",
                $"Movie with ID {request.MovieId} not found"
            ));
        }

        // Check if user already rated this movie
        var existingRate = await _unitOfWork.Repository<Rate>()
            .Query()
            .FirstOrDefaultAsync(r => r.UserId == userId && r.MovieId == request.MovieId, cancellationToken);

        bool isNew = false;
        Rate rate;

        if (existingRate != null)
        {
            // Update existing rate
            existingRate.Rating = request.Rating;
            existingRate.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.Repository<Rate>().Update(existingRate);
            rate = existingRate;
        }
        else
        {
            // Create new rate
            rate = new Rate
            {
                Id = Guid.NewGuid(),
                UserId = userId.Value,
                MovieId = request.MovieId,
                Rating = request.Rating,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            await _unitOfWork.Repository<Rate>().AddAsync(rate, cancellationToken);
            isNew = true;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var response = new RateResponse
        {
            RateId = rate.Id,
            MovieId = rate.MovieId,
            UserId = rate.UserId,
            Rating = rate.Rating,
            IsNew = isNew,
            CreatedAt = rate.CreatedAt,
            UpdatedAt = rate.UpdatedAt ?? rate.CreatedAt
        };

        return Result.Success(response);
    }
}
