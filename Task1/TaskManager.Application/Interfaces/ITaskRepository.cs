using System.Collections.Generic;
using System.Threading.Tasks;
using TaskManager.Domain.Entities;

namespace TaskManager.Application.Interfaces;

/// <summary>
/// Defines the contract for data access operations related to tasks.
/// </summary>
public interface ITaskRepository
{
    /// <summary>
    /// Gets a task by its unique identifier.
    /// </summary>
    /// <param name="id">The task ID.</param>
    /// <returns>The task item or null if not found.</returns>
    Task<TaskItem?> GetById(int id);

    /// <summary>
    /// Gets all tasks.
    /// </summary>
    /// <returns>A list of all task items.</returns>
    Task<List<TaskItem>> GetAll();

    /// <summary>
    /// Adds a new task to the data store.
    /// </summary>
    /// <param name="task">The task item to add.</param>
    /// <returns>The added task item with its generated ID.</returns>
    Task<TaskItem> Create(TaskItem task);

    /// <summary>
    /// Updates an existing task in the data store.
    /// </summary>
    /// <param name="task">The task item with updated values.</param>
    /// <returns>Task representing the asynchronous operation.</returns>
    Task Update(TaskItem task);

    /// <summary>
    /// Deletes a task by its unique identifier.
    /// </summary>
    /// <param name="id">The ID of the task to delete.</param>
    /// <returns>True if deletion was successful, false otherwise.</returns>
    Task<bool> Delete(int id);
}