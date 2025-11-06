using CineSocial.Application.Common.Interfaces;
using CineSocial.Application.Common.Results;
using CineSocial.Domain.Entities.Movie;
using CineSocial.Domain.Entities.Social;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CineSocial.Application.Features.Rates.Queries.GetMovieRates;

public class GetMovieRatesQueryHandler : IRequestHandler<GetMovieRatesQuery, PagedResult<List<RateDto>>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetMovieRatesQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<PagedResult<List<RateDto>>> Handle(GetMovieRatesQuery request, CancellationToken cancellationToken)
    {
        // Check if movie exists
        var movieExists = await _unitOfWork.Repository<MovieEntity>()
            .Query()
            .AnyAsync(m => m.Id == request.MovieId, cancellationToken);

        if (!movieExists)
        {
            return PagedResult<List<RateDto>>.Failure(Error.NotFound(
                "Rate.MovieNotFound",
                $"Movie with ID {request.MovieId} not found"
            ));
        }

        var query = _unitOfWork.Repository<Rate>()
            .Query()
            .Include(r => r.User)
            .Where(r => r.MovieId == request.MovieId)
            .OrderByDescending(r => r.CreatedAt);

        var totalCount = await query.CountAsync(cancellationToken);

        var rates = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(r => new RateDto
            {
                RateId = r.Id,
                UserId = r.UserId,
                Username = r.User.Username,
                Rating = r.Rating,
                CreatedAt = r.CreatedAt,
                UpdatedAt = r.UpdatedAt ?? r.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return PagedResult<List<RateDto>>.Success(
            rates,
            request.Page,
            request.PageSize,
            totalCount
        );
    }
}
