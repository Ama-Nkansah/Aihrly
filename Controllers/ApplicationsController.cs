using Aihrly.Data;
using Aihrly.DTOs;
using Aihrly.Models;
using Aihrly.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Aihrly.Controllers;

[Route("api/applications")]
public class ApplicationsController : BaseApiController
{
    private readonly AppDbContext _db;

    public ApplicationsController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetApplication(Guid id)
    {
        var application = await _db.Applications
            .Include(a => a.Job)
            .Include(a => a.Notes)
                .ThenInclude(n => n.CreatedBy)
            .Include(a => a.StageHistories)
                .ThenInclude(h => h.ChangedBy)
            .Include(a => a.Scores)
                .ThenInclude(s => s.SetBy)
            .Include(a => a.Scores)
                .ThenInclude(s => s.UpdatedBy)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (application is null)
            return NotFound(new ProblemDetails { Title = $"Application {id} not found", Status = 404 });

        var response = new ApplicationProfileResponse(
            Id: application.Id,
            JobId: application.JobId,
            JobTitle: application.Job.Title,
            CandidateName: application.CandidateName,
            CandidateEmail: application.CandidateEmail,
            CoverLetter: application.CoverLetter,
            Stage: application.Stage.ToString(),
            AppliedAt: application.AppliedAt,
            Scores: application.Scores.Select(s => new ScoreResponse(
                Dimension: s.Dimension.ToString(),
                Score: s.Score,
                Comment: s.Comment,
                SetByName: s.SetBy.Name,
                SetAt: s.SetAt,
                UpdatedByName: s.UpdatedBy?.Name,
                UpdatedAt: s.UpdatedAt
            )).ToList(),
            Notes: application.Notes
                .OrderByDescending(n => n.CreatedAt)
                .Select(n => new NoteResponse(
                    Id: n.Id,
                    Type: n.Type.ToString(),
                    Description: n.Description,
                    AuthorName: n.CreatedBy.Name,
                    CreatedAt: n.CreatedAt
                )).ToList(),
            StageHistory: application.StageHistories
                .OrderBy(h => h.ChangedAt)
                .Select(h => new StageHistoryResponse(
                    FromStage: h.FromStage.ToString(),
                    ToStage: h.ToStage.ToString(),
                    ChangedByName: h.ChangedBy.Name,
                    ChangedAt: h.ChangedAt,
                    Reason: h.Reason
                )).ToList()
        );

        return Ok(response);
    }

    [HttpPatch("{id:guid}/stage")]
    public async Task<IActionResult> MoveStage(Guid id, [FromBody] MoveStageRequest request)
    {
        var (memberId, error) = await ResolveTeamMemberAsync(_db);
        if (error is not null) return error;

        var application = await _db.Applications.FindAsync(id);
        if (application is null)
            return NotFound(new ProblemDetails { Title = $"Application {id} not found", Status = 404 });

        if (StageTransitions.IsTerminal(application.Stage))
            return BadRequest(new ProblemDetails
            {
                Title = $"Application is already in a terminal stage: {application.Stage}",
                Status = 400
            });

        if (!StageTransitions.IsValid(application.Stage, request.TargetStage))
            return BadRequest(new ProblemDetails
            {
                Title = $"Invalid transition from {application.Stage} to {request.TargetStage}",
                Status = 400
            });

        var history = new StageHistory
        {
            Id = Guid.NewGuid(),
            ApplicationId = application.Id,
            FromStage = application.Stage,
            ToStage = request.TargetStage,
            ChangedById = memberId!.Value,
            ChangedAt = DateTime.UtcNow,
            Reason = request.Reason
        };

        application.Stage = request.TargetStage;

        await _db.StageHistories.AddAsync(history);
        await _db.SaveChangesAsync();

        return Ok(new { message = $"Stage moved to {request.TargetStage}", stage = request.TargetStage.ToString() });
    }

    [HttpPost("{id:guid}/notes")]
    public async Task<IActionResult> AddNote(Guid id, [FromBody] CreateNoteRequest request)
    {
        var (memberId, error) = await ResolveTeamMemberAsync(_db);
        if (error is not null) return error;

        var application = await _db.Applications.FindAsync(id);
        if (application is null)
            return NotFound(new ProblemDetails { Title = $"Application {id} not found", Status = 404 });

        var note = new ApplicationNote
        {
            Id = Guid.NewGuid(),
            ApplicationId = id,
            Type = request.Type,
            Description = request.Description,
            CreatedById = memberId!.Value,
            CreatedAt = DateTime.UtcNow
        };

        await _db.ApplicationNotes.AddAsync(note);
        await _db.SaveChangesAsync();

        await _db.Entry(note).Reference(n => n.CreatedBy).LoadAsync();

        return CreatedAtAction(nameof(GetNotes), new { id }, new NoteResponse(
            Id: note.Id,
            Type: note.Type.ToString(),
            Description: note.Description,
            AuthorName: note.CreatedBy.Name,
            CreatedAt: note.CreatedAt
        ));
    }

    [HttpGet("{id:guid}/notes")]
    public async Task<IActionResult> GetNotes(Guid id)
    {
        var applicationExists = await _db.Applications.AnyAsync(a => a.Id == id);
        if (!applicationExists)
            return NotFound(new ProblemDetails { Title = $"Application {id} not found", Status = 404 });

        var notes = await _db.ApplicationNotes
            .Include(n => n.CreatedBy)
            .Where(n => n.ApplicationId == id)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync();

        var response = notes.Select(n => new NoteResponse(
            Id: n.Id,
            Type: n.Type.ToString(),
            Description: n.Description,
            AuthorName: n.CreatedBy.Name,
            CreatedAt: n.CreatedAt
        )).ToList();

        return Ok(response);
    }

    [HttpPut("{id:guid}/scores/culture-fit")]
    public async Task<IActionResult> SetCultureFitScore(Guid id, [FromBody] SetScoreRequest request) =>
        await SetScore(id, ScoreDimension.CultureFit, request);

    [HttpPut("{id:guid}/scores/interview")]
    public async Task<IActionResult> SetInterviewScore(Guid id, [FromBody] SetScoreRequest request) =>
        await SetScore(id, ScoreDimension.Interview, request);

    [HttpPut("{id:guid}/scores/assessment")]
    public async Task<IActionResult> SetAssessmentScore(Guid id, [FromBody] SetScoreRequest request) =>
        await SetScore(id, ScoreDimension.Assessment, request);

    private async Task<IActionResult> SetScore(Guid applicationId, ScoreDimension dimension, SetScoreRequest request)
    {
        var (memberId, error) = await ResolveTeamMemberAsync(_db);
        if (error is not null) return error;

        var application = await _db.Applications.FindAsync(applicationId);
        if (application is null)
            return NotFound(new ProblemDetails { Title = $"Application {applicationId} not found", Status = 404 });

        var existing = await _db.ApplicationScores
            .FirstOrDefaultAsync(s => s.ApplicationId == applicationId && s.Dimension == dimension);

        if (existing is null)
        {
            var score = new ApplicationScore
            {
                Id = Guid.NewGuid(),
                ApplicationId = applicationId,
                Dimension = dimension,
                Score = request.Score,
                Comment = request.Comment,
                SetById = memberId!.Value,
                SetAt = DateTime.UtcNow
            };
            await _db.ApplicationScores.AddAsync(score);
        }
        else
        {
            existing.Score = request.Score;
            existing.Comment = request.Comment;
            existing.UpdatedById = memberId!.Value;
            existing.UpdatedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();
        return Ok(new { message = $"{dimension} score saved", score = request.Score });
    }
}
