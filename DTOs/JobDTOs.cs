using System.ComponentModel.DataAnnotations;

namespace Aihrly.DTOs;

public class CreateJobRequest
{
    [Required]
    public string Title { get; set; } = null!;

    [Required]
    public string Description { get; set; } = null!;

    [Required]
    public string Location { get; set; } = null!;
}

public record JobResponse(
    Guid Id,
    string Title,
    string Description,
    string Location,
    string Status,
    DateTime CreatedAt
);
