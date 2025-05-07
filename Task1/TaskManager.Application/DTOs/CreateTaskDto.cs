using System.ComponentModel.DataAnnotations;
using TaskManager.Domain.Enums;

namespace TaskManager.Application.DTOs;

/// <summary>
/// DTO for creating a new task.
/// </summary>
public record CreateTaskDto
{
    /// <summary>
    /// The title of the task.
    /// </summary>
    [Required]
    [StringLength(200)]
    public required string Title { get; init; }

    /// <summary>
    /// The description of the task.
    /// </summary>
    [StringLength(1000)]
    public string? Description { get; init; }

    /// <summary>
    /// The initial status of the task. Defaults to New.
    /// </summary>
    public TaskItemStatus ItemStatus { get; init; } = TaskItemStatus.New; // Default value
}