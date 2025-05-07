namespace TaskManager.Domain.Enums;

/// <summary>
/// Represents the status of a task.
/// </summary>
public enum TaskItemStatus
{
    /// <summary>
    /// The task is new and has not been started.
    /// </summary>
    New = 1,

    /// <summary>
    /// The task is currently being worked on.
    /// </summary>
    InProgress = 2,

    /// <summary>
    /// The task has been completed.
    /// </summary>
    Completed = 3
}