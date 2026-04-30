using System.ComponentModel.DataAnnotations;

namespace Aihrly.DTOs;

public class SetScoreRequest
{
    [Required]
    [Range(1, 5)]
    public int Score { get; set; }

    public string? Comment { get; set; }
}

public record ScoreResponse(
    string Dimension,
    int Score,
    string? Comment,
    string SetByName,
    DateTime SetAt,
    string? UpdatedByName,
    DateTime? UpdatedAt
);
