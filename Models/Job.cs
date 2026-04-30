namespace Aihrly.Models;

public class Job
{
    public Guid Id { get; set; }
    public string Title { get; set; } = null!;
    public string Description { get; set; } = null!;
    public string Location { get; set; } = null!;
    public JobStatus Status { get; set; } = JobStatus.Open;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public ICollection<Application> Applications { get; set; } = [];
}

public enum JobStatus
{
    Open,
    Closed
}
