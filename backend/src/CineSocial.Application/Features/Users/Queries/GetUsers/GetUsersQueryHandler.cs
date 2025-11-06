using CineSocial.Application.Common.Interfaces;
using CineSocial.Application.Common.Results;
using CineSocial.Domain.Entities.User;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CineSocial.Application.Features.Users.Queries.GetUsers;

public class GetUsersQueryHandler : IRequestHandler<GetUsersQuery, PagedResult<List<UserDto>>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public GetUsersQueryHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<PagedResult<List<UserDto>>> Handle(GetUsersQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var currentUserId = _currentUserService.UserId;

            var query = _unitOfWork.Repository<AppUser>()
                .Query()
                .Where(u => u.IsActive);

            // Exclude current user from list
            if (currentUserId.HasValue)
            {
                query = query.Where(u => u.Id != currentUserId.Value);
            }

            // Search filter
            if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            {
                var searchLower = request.SearchTerm.ToLower();
                query = query.Where(u => u.Username.ToLower().Contains(searchLower) ||
                                        u.Email.ToLower().Contains(searchLower));
            }

            // Order by username
            query = query.OrderBy(u => u.Username);

            var totalCount = await query.CountAsync(cancellationToken);

            var users = await query
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(u => new
                {
                    User = u,
                    FollowerCount = u.Followers.Count,
                    FollowingCount = u.Following.Count
                })
                .ToListAsync(cancellationToken);

            // Get follow and block status for current user
            List<Guid> followingIds = new();
            List<Guid> blockedIds = new();

            if (currentUserId.HasValue)
            {
                followingIds = await _unitOfWork.Repository<Follow>()
                    .Query()
                    .Where(f => f.FollowerId == currentUserId.Value)
                    .Select(f => f.FollowingId)
                    .ToListAsync(cancellationToken);

                blockedIds = await _unitOfWork.Repository<Block>()
                    .Query()
                    .Where(b => b.BlockerId == currentUserId.Value)
                    .Select(b => b.BlockedUserId)
                    .ToListAsync(cancellationToken);
            }

            var result = users.Select(u => new UserDto
            {
                UserId = u.User.Id,
                Username = u.User.Username,
                Email = u.User.Email,
                Bio = u.User.Bio,
                CreatedAt = u.User.CreatedAt,
                IsFollowing = followingIds.Contains(u.User.Id),
                IsBlocked = blockedIds.Contains(u.User.Id),
                FollowerCount = u.FollowerCount,
                FollowingCount = u.FollowingCount
            }).ToList();

            return PagedResult<List<UserDto>>.Success(
                result,
                request.Page,
                request.PageSize,
                totalCount
            );
        }
        catch (OperationCanceledException)
        {
            return PagedResult<List<UserDto>>.Failure(Error.Failure(
                "Users.RequestCancelled",
                "Request was cancelled"
            ));
        }
    }
}
