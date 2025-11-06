using CineSocial.Application.Common.Interfaces;
using CineSocial.Application.Common.Results;
using CineSocial.Domain.Entities.Social;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CineSocial.Application.Features.MovieLists.Queries.GetMyMovieLists;

public class GetMyMovieListsQueryHandler : IRequestHandler<GetMyMovieListsQuery, Result<List<MovieListSummaryDto>>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public GetMyMovieListsQueryHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<Result<List<MovieListSummaryDto>>> Handle(GetMyMovieListsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var userId = _currentUserService.UserId;
            if (!userId.HasValue)
            {
                return Error.Unauthorized("User.Unauthorized", "User is not authenticated");
            }

            // Check if user has a watchlist, create if not exists
            var hasWatchlist = await _unitOfWork.Repository<MovieList>()
                .Query()
                .AnyAsync(ml => ml.UserId == userId.Value && ml.IsWatchlist, cancellationToken);

            if (!hasWatchlist)
            {
                var watchlist = new MovieList
                {
                    Id = Guid.NewGuid(),
                    UserId = userId.Value,
                    Name = "Watchlist",
                    Description = "My default watchlist",
                    IsPublic = false,
                    IsWatchlist = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                await _unitOfWork.Repository<MovieList>().AddAsync(watchlist, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }

            var movieLists = await _unitOfWork.Repository<MovieList>()
                .Query()
                .Where(ml => ml.UserId == userId.Value)
                .OrderByDescending(ml => ml.IsWatchlist)
                .ThenByDescending(ml => ml.CreatedAt)
                .Select(ml => new MovieListSummaryDto
                {
                    MovieListId = ml.Id,
                    Name = ml.Name,
                    Description = ml.Description,
                    IsPublic = ml.IsPublic,
                    IsWatchlist = ml.IsWatchlist,
                    MovieCount = ml.Items.Count,
                    FavoriteCount = ml.FavoriteCount,
                    CreatedAt = ml.CreatedAt
                })
                .ToListAsync(cancellationToken);

            return Result.Success(movieLists);
        }
        catch (OperationCanceledException)
        {
            return Error.Failure("MovieList.RequestCancelled", "Request was cancelled");
        }
    }
}
