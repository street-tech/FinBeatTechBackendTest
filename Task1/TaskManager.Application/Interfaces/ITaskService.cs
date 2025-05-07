using TaskManager.Application.DTOs;

namespace TaskManager.Application.Interfaces;

/// <summary>
/// Defines the contract for task management business logic.
/// </summary>
public interface ITaskService
{
    /// <summary>
    /// Creates a new task.
    /// </summary>
    /// <param name="createTaskDto">Data for the new task.</param>
    /// <returns>The created task DTO.</returns>
    Task<TaskDto> CreateTask(CreateTaskDto createTaskDto);

    /// <summary>
    /// Gets a task by its ID.
    /// </summary>
    /// <param name="id">The task ID.</param>
    /// <returns>The task DTO or null if not found.</returns>
    Task<TaskDto?> GetTaskById(int id);

    /// <summary>
    /// Gets all tasks.
    /// </summary>
    /// <returns>A list of task DTOs.</returns>
    Task<IEnumerable<TaskDto>> GetAllTasksAsync();

    /// <summary>
    /// Updates an existing task.
    /// </summary>
    /// <param name="id">The ID of the task to update.</param>
    /// <param name="updateTaskDto">The updated task data.</param>
    /// <returns>True if update was successful, false if task not found.</returns>
    Task<bool> UpdateTaskAsync(int id, UpdateTaskDto updateTaskDto);

    /// <summary>
    /// Deletes a task by its ID.
    /// </summary>
    /// <param name="id">The ID of the task to delete.</param>
    /// <returns>True if deletion was successful, false if task not found.</returns>
    Task<bool> DeleteTaskAsync(int id);
}