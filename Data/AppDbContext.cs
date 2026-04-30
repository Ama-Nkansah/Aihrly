using Aihrly.Models;
using Microsoft.EntityFrameworkCore;

namespace Aihrly.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<TeamMember> TeamMembers => Set<TeamMember>();
    public DbSet<Job> Jobs => Set<Job>();
    public DbSet<Application> Applications => Set<Application>();
    public DbSet<ApplicationNote> ApplicationNotes => Set<ApplicationNote>();
    public DbSet<StageHistory> StageHistories => Set<StageHistory>();
    public DbSet<ApplicationScore> ApplicationScores => Set<ApplicationScore>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        SeedTeamMembers(modelBuilder);
        ConfigureIndexes(modelBuilder);
        ConfigureEnums(modelBuilder);
    }

    private static void SeedTeamMembers(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TeamMember>().HasData(
            new TeamMember
            {
                Id = new Guid("11111111-1111-1111-1111-111111111111"),
                Name = "Alice Johnson",
                Email = "alice@aihrly.com",
                Role = TeamMemberRole.Recruiter
            },
            new TeamMember
            {
                Id = new Guid("22222222-2222-2222-2222-222222222222"),
                Name = "Bob Smith",
                Email = "bob@aihrly.com",
                Role = TeamMemberRole.HiringManager
            },
            new TeamMember
            {
                Id = new Guid("33333333-3333-3333-3333-333333333333"),
                Name = "Carol White",
                Email = "carol@aihrly.com",
                Role = TeamMemberRole.Recruiter
            }
        );
    }

    private static void ConfigureIndexes(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Application>()
            .HasIndex(a => new { a.JobId, a.CandidateEmail })
            .IsUnique();

        modelBuilder.Entity<ApplicationScore>()
            .HasIndex(s => new { s.ApplicationId, s.Dimension })
            .IsUnique();

        modelBuilder.Entity<ApplicationNote>()
            .HasIndex(n => n.ApplicationId);

        modelBuilder.Entity<StageHistory>()
            .HasIndex(s => s.ApplicationId);

        modelBuilder.Entity<Application>()
            .HasIndex(a => a.JobId);
    }

    private static void ConfigureEnums(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TeamMember>()
            .Property(t => t.Role)
            .HasConversion<string>();

        modelBuilder.Entity<Job>()
            .Property(j => j.Status)
            .HasConversion<string>();

        modelBuilder.Entity<Application>()
            .Property(a => a.Stage)
            .HasConversion<string>();

        modelBuilder.Entity<ApplicationNote>()
            .Property(n => n.Type)
            .HasConversion<string>();

        modelBuilder.Entity<StageHistory>()
            .Property(s => s.FromStage)
            .HasConversion<string>();

        modelBuilder.Entity<StageHistory>()
            .Property(s => s.ToStage)
            .HasConversion<string>();

        modelBuilder.Entity<ApplicationScore>()
            .Property(s => s.Dimension)
            .HasConversion<string>();
    }
}
