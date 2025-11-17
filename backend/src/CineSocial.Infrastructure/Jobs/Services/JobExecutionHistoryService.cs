using CineSocial.Application.Common.Interfaces;
using CineSocial.Domain.Entities.Jobs;
using CineSocial.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CineSocial.Infrastructure.Jobs.Services;

public class JobExecutionHistoryService : IJobExecutionHistoryService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IRepository<JobExecutionHistory> _repository;

    public JobExecutionHistoryService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
        _repository = unitOfWork.Repository<JobExecutionHistory>();
    }

    public async Task<JobExecutionHistory> CreateHistoryAsync(JobExecutionHistory history, CancellationToken cancellationToken = default)
    {
        await _repository.AddAsync(history, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return history;
    }

    public async Task UpdateHistoryAsync(JobExecutionHistory history, CancellationToken cancellationToken = default)
    {
        history.UpdatedAt = DateTime.UtcNow;
        _repository.Update(history);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<JobExecutionHistory?> GetHistoryByIdAsync(Guid historyId, CancellationToken cancellationToken = default)
    {
        var result = await _repository.GetByIdAsync(historyId, cancellationToken);
        return result.IsSuccess ? result.Value : null;
    }

    public async Task<JobExecutionHistory?> GetHistoryByFireInstanceIdAsync(string fireInstanceId, CancellationToken cancellationToken = default)
    {
        return await _repository.Query()
            .FirstOrDefaultAsync(h => h.QuartzFireInstanceId == fireInstanceId, cancellationToken);
    }

    public async Task<List<JobExecutionHistory>> GetRecentHistoryAsync(string jobName, int count = 50, CancellationToken cancellationToken = default)
    {
        return await _repository.Query()
            .Where(h => h.JobName == jobName)
            .OrderByDescending(h => h.StartedAt)
            .Take(count)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<JobExecutionHistory>> GetFailedJobsForRetryAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        return await _repository.Query()
            .Where(h =>
                h.Status == JobExecutionStatus.FailedWillRetry &&
                h.NextRetryAt.HasValue &&
                h.NextRetryAt.Value <= now)
            .OrderBy(h => h.NextRetryAt)
            .ToListAsync(cancellationToken);
    }
}
