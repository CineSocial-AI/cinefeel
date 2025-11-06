using CineSocial.Application.Common.Interfaces;
using CineSocial.Application.Common.Results;
using CineSocial.Domain.Entities.Social;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CineSocial.Application.Features.MovieLists.Queries.GetPublicMovieLists;

public class GetPublicMovieListsQueryHandler : IRequestHandler<GetPublicMovieListsQuery, PagedResult<List<PublicMovieListDto>>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public GetPublicMovieListsQueryHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<PagedResult<List<PublicMovieListDto>>> Handle(GetPublicMovieListsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var userId = _currentUserService.UserId;

            var query = _unitOfWork.Repository<MovieList>()
                .Query()
                .Include(ml => ml.User)
                .Where(ml => ml.IsPublic)
                .OrderByDescending(ml => ml.FavoriteCount)
                .ThenByDescending(ml => ml.CreatedAt);

            var totalCount = await query.CountAsync(cancellationToken);

            var movieLists = await query
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync(cancellationToken);

            // Get favorited list IDs for current user
            List<Guid> favoritedListIds = new();
            if (userId.HasValue)
            {
                favoritedListIds = await _unitOfWork.Repository<MovieListFavorite>()
                    .Query()
                    .Where(mlf => mlf.UserId == userId.Value)
                    .Select(mlf => mlf.MovieListId)
                    .ToListAsync(cancellationToken);
            }

            var result = movieLists.Select(ml => new PublicMovieListDto
            {
                MovieListId = ml.Id,
                UserId = ml.UserId,
                Username = ml.User.Username,
                Name = ml.Name,
                Description = ml.Description,
                MovieCount = ml.Items.Count,
                FavoriteCount = ml.FavoriteCount,
                IsFavorited = favoritedListIds.Contains(ml.Id),
                CreatedAt = ml.CreatedAt
            }).ToList();

            return PagedResult<List<PublicMovieListDto>>.Success(
                result,
                request.Page,
                request.PageSize,
                totalCount
            );
        }
        catch (OperationCanceledException)
        {
            return PagedResult<List<PublicMovieListDto>>.Failure(Error.Failure(
                "MovieList.RequestCancelled",
                "Request was cancelled"
            ));
        }
    }
}
