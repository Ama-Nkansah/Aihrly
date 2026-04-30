namespace Aihrly.Models;

public class ApplicationNote
{
    public Guid Id { get; set; }
    public Guid ApplicationId { get; set; }
    public Application Application { get; set; } = null!;
    public NoteType Type { get; set; }
    public string Description { get; set; } = null!;
    public Guid CreatedById { get; set; }
    public TeamMember CreatedBy { get; set; } = null!;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public enum NoteType
{
    General,
    Screening,
    Interview,
    ReferenceCheck,
    RedFlag
}
