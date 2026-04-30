using System.ComponentModel.DataAnnotations;

namespace Aihrly.DTOs;

public class CreateApplicationRequest
{
    [Required]
    public string CandidateName { get; set; } = null!;

    [Required]
    [EmailAddress]
    public string CandidateEmail { get; set; } = null!;

    public string? CoverLetter { get; set; }
}

public record ApplicationListResponse(
    Guid Id,
    string CandidateName,
    string CandidateEmail,
    string Stage,
    DateTime AppliedAt
);

public record ApplicationProfileResponse(
    Guid Id,
    Guid JobId,
    string JobTitle,
    string CandidateName,
    string CandidateEmail,
    string? CoverLetter,
    string Stage,
    DateTime AppliedAt,
    IReadOnlyList<ScoreResponse> Scores,
    IReadOnlyList<NoteResponse> Notes,
    IReadOnlyList<StageHistoryResponse> StageHistory
);
