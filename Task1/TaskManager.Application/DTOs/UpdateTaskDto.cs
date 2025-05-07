using System.ComponentModel.DataAnnotations;
using TaskManager.Domain.Enums;

namespace TaskManager.Application.DTOs;

/// <summary>
/// DTO for updating an existing task.
/// </summary>
public record UpdateTaskDto
{
    /// <summary>
    /// The new title of the task.
    /// </summary>
    [Required]
    [StringLength(200)]
    public required string Title { get; init; }

    /// <summary>
    /// The new description of the task.
    /// </summary>
    [StringLength(1000)]
    public string? Description { get; init; }

    /// <summary>
    /// The new status of the task.
    /// </summary>
    [Required]
    [EnumDataType(typeof(TaskItemStatus))] // Validate enum value
    public TaskItemStatus ItemStatus { get; init; }
}