using CineSocial.Application.Common.Interfaces;
using CineSocial.Application.Common.Results;
using CineSocial.Application.Features.MovieLists.Queries.GetMyMovieLists;
using CineSocial.Domain.Entities.Social;
using CineSocial.Domain.Entities.User;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CineSocial.Application.Features.MovieLists.Queries.GetUserMovieLists;

public class GetUserMovieListsQueryHandler : IRequestHandler<GetUserMovieListsQuery, Result<List<MovieListSummaryDto>>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public GetUserMovieListsQueryHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<Result<List<MovieListSummaryDto>>> Handle(GetUserMovieListsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var currentUserId = _currentUserService.UserId;

            // Check if user exists
            var userExists = await _unitOfWork.Repository<AppUser>()
                .Query()
                .AnyAsync(u => u.Id == request.UserId, cancellationToken);

            if (!userExists)
            {
                return Error.NotFound("User.NotFound", $"User with ID {request.UserId} not found");
            }

            // If viewing own profile, show all lists
            // If viewing others, only show public lists
            var query = _unitOfWork.Repository<MovieList>()
                .Query()
                .Where(ml => ml.UserId == request.UserId);

            if (!currentUserId.HasValue || currentUserId.Value != request.UserId)
            {
                query = query.Where(ml => ml.IsPublic);
            }

            var movieLists = await query
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
