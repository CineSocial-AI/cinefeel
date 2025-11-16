using CineSocial.Application.Common.Interfaces;
using CineSocial.Application.Common.Models;
using Microsoft.Extensions.Logging;
using Quartz;

namespace CineSocial.Infrastructure.Jobs.Services;

public class JobSchedulerService : IJobSchedulerService
{
    private readonly ISchedulerFactory _schedulerFactory;
    private readonly ILogger<JobSchedulerService> _logger;

    public JobSchedulerService(ISchedulerFactory schedulerFactory, ILogger<JobSchedulerService> logger)
    {
        _schedulerFactory = schedulerFactory;
        _logger = logger;
    }

    public async Task<string> ScheduleEmailVerificationJobAsync(
        string email,
        string username,
        string token,
        CancellationToken cancellationToken = default)
    {
        var jobData = new Dictionary<string, object>
        {
            { "Email", email },
            { "Username", username },
            { "Token", token }
        };

        return await ScheduleJobAsync<Email.SendEmailVerificationJob>(
            jobData,
            delaySeconds: 0, // Immediate
            maxRetries: 3,
            cancellationToken
        );
    }

    public async Task<string> ScheduleJobAsync<TJob>(
        IDictionary<string, object> jobData,
        int delaySeconds = 0,
        int maxRetries = 3,
        CancellationToken cancellationToken = default) where TJob : IJob
    {
        var scheduler = await _schedulerFactory.GetScheduler(cancellationToken);

        // Create unique job ID
        var jobId = Guid.NewGuid().ToString("N");
        var jobKey = new JobKey(jobId, typeof(TJob).Name);

        // Add retry info to job data
        jobData["RetryCount"] = 0;
        jobData["MaxRetries"] = maxRetries;

        // Build job
        var job = JobBuilder.Create<TJob>()
            .WithIdentity(jobKey)
            .UsingJobData(new JobDataMap(jobData))
            .StoreDurably(false)
            .Build();

        // Build trigger
        var triggerBuilder = TriggerBuilder.Create()
            .WithIdentity($"{jobId}-trigger", typeof(TJob).Name)
            .ForJob(jobKey);

        if (delaySeconds > 0)
        {
            triggerBuilder.StartAt(DateTimeOffset.UtcNow.AddSeconds(delaySeconds));
        }
        else
        {
            triggerBuilder.StartNow();
        }

        var trigger = triggerBuilder.Build();

        // Schedule job
        await scheduler.ScheduleJob(job, trigger, cancellationToken);

        _logger.LogInformation(
            "Scheduled job {JobType} with ID {JobId}, delay: {Delay}s, max retries: {MaxRetries}",
            typeof(TJob).Name,
            jobId,
            delaySeconds,
            maxRetries
        );

        return jobId;
    }

    public async Task<bool> CancelJobAsync(string jobId, CancellationToken cancellationToken = default)
    {
        var scheduler = await _schedulerFactory.GetScheduler(cancellationToken);

        // Try to find the job in all groups
        var allJobs = await scheduler.GetJobKeys(Quartz.Impl.Matchers.GroupMatcher<JobKey>.AnyGroup(), cancellationToken);
        var jobKey = allJobs.FirstOrDefault(k => k.Name == jobId);

        if (jobKey == null)
        {
            _logger.LogWarning("Job {JobId} not found for cancellation", jobId);
            return false;
        }

        var result = await scheduler.DeleteJob(jobKey, cancellationToken);

        if (result)
        {
            _logger.LogInformation("Job {JobId} cancelled successfully", jobId);
        }
        else
        {
            _logger.LogWarning("Failed to cancel job {JobId}", jobId);
        }

        return result;
    }

    public async Task<JobInfo?> GetJobInfoAsync(string jobId, CancellationToken cancellationToken = default)
    {
        var scheduler = await _schedulerFactory.GetScheduler(cancellationToken);

        var allJobs = await scheduler.GetJobKeys(Quartz.Impl.Matchers.GroupMatcher<JobKey>.AnyGroup(), cancellationToken);
        var jobKey = allJobs.FirstOrDefault(k => k.Name == jobId);

        if (jobKey == null)
        {
            return null;
        }

        var jobDetail = await scheduler.GetJobDetail(jobKey, cancellationToken);
        if (jobDetail == null)
        {
            return null;
        }

        var triggers = await scheduler.GetTriggersOfJob(jobKey, cancellationToken);
        var trigger = triggers.FirstOrDefault();

        var triggerState = trigger != null
            ? await scheduler.GetTriggerState(trigger.Key, cancellationToken)
            : TriggerState.None;

        return new JobInfo
        {
            JobId = jobId,
            JobName = jobKey.Name,
            JobGroup = jobKey.Group,
            JobType = jobDetail.JobType.Name,
            NextFireTime = trigger?.GetNextFireTimeUtc(),
            PreviousFireTime = trigger?.GetPreviousFireTimeUtc(),
            State = triggerState.ToString()
        };
    }
}
