using CineSocial.Infrastructure.Email;
using CineSocial.Infrastructure.Jobs.Base;
using CineSocial.Infrastructure.Jobs.Services;
using Microsoft.Extensions.Logging;
using Quartz;

namespace CineSocial.Infrastructure.Jobs.Email;

/// <summary>
/// Job for sending email verification emails
/// </summary>
[DisallowConcurrentExecution]
public class SendEmailVerificationJob : BaseJob
{
    private readonly IEmailService _emailService;
    private readonly ILogger<SendEmailVerificationJob> _logger;

    public SendEmailVerificationJob(
        IEmailService emailService,
        IJobExecutionHistoryService historyService,
        ILogger<SendEmailVerificationJob> logger)
        : base(historyService, logger)
    {
        _emailService = emailService;
        _logger = logger;
    }

    protected override async Task ExecuteJob(IJobExecutionContext context)
    {
        var jobData = context.JobDetail.JobDataMap;

        // Extract parameters
        var email = jobData.GetString("Email");
        var username = jobData.GetString("Username");
        var token = jobData.GetString("Token");

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(username) || string.IsNullOrEmpty(token))
        {
            throw new ArgumentException("Email, Username, and Token are required parameters");
        }

        _logger.LogInformation(
            "Sending email verification to {Email} for user {Username}",
            email,
            username
        );

        // Send the email
        await _emailService.SendEmailVerificationAsync(email, username, token, context.CancellationToken);

        _logger.LogInformation(
            "Email verification sent successfully to {Email}",
            email
        );
    }
}
