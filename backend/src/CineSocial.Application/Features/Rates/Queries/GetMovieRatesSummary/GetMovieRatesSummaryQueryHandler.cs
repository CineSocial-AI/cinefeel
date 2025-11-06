using CineSocial.Application.Common.Interfaces;
using CineSocial.Application.Common.Results;
using CineSocial.Application.Features.Rates.Queries.GetMovieRates;
using CineSocial.Domain.Entities.Movie;
using CineSocial.Domain.Entities.Social;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CineSocial.Application.Features.Rates.Queries.GetMovieRatesSummary;

public class GetMovieRatesSummaryQueryHandler : IRequestHandler<GetMovieRatesSummaryQuery, Result<MovieRatesSummary>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetMovieRatesSummaryQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<MovieRatesSummary>> Handle(GetMovieRatesSummaryQuery request, CancellationToken cancellationToken)
    {
        // Check if movie exists
        var movieExists = await _unitOfWork.Repository<MovieEntity>()
            .Query()
            .AnyAsync(m => m.Id == request.MovieId, cancellationToken);

        if (!movieExists)
        {
            return Result.Failure<MovieRatesSummary>(Error.NotFound(
                "Rate.MovieNotFound",
                $"Movie with ID {request.MovieId} not found"
            ));
        }

        var rates = await _unitOfWork.Repository<Rate>()
            .Query()
            .Where(r => r.MovieId == request.MovieId)
            .Select(r => r.Rating)
            .ToListAsync(cancellationToken);

        if (rates.Count == 0)
        {
            return Result.Success(new MovieRatesSummary
            {
                MovieId = request.MovieId,
                AverageRating = 0,
                TotalRates = 0,
                RatingDistribution = new Dictionary<int, int>()
            });
        }

        var averageRating = rates.Average();
        var totalRates = rates.Count;

        // Calculate rating distribution (0-10)
        var distribution = new Dictionary<int, int>();
        for (int i = 0; i <= 10; i++)
        {
            distribution[i] = rates.Count(r => (int)Math.Round(r) == i);
        }

        var summary = new MovieRatesSummary
        {
            MovieId = request.MovieId,
            AverageRating = Math.Round(averageRating, 1),
            TotalRates = totalRates,
            RatingDistribution = distribution
        };

        return Result.Success(summary);
    }
}
