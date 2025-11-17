using CineSocial.Application.Common.Results;
using MediatR;

namespace CineSocial.Application.Features.Users.Queries.GetUserProfile;

public record GetUserProfileQuery(Guid UserId) : IRequest<Result<UserProfileDto>>;

public record UserProfileDto
{
    public Guid UserId { get; init; }
    public string Username { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string? Bio { get; init; }

    // Image IDs (legacy/fallback)
    public Guid? ProfileImageId { get; init; }
    public Guid? BackgroundImageId { get; init; }

    // Direct cloud URLs for images (preferred - no extra API call needed)
    public string? ProfileImageUrl { get; init; }
    public string? BackgroundImageUrl { get; init; }

    public DateTime CreatedAt { get; init; }
    public DateTime? LastLoginAt { get; init; }
    public bool IsFollowing { get; init; }
    public bool IsBlocked { get; init; }
    public int FollowerCount { get; init; }
    public int FollowingCount { get; init; }
}
