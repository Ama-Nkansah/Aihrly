using System.ComponentModel.DataAnnotations;
using Aihrly.Models;

namespace Aihrly.DTOs;

public class CreateNoteRequest
{
    [Required]
    public NoteType Type { get; set; }

    [Required]
    public string Description { get; set; } = null!;
}

public record NoteResponse(
    Guid Id,
    string Type,
    string Description,
    string AuthorName,
    DateTime CreatedAt
);
