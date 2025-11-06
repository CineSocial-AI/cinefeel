using CineSocial.Application.Common.Results;
using MediatR;

namespace CineSocial.Application.Features.Users.Queries.GetUsers;

public record GetUsersQuery(
    int Page = 1,
    int PageSize = 20,
    string? SearchTerm = null
) : IRequest<PagedResult<List<UserDto>>>;

public record UserDto
{
    public Guid UserId { get; init; }
    public string Username { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string? Bio { get; init; }
    public DateTime CreatedAt { get; init; }
    public bool IsFollowing { get; init; }
    public bool IsBlocked { get; init; }
    public int FollowerCount { get; init; }
    public int FollowingCount { get; init; }
}
