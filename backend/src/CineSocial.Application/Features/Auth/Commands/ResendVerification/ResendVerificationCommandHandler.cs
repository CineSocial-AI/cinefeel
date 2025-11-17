using CineSocial.Application.Common.Interfaces;
using CineSocial.Application.Common.Results;
using CineSocial.Domain.Entities.User;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CineSocial.Application.Features.Auth.Commands.ResendVerification;

public class ResendVerificationCommandHandler : IRequestHandler<ResendVerificationCommand, Result<Unit>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IJobSchedulerService _jobScheduler;

    public ResendVerificationCommandHandler(IUnitOfWork unitOfWork, IJobSchedulerService jobScheduler)
    {
        _unitOfWork = unitOfWork;
        _jobScheduler = jobScheduler;
    }

    public async Task<Result<Unit>> Handle(ResendVerificationCommand request, CancellationToken cancellationToken)
    {
        var user = await _unitOfWork.Repository<AppUser>()
            .Query()
            .FirstOrDefaultAsync(u => u.Email.ToLower() == request.Email.ToLower(), cancellationToken);

        if (user == null || user.EmailConfirmed)
        {
            return Result.Success(Unit.Value);
        }

        var verificationToken = Convert.ToBase64String(Guid.NewGuid().ToByteArray());
        user.EmailVerificationToken = verificationToken;
        user.EmailVerificationTokenExpiry = DateTime.UtcNow.AddHours(24);
        user.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.Repository<AppUser>().Update(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _jobScheduler.ScheduleEmailVerificationJobAsync(
            user.Email,
            user.Username,
            verificationToken,
            cancellationToken
        );

        return Result.Success(Unit.Value);
    }
}
