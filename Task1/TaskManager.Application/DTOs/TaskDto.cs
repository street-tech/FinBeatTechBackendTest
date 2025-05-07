using TaskManager.Domain.Enums;

// Нужно обрать внимение и поменять нейминг

namespace TaskManager.Application.DTOs;

/// <summary>
/// DTO for representing a task.
/// </summary>
public record TaskDto
{
    /// <summary>
    /// Task ID.
    /// </summary>
    public int Id { get; init; }
    /// <summary>
    /// Task Title.
    /// </summary>
    public required string Title { get; init; }
    /// <summary>
    /// Task Description.
    /// </summary>
    public string? Description { get; init; }
    /// <summary>
    /// Task Status.
    /// </summary>
    public TaskItemStatus ItemStatus { get; init; }
    /// <summary>
    /// Creation timestamp (UTC).
    /// </summary>
    public DateTime CreatedAt { get; init; }
    /// <summary>
    /// Last update timestamp (UTC).
    /// </summary>
    public DateTime UpdatedAt { get; init; }
}