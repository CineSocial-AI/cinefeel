using CineSocial.Application.Common.Interfaces;
using CineSocial.Application.Common.Models;
using CineSocial.Domain.Entities.User;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CineSocial.Application.Features.Auth.Commands.VerifyEmail;

public class VerifyEmailCommandHandler : IRequestHandler<VerifyEmailCommand, Result<string>>
{
    private readonly IApplicationDbContext _context;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<VerifyEmailCommandHandler> _logger;

    public VerifyEmailCommandHandler(
        IApplicationDbContext context,
        IUnitOfWork unitOfWork,
        ILogger<VerifyEmailCommandHandler> logger)
    {
        _context = context;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<string>> Handle(VerifyEmailCommand request, CancellationToken cancellationToken)
    {
        // Find user with the verification token
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.EmailVerificationToken == request.Token, cancellationToken);

        if (user == null)
        {
            _logger.LogWarning("Invalid email verification token: {Token}", request.Token);
            return Result.Failure<string>("Geçersiz doğrulama linki");
        }

        // Check if token is expired
        if (user.EmailVerificationTokenExpiry.HasValue &&
            user.EmailVerificationTokenExpiry.Value < DateTime.UtcNow)
        {
            _logger.LogWarning("Expired email verification token for user {UserId}", user.Id);
            return Result.Failure<string>("Doğrulama linki süresi dolmuş. Lütfen yeni bir doğrulama maili isteyin.");
        }

        // Check if already verified
        if (user.EmailConfirmed)
        {
            _logger.LogInformation("User {UserId} email already verified", user.Id);
            return Result.Success("Email adresi zaten doğrulanmış");
        }

        // Mark email as confirmed
        user.EmailConfirmed = true;
        user.EmailVerificationToken = null;
        user.EmailVerificationTokenExpiry = null;
        user.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Email verified successfully for user {UserId}", user.Id);

        return Result.Success("Email adresiniz başarıyla doğrulandı! Artık giriş yapabilirsiniz.");
    }
}
