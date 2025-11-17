using CineSocial.Application.Common.Interfaces;
using CineSocial.Application.Common.Results;
using CineSocial.Application.Common.Security;
using CineSocial.Application.Features.Auth.Commands.Register;
using CineSocial.Domain.Entities.User;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CineSocial.Application.Features.Auth.Commands.Login;

public class LoginCommandHandler : IRequestHandler<LoginCommand, Result<AuthResponse>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IJwtService _jwtService;

    public LoginCommandHandler(IUnitOfWork unitOfWork, IJwtService jwtService)
    {
        _unitOfWork = unitOfWork;
        _jwtService = jwtService;
    }

    public async Task<Result<AuthResponse>> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        // Find user by username or email
        var user = await _unitOfWork.Repository<AppUser>()
            .Query()
            .FirstOrDefaultAsync(u =>
                u.Username.ToLower() == request.UsernameOrEmail.ToLower() ||
                u.Email.ToLower() == request.UsernameOrEmail.ToLower(),
                cancellationToken);

        if (user == null)
        {
            return Result.Failure<AuthResponse>(Error.Unauthorized(
                "Auth.InvalidCredentials",
                "Invalid username/email or password"
            ));
        }

        // Verify password
        if (!PasswordHasher.VerifyPassword(request.Password, user.PasswordHash))
        {
            return Result.Failure<AuthResponse>(Error.Unauthorized(
                "Auth.InvalidCredentials",
                "Invalid username/email or password"
            ));
        }

        // Check if user is active
        if (!user.IsActive)
        {
            return Result.Failure<AuthResponse>(Error.Forbidden(
                "Auth.UserInactive",
                "User account is inactive"
            ));
        }

        // Check if email is confirmed
        if (!user.EmailConfirmed)
        {
            return Result.Failure<AuthResponse>(Error.Forbidden(
                "Auth.EmailNotConfirmed",
                "Please verify your email address before logging in. Check your inbox for the verification email."
            ));
        }

        // Update last login
        user.LastLoginAt = DateTime.UtcNow;
        user.UpdatedAt = DateTime.UtcNow;
        _unitOfWork.Repository<AppUser>().Update(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

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
