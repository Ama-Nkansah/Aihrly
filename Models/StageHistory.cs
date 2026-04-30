namespace Aihrly.Models;

public class StageHistory
{
    public Guid Id { get; set; }
    public Guid ApplicationId { get; set; }
    public Application Application { get; set; } = null!;
    public ApplicationStage FromStage { get; set; }
    public ApplicationStage ToStage { get; set; }
    public Guid ChangedById { get; set; }
    public TeamMember ChangedBy { get; set; } = null!;
    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
    public string? Reason { get; set; }
}
