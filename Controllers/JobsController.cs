using Aihrly.Data;
using Aihrly.DTOs;
using Aihrly.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Aihrly.Controllers;

[Route("api/jobs")]
public class JobsController : BaseApiController
{
    private readonly AppDbContext _db;

    public JobsController(AppDbContext db)
    {
        _db = db;
    }

    [HttpPost]
    public async Task<IActionResult> CreateJob([FromBody] CreateJobRequest request)
    {
        var job = new Job
        {
            Id = Guid.NewGuid(),
            Title = request.Title,
            Description = request.Description,
            Location = request.Location,
            Status = JobStatus.Open,
            CreatedAt = DateTime.UtcNow
        };

        await _db.Jobs.AddAsync(job);
        await _db.SaveChangesAsync();

        var response = MapToJobResponse(job);
        return CreatedAtAction(nameof(GetJob), new { id = job.Id }, response);
    }

    [HttpGet]
    public async Task<IActionResult> GetJobs(
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var query = _db.Jobs.AsQueryable();

        if (!string.IsNullOrEmpty(status) &&
            Enum.TryParse<JobStatus>(status, ignoreCase: true, out var parsedStatus))
        {
            query = query.Where(j => j.Status == parsedStatus);
        }

        var totalCount = await query.CountAsync();

        var jobs = await query
            .OrderByDescending(j => j.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var response = new PagedResult<JobResponse>(
            Items: jobs.Select(MapToJobResponse).ToList(),
            Page: page,
            PageSize: pageSize,
            TotalCount: totalCount
        );

        return Ok(response);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetJob(Guid id)
    {
        var job = await _db.Jobs.FindAsync(id);
        if (job is null)
            return NotFound(new ProblemDetails { Title = $"Job {id} not found", Status = 404 });

        return Ok(MapToJobResponse(job));
    }

    [HttpPost("{jobId:guid}/applications")]
    public async Task<IActionResult> CreateApplication(
        Guid jobId,
        [FromBody] CreateApplicationRequest request)
    {
        var job = await _db.Jobs.FindAsync(jobId);
        if (job is null)
            return NotFound(new ProblemDetails { Title = $"Job {jobId} not found", Status = 404 });

        var duplicate = await _db.Applications
            .AnyAsync(a => a.JobId == jobId && a.CandidateEmail == request.CandidateEmail);

        if (duplicate)
            return Conflict(new ProblemDetails
            {
                Title = "This email has already applied to this job",
                Status = 409
            });

        var application = new Application
        {
            Id = Guid.NewGuid(),
            JobId = jobId,
            CandidateName = request.CandidateName,
            CandidateEmail = request.CandidateEmail,
            CoverLetter = request.CoverLetter,
            Stage = ApplicationStage.Applied,
            AppliedAt = DateTime.UtcNow
        };

        await _db.Applications.AddAsync(application);
        await _db.SaveChangesAsync();

        var response = new ApplicationListResponse(
            Id: application.Id,
            CandidateName: application.CandidateName,
            CandidateEmail: application.CandidateEmail,
            Stage: application.Stage.ToString(),
            AppliedAt: application.AppliedAt
        );

        return CreatedAtAction("GetApplication", "Applications", new { id = application.Id }, response);
    }

    [HttpGet("{jobId:guid}/applications")]
    public async Task<IActionResult> GetApplications(
        Guid jobId,
        [FromQuery] string? stage,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var jobExists = await _db.Jobs.AnyAsync(j => j.Id == jobId);
        if (!jobExists)
            return NotFound(new ProblemDetails { Title = $"Job {jobId} not found", Status = 404 });

        var query = _db.Applications.Where(a => a.JobId == jobId);

        if (!string.IsNullOrEmpty(stage) &&
            Enum.TryParse<ApplicationStage>(stage, ignoreCase: true, out var parsedStage))
        {
            query = query.Where(a => a.Stage == parsedStage);
        }

        var totalCount = await query.CountAsync();

        var applications = await query
            .OrderByDescending(a => a.AppliedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var response = new PagedResult<ApplicationListResponse>(
            Items: applications.Select(a => new ApplicationListResponse(
                Id: a.Id,
                CandidateName: a.CandidateName,
                CandidateEmail: a.CandidateEmail,
                Stage: a.Stage.ToString(),
                AppliedAt: a.AppliedAt
            )).ToList(),
            Page: page,
            PageSize: pageSize,
            TotalCount: totalCount
        );

        return Ok(response);
    }

    private static JobResponse MapToJobResponse(Job job) => new(
        Id: job.Id,
        Title: job.Title,
        Description: job.Description,
        Location: job.Location,
        Status: job.Status.ToString(),
        CreatedAt: job.CreatedAt
    );
}
