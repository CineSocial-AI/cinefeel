using CineSocial.Domain.Common;
using CineSocial.Domain.Enums;

namespace CineSocial.Domain.Entities.Jobs;

public class JobExecutionHistory : BaseAuditableEntity
{
    /// <summary>
    /// Job name (e.g., "SendEmailVerificationJob")
    /// </summary>
    public string JobName { get; set; } = string.Empty;

    /// <summary>
    /// Job group (e.g., "Email", "Notification")
    /// </summary>
    public string JobGroup { get; set; } = string.Empty;

    /// <summary>
    /// Full job type name (e.g., "CineSocial.Infrastructure.Jobs.Email.SendEmailVerificationJob")
    /// </summary>
    public string JobType { get; set; } = string.Empty;

    /// <summary>
    /// Job parameters as JSON (e.g., {"email": "user@example.com", "token": "abc123"})
    /// </summary>
    public string? JobData { get; set; }

    /// <summary>
    /// Quartz job instance ID (fireInstanceId)
    /// </summary>
    public string? QuartzFireInstanceId { get; set; }

    /// <summary>
    /// Job execution status
    /// </summary>
    public JobExecutionStatus Status { get; set; } = JobExecutionStatus.Running;

    /// <summary>
    /// When the job started executing
    /// </summary>
    public DateTime StartedAt { get; set; }

    /// <summary>
    /// When the job completed (successfully or with error)
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// Job execution duration in milliseconds
    /// </summary>
    public long? DurationMs { get; set; }

    /// <summary>
    /// Error message if job failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Stack trace if job failed
    /// </summary>
    public string? StackTrace { get; set; }

    /// <summary>
    /// Current retry attempt (0 = first attempt)
    /// </summary>
    public int RetryCount { get; set; } = 0;

    /// <summary>
    /// Maximum retry attempts allowed
    /// </summary>
    public int MaxRetries { get; set; } = 3;

    /// <summary>
    /// When the job will be retried (if failed)
    /// </summary>
    public DateTime? NextRetryAt { get; set; }

    /// <summary>
    /// Job result data (optional, can store any result as JSON)
    /// </summary>
    public string? ResultData { get; set; }
}
