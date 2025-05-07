using Microsoft.AspNetCore.Mvc;
using TaskManager.Application.DTOs;
using TaskManager.Application.Interfaces;

namespace TaskManager.Api.Controllers;

/// <summary>
/// API endpoints for managing tasks.
/// </summary>
[Route("api/[controller]")]
[ApiController]
public class TasksController(ITaskService taskService, ILogger<TasksController> logger) : ControllerBase
{
    /// <summary>
    /// Creates a new task.
    /// </summary>
    /// <param name="createTaskDto">The data for the new task.</param>
    /// <returns>The created task.</returns>
    /// <response code="201">Returns the newly created task.</response>
    /// <response code="400">If the item is null or invalid.</response>
    [HttpPost]
    [ProducesResponseType(typeof(TaskDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<TaskDto>> CreateTask([FromBody] CreateTaskDto createTaskDto)
    {
        logger.LogInformation("Received request to create task with title: {TaskTitle}", createTaskDto.Title);
        var createdTask = await taskService.CreateTask(createTaskDto);
        return CreatedAtAction(nameof(GetTaskById), new { id = createdTask.Id }, createdTask);
    }

    /// <summary>
    /// Gets a specific task by its ID.
    /// </summary>
    /// <param name="id">The ID of the task to retrieve.</param>
    /// <returns>The requested task.</returns>
    /// <response code="200">Returns the requested task.</response>
    /// <response code="404">If the task with the specified ID is not found.</response>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(TaskDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TaskDto>> GetTaskById(int id)
    {
        logger.LogInformation("Received request to get task by ID: {TaskId}", id);
        var task = await taskService.GetTaskById(id);
        if (task == null)
        {
            logger.LogWarning("Task with ID: {TaskId} not found.", id);
            return NotFound();
        }
        logger.LogInformation("Returning task with ID: {TaskId}", id);
        return Ok(task);
    }

    /// <summary>
    /// Gets all tasks.
    /// </summary>
    /// <returns>A list of all tasks.</returns>
    /// <response code="200">Returns a list of tasks.</response>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<TaskDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<TaskDto>>> GetAllTasks()
    {
        logger.LogInformation("Received request to get all tasks.");
        var tasks = await taskService.GetAllTasksAsync();
        logger.LogInformation("Returning {TaskCount} tasks.", tasks.Count());
        return Ok(tasks);
    }

    /// <summary>
    /// Updates an existing task.
    /// </summary>
    /// <param name="id">The ID of the task to update.</param>
    /// <param name="updateTaskDto">The updated task data.</param>
    /// <returns>No content if successful.</returns>
    /// <response code="204">If the update was successful.</response>
    /// <response code="400">If the provided data is invalid.</response>
    /// <response code="404">If the task with the specified ID is not found.</response>
    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateTask(int id, [FromBody] UpdateTaskDto updateTaskDto)
    {
        logger.LogInformation("Received request to update task with ID: {TaskId}", id);
        var success = await taskService.UpdateTaskAsync(id, updateTaskDto);
        if (!success)
        {
            logger.LogWarning("Update failed for task ID: {TaskId}. Task not found.", id);
            return NotFound();
        }
        logger.LogInformation("Task with ID: {TaskId} updated successfully.", id);
        return NoContent();
    }

    /// <summary>
    /// Deletes a task by its ID.
    /// </summary>
    /// <param name="id">The ID of the task to delete.</param>
    /// <returns>No content if successful.</returns>
    /// <response code="204">If the deletion was successful.</response>
    /// <response code="404">If the task with the specified ID is not found.</response>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteTask(int id)
    {
        logger.LogInformation("Received request to delete task with ID: {TaskId}", id);
        var success = await taskService.DeleteTaskAsync(id);
        if (!success)
        {
            logger.LogWarning("Delete failed for task ID: {TaskId}. Task not found.", id);
            return NotFound();
        }
        logger.LogInformation("Task with ID: {TaskId} deleted successfully.", id);
        return NoContent();
    }
}