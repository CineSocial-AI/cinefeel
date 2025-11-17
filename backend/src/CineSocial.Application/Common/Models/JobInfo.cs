namespace CineSocial.Application.Common.Models;

public class JobInfo
{
    public string JobId { get; set; } = string.Empty;
    public string JobName { get; set; } = string.Empty;
    public string JobGroup { get; set; } = string.Empty;
    public string JobType { get; set; } = string.Empty;
    public DateTimeOffset? NextFireTime { get; set; }
    public DateTimeOffset? PreviousFireTime { get; set; }
    public string State { get; set; } = string.Empty;
}
