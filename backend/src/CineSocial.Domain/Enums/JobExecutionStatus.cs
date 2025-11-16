namespace CineSocial.Domain.Enums;

public enum JobExecutionStatus
{
    /// <summary>
    /// Job is currently running
    /// </summary>
    Running = 0,

    /// <summary>
    /// Job completed successfully
    /// </summary>
    Success = 1,

    /// <summary>
    /// Job failed but will be retried
    /// </summary>
    FailedWillRetry = 2,

    /// <summary>
    /// Job failed permanently (no more retries)
    /// </summary>
    Failed = 3,

    /// <summary>
    /// Job was cancelled/vetoed
    /// </summary>
    Cancelled = 4
}
