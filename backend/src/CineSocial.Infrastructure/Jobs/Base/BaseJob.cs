using CineSocial.Domain.Entities.Jobs;
using CineSocial.Domain.Enums;
using CineSocial.Infrastructure.Jobs.Services;
using Microsoft.Extensions.Logging;
using Quartz;
using System.Diagnostics;
using System.Text.Json;

namespace CineSocial.Infrastructure.Jobs.Base;

/// <summary>
/// Base class for all Quartz jobs with automatic execution history tracking
/// </summary>
public abstract class BaseJob : IJob
{
    private readonly IJobExecutionHistoryService _historyService;
    private readonly ILogger<BaseJob> _logger;

    protected BaseJob(IJobExecutionHistoryService historyService, ILogger<BaseJob> logger)
    {
        _historyService = historyService;
        _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        var jobHistory = await CreateJobHistory(context);

        try
        {
            _logger.LogInformation(
                "Starting job {JobName} (Group: {JobGroup}, Instance: {FireInstanceId})",
                context.JobDetail.Key.Name,
                context.JobDetail.Key.Group,
                context.FireInstanceId
            );

            // Execute the actual job logic
            await ExecuteJob(context);

            stopwatch.Stop();

            // Mark as successful
            jobHistory.Status = JobExecutionStatus.Success;
            jobHistory.CompletedAt = DateTime.UtcNow;
            jobHistory.DurationMs = stopwatch.ElapsedMilliseconds;

            await _historyService.UpdateHistoryAsync(jobHistory);

            _logger.LogInformation(
                "Job {JobName} completed successfully in {Duration}ms",
                context.JobDetail.Key.Name,
                stopwatch.ElapsedMilliseconds
            );
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            _logger.LogError(
                ex,
                "Job {JobName} failed after {Duration}ms: {ErrorMessage}",
                context.JobDetail.Key.Name,
                stopwatch.ElapsedMilliseconds,
                ex.Message
            );

            // Determine retry status
            var retryCount = context.JobDetail.JobDataMap.GetIntValue("RetryCount");
            var maxRetries = context.JobDetail.JobDataMap.GetIntValue("MaxRetries");

            if (maxRetries == 0)
            {
                maxRetries = 3; // Default
            }

            bool willRetry = retryCount < maxRetries;

            jobHistory.Status = willRetry ? JobExecutionStatus.FailedWillRetry : JobExecutionStatus.Failed;
            jobHistory.CompletedAt = DateTime.UtcNow;
            jobHistory.DurationMs = stopwatch.ElapsedMilliseconds;
            jobHistory.ErrorMessage = ex.Message;
            jobHistory.StackTrace = ex.StackTrace;
            jobHistory.RetryCount = retryCount;
            jobHistory.MaxRetries = maxRetries;

            if (willRetry)
            {
                // Exponential backoff: 1min, 5min, 15min
                var delayMinutes = Math.Pow(5, retryCount);
                jobHistory.NextRetryAt = DateTime.UtcNow.AddMinutes(delayMinutes);
            }

            await _historyService.UpdateHistoryAsync(jobHistory);

            // Rethrow to let Quartz handle retry
            if (willRetry)
            {
                throw new JobExecutionException(ex, refireImmediately: false);
            }
            else
            {
                throw;
            }
        }
    }

    /// <summary>
    /// Override this method to implement your job logic
    /// </summary>
    protected abstract Task ExecuteJob(IJobExecutionContext context);

    private async Task<JobExecutionHistory> CreateJobHistory(IJobExecutionContext context)
    {
        var jobData = new Dictionary<string, object?>();
        foreach (var key in context.JobDetail.JobDataMap.Keys)
        {
            jobData[key] = context.JobDetail.JobDataMap[key];
        }

        var history = new JobExecutionHistory
        {
            Id = Guid.NewGuid(),
            JobName = context.JobDetail.Key.Name,
            JobGroup = context.JobDetail.Key.Group,
            JobType = context.JobDetail.JobType.FullName ?? "Unknown",
            JobData = JsonSerializer.Serialize(jobData),
            QuartzFireInstanceId = context.FireInstanceId,
            Status = JobExecutionStatus.Running,
            StartedAt = DateTime.UtcNow,
            RetryCount = context.JobDetail.JobDataMap.GetIntValue("RetryCount"),
            MaxRetries = context.JobDetail.JobDataMap.GetIntValue("MaxRetries"),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        return await _historyService.CreateHistoryAsync(history);
    }
}
