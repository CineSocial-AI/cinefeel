using CineSocial.Domain.Entities.Jobs;

namespace CineSocial.Infrastructure.Jobs.Services;

public interface IJobExecutionHistoryService
{
    /// <summary>
    /// Creates a new job execution history record
    /// </summary>
    Task<JobExecutionHistory> CreateHistoryAsync(JobExecutionHistory history, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing job execution history record
    /// </summary>
    Task UpdateHistoryAsync(JobExecutionHistory history, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets job execution history by ID
    /// </summary>
    Task<JobExecutionHistory?> GetHistoryByIdAsync(Guid historyId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets job execution history by Quartz fire instance ID
    /// </summary>
    Task<JobExecutionHistory?> GetHistoryByFireInstanceIdAsync(string fireInstanceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets recent job execution history for a specific job type
    /// </summary>
    Task<List<JobExecutionHistory>> GetRecentHistoryAsync(string jobName, int count = 50, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets failed jobs that need retry
    /// </summary>
    Task<List<JobExecutionHistory>> GetFailedJobsForRetryAsync(CancellationToken cancellationToken = default);
}
