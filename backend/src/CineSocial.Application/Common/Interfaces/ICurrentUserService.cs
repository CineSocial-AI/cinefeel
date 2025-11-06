namespace CineSocial.Application.Common.Interfaces;

/// <summary>
/// Service for accessing current authenticated user information
/// </summary>
public interface ICurrentUserService
{
    /// <summary>
    /// Gets the current user's ID
    /// </summary>
    Guid? UserId { get; }

    /// <summary>
    /// Gets the current user's username
    /// </summary>
    string? Username { get; }

    /// <summary>
    /// Gets whether the user is authenticated
    /// </summary>
    bool IsAuthenticated { get; }
}
