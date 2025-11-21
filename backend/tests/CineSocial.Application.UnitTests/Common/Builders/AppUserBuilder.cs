using CineSocial.Domain.Entities.User;
using CineSocial.Domain.Enums;

namespace CineSocial.Application.UnitTests.Common.Builders;

/// <summary>
/// Builder for creating AppUser test data with a fluent API
/// </summary>
public class AppUserBuilder
{
    private Guid _id = Guid.NewGuid();
    private string _username = "testuser";
    private string _email = "test@example.com";
    private string _passwordHash = "hashed_password";
    private UserRole _role = UserRole.User;
    private bool _isActive = true;
    private bool _emailConfirmed = true;
    private DateTime? _lastLoginAt;
    private string? _bio;

    public AppUserBuilder WithId(Guid id)
    {
        _id = id;
        return this;
    }

    public AppUserBuilder WithUsername(string username)
    {
        _username = username;
        return this;
    }

    public AppUserBuilder WithEmail(string email)
    {
        _email = email;
        return this;
    }

    public AppUserBuilder WithPasswordHash(string passwordHash)
    {
        _passwordHash = passwordHash;
        return this;
    }

    public AppUserBuilder WithRole(UserRole role)
    {
        _role = role;
        return this;
    }

    public AppUserBuilder IsInactive()
    {
        _isActive = false;
        return this;
    }

    public AppUserBuilder EmailNotConfirmed()
    {
        _emailConfirmed = false;
        return this;
    }

    public AppUserBuilder WithLastLoginAt(DateTime lastLoginAt)
    {
        _lastLoginAt = lastLoginAt;
        return this;
    }

    public AppUserBuilder WithBio(string bio)
    {
        _bio = bio;
        return this;
    }

    public AppUser Build()
    {
        return new AppUser
        {
            Id = _id,
            Username = _username,
            Email = _email,
            PasswordHash = _passwordHash,
            Role = _role,
            IsActive = _isActive,
            EmailConfirmed = _emailConfirmed,
            LastLoginAt = _lastLoginAt,
            Bio = _bio,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }
}
