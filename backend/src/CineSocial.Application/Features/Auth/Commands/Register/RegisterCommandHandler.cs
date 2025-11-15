using CineSocial.Application.Common.Interfaces;
using CineSocial.Application.Common.Results;
using CineSocial.Application.Common.Security;
using CineSocial.Domain.Entities.User;
using CineSocial.Domain.Entities.Social;
using CineSocial.Domain.Enums;
using CineSocial.Infrastructure.Email;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CineSocial.Application.Features.Auth.Commands.Register;

public class RegisterCommandHandler : IRequestHandler<RegisterCommand, Result<AuthResponse>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IJwtService _jwtService;
    private readonly IEmailService _emailService;

    public RegisterCommandHandler(IUnitOfWork unitOfWork, IJwtService jwtService, IEmailService emailService)
    {
        _unitOfWork = unitOfWork;
        _jwtService = jwtService;
        _emailService = emailService;
    }

    public async Task<Result<AuthResponse>> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        // Check if username already exists
        var usernameExists = await _unitOfWork.Repository<AppUser>()
            .Query()
            .AnyAsync(u => u.Username.ToLower() == request.Username.ToLower(), cancellationToken);

        if (usernameExists)
        {
            return Result.Failure<AuthResponse>(Error.Conflict(
                "Auth.UsernameExists",
                "Username is already taken"
            ));
        }

        // Check if email already exists
        var emailExists = await _unitOfWork.Repository<AppUser>()
            .Query()
            .AnyAsync(u => u.Email.ToLower() == request.Email.ToLower(), cancellationToken);

        if (emailExists)
        {
            return Result.Failure<AuthResponse>(Error.Conflict(
                "Auth.EmailExists",
                "Email is already registered"
            ));
        }

        // Generate email verification token
        var verificationToken = Convert.ToBase64String(Guid.NewGuid().ToByteArray());

        // Create new user
        var user = new AppUser
        {
            Id = Guid.NewGuid(),
            Username = request.Username,
            Email = request.Email,
            PasswordHash = PasswordHasher.HashPassword(request.Password),
            Role = UserRole.User,
            IsActive = true,
            EmailConfirmed = false,
            EmailVerificationToken = verificationToken,
            EmailVerificationTokenExpiry = DateTime.UtcNow.AddHours(24),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _unitOfWork.Repository<AppUser>().AddAsync(user, cancellationToken);

        // Create default watchlist for the user
        var watchlist = new MovieList
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Name = "Watchlist",
            Description = "My default watchlist",
            IsPublic = false,
            IsWatchlist = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _unitOfWork.Repository<MovieList>().AddAsync(watchlist, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Send email verification
        try
        {
            await _emailService.SendEmailVerificationAsync(user.Email, user.Username, verificationToken, cancellationToken);
        }
        catch (Exception ex)
        {
            // Log the error but don't fail the registration
            // User is already created, we can resend the email later
            // TODO: Consider using background job for email sending
            Console.WriteLine($"Failed to send verification email: {ex.Message}");
        }

        // Generate JWT token
        var token = _jwtService.GenerateToken(user);

        var response = new AuthResponse
        {
            UserId = user.Id,
            Username = user.Username,
            Email = user.Email,
            Role = user.Role.ToString(),
            Token = token
        };

        return Result.Success(response);
    }
}
