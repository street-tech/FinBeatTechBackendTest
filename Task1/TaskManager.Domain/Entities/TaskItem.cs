using TaskManager.Domain.Enums;

namespace TaskManager.Domain.Entities;

public class TaskItem
{
    /// <summary>
    /// Unique identifier for the task.
    /// </summary>
    public int Id { get; init; }

    /// <summary>
    /// The title or name of the task.
    /// </summary>
    public required string Title { get; set; }

    /// <summary>
    /// A detailed description of the task.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// The current status of the task.
    /// </summary>
    public TaskItemStatus ItemStatus { get; set; }

    /// <summary>
    /// The date and time when the task was created (UTC).
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// The date and time when the task was last updated (UTC).
    /// </summary>
    public DateTime UpdatedAt { get; set; }
}