namespace CineSocial.Infrastructure.Email;

public interface IEmailService
{
    /// <summary>
    /// Sends an email asynchronously
    /// </summary>
    /// <param name="to">Recipient email address</param>
    /// <param name="subject">Email subject</param>
    /// <param name="body">Email body (HTML supported)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task SendEmailAsync(string to, string subject, string body, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends email verification message to user
    /// </summary>
    /// <param name="email">User email</param>
    /// <param name="username">User username</param>
    /// <param name="token">Verification token</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task SendEmailVerificationAsync(string email, string username, string token, CancellationToken cancellationToken = default);
}
