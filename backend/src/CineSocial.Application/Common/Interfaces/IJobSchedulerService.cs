namespace CineSocial.Application.Common.Interfaces;

public interface IJobSchedulerService
{
    /// <summary>
    /// Schedules an email verification job
    /// </summary>
    Task<string> ScheduleEmailVerificationJobAsync(
        string email,
        string username,
        string token,
        CancellationToken cancellationToken = default);
}
