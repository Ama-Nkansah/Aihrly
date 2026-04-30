using System.ComponentModel.DataAnnotations;
using Aihrly.Models;

namespace Aihrly.DTOs;

public class MoveStageRequest
{
    [Required]
    public ApplicationStage TargetStage { get; set; }

    public string? Reason { get; set; }
}

public record StageHistoryResponse(
    string FromStage,
    string ToStage,
    string ChangedByName,
    DateTime ChangedAt,
    string? Reason
);
