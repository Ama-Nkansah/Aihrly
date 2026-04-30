namespace Aihrly.Models;

public class Application
{
    public Guid Id { get; set; }
    public Guid JobId { get; set; }
    public Job Job { get; set; } = null!;
    public string CandidateName { get; set; } = null!;
    public string CandidateEmail { get; set; } = null!;
    public string? CoverLetter { get; set; }
    public ApplicationStage Stage { get; set; } = ApplicationStage.Applied;
    public DateTime AppliedAt { get; set; } = DateTime.UtcNow;
    public ICollection<ApplicationNote> Notes { get; set; } = [];
    public ICollection<StageHistory> StageHistories { get; set; } = [];
    public ICollection<ApplicationScore> Scores { get; set; } = [];
}

public enum ApplicationStage
{
    Applied,
    Screening,
    Interview,
    Offer,
    Hired,
    Rejected
}
