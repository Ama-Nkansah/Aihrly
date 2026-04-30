namespace Aihrly.Models;

public class TeamMember
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string Email { get; set; } = null!;
    public TeamMemberRole Role { get; set; }
}

public enum TeamMemberRole
{
    Recruiter,
    HiringManager
}
