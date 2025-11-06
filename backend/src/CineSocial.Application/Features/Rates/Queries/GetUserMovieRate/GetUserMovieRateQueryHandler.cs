using CineSocial.Application.Common.Interfaces;
using CineSocial.Application.Common.Results;
using CineSocial.Application.Features.Rates.Queries.GetMovieRates;
using CineSocial.Domain.Entities.Social;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CineSocial.Application.Features.Rates.Queries.GetUserMovieRate;

public class GetUserMovieRateQueryHandler : IRequestHandler<GetUserMovieRateQuery, Result<RateDto?>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public GetUserMovieRateQueryHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<Result<RateDto?>> Handle(GetUserMovieRateQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        if (userId == null)
        {
            return Result.Failure<RateDto?>(Error.Unauthorized(
                "Rate.Unauthorized",
                "User must be authenticated to view their rates"
            ));
        }

        var rate = await _unitOfWork.Repository<Rate>()
            .Query()
            .Include(r => r.User)
            .FirstOrDefaultAsync(r => r.UserId == userId && r.MovieId == request.MovieId, cancellationToken);

        if (rate == null)
        {
            return Result.Success<RateDto?>(null);
        }

        var rateDto = new RateDto
        {
            RateId = rate.Id,
            UserId = rate.UserId,
            Username = rate.User.Username,
            Rating = rate.Rating,
            CreatedAt = rate.CreatedAt,
            UpdatedAt = rate.UpdatedAt ?? rate.CreatedAt
        };

        return Result.Success<RateDto?>(rateDto);
    }
}
