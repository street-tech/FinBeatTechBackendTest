using System.Text.Json;
using Microsoft.Extensions.Logging;
using TaskManager.Application.DTOs;
using TaskManager.Application.Interfaces;
using TaskManager.Domain.Entities;

namespace TaskManager.Infrastructure.Services;

/// <summary>
/// Implements the business logic for task management.
/// </summary>
public class TaskService(
    ITaskRepository taskRepository,
    IMessageProducer messageProducer,
    ILogger<TaskService> logger
) : ITaskService
{
    public async Task<TaskDto> CreateTask(CreateTaskDto createTaskDto)
    {
        logger.LogInformation("Attempting to create a new task with title: {TaskTitle}", createTaskDto.Title);
        var taskItem = new TaskItem
        {
            Title = createTaskDto.Title,
            Description = createTaskDto.Description,
            ItemStatus = createTaskDto.ItemStatus
        };

        var createdTask = await taskRepository.Create(taskItem);
        var taskDto = MapToTaskDto(createdTask);
        
        try
        {
            var message = new { EventType = "TaskCreated", Payload = taskDto };
            await messageProducer.SendMessageAsync(
                "tasks",
                "task.created",
                JsonSerializer.Serialize(message)
            );
            logger.LogInformation("TaskCreated event published for Task ID: {TaskId}", taskDto.Id);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to publish TaskCreated event for Task ID: {TaskId}", taskDto.Id);
        }

        logger.LogInformation("Successfully created Task ID: {TaskId}", taskDto.Id);
        return taskDto;
    }
    
    public async Task<TaskDto?> GetTaskById(int id)
    {
        logger.LogInformation("Attempting to retrieve task with ID: {TaskId}", id);
        var taskItem = await taskRepository.GetById(id);
        if (taskItem == null)
        {
            logger.LogWarning("Task with ID: {TaskId} not found.", id);
            return null;
        }
        logger.LogInformation("Successfully retrieved task with ID: {TaskId}", id);
        return MapToTaskDto(taskItem);
    }
    
    public async Task<IEnumerable<TaskDto>> GetAllTasksAsync()
    {
        logger.LogInformation("Attempting to retrieve all tasks.");
        var taskItems = await taskRepository.GetAll();
        var taskDtos = taskItems.Select(MapToTaskDto).ToList();
        logger.LogInformation("Successfully retrieved {TaskCount} tasks.", taskDtos.Count);
        return taskDtos;
    }
    
    public async Task<bool> UpdateTaskAsync(int id, UpdateTaskDto updateTaskDto)
    {
        logger.LogInformation("Attempting to update task with ID: {TaskId}", id);
        var existingTask = await taskRepository.GetById(id);
        if (existingTask == null)
        {
            logger.LogWarning("Update failed. Task with ID: {TaskId} not found.", id);
            return false;
        }

        existingTask.Title = updateTaskDto.Title;
        existingTask.Description = updateTaskDto.Description;
        existingTask.ItemStatus = updateTaskDto.ItemStatus;

        await taskRepository.Update(existingTask);
        var taskDto = MapToTaskDto(existingTask);
        
        try
        {
            var message = new { EventType = "TaskUpdated", Payload = taskDto };
            await messageProducer.SendMessageAsync(
                "tasks",
                "task.updated",
                JsonSerializer.Serialize(message)
            );
            logger.LogInformation("TaskUpdated event published for Task ID: {TaskId}", id);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to publish TaskUpdated event for Task ID: {TaskId}", id);
        }

        logger.LogInformation("Successfully updated Task ID: {TaskId}", id);
        return true;
    }
    
    public async Task<bool> DeleteTaskAsync(int id)
    {
        logger.LogInformation("Attempting to delete task with ID: {TaskId}", id);
        var taskToDelete = await taskRepository.GetById(id);
        if (taskToDelete == null)
        {
            logger.LogWarning("Delete failed. Task with ID: {TaskId} not found.", id);
            return false;
        }

        var deleted = await taskRepository.Delete(id);

        if (deleted)
        {
            try
            {
                var message = new { EventType = "TaskDeleted", Payload = new { TaskId = id } };
                await messageProducer.SendMessageAsync(
                    "tasks",
                    "task.deleted",
                    JsonSerializer.Serialize(message)
                );
                logger.LogInformation("TaskDeleted event published for Task ID: {TaskId}", id);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to publish TaskDeleted event for Task ID: {TaskId}", id);
            }
            logger.LogInformation("Successfully deleted Task ID: {TaskId}", id);
        }
        else
        {
            logger.LogError("Deletion reported failure for Task ID: {TaskId} despite being found initially.", id);
        }

        return deleted;
    }
    
    private static TaskDto MapToTaskDto(TaskItem taskItem)
    {
        return new TaskDto
        {
            Id = taskItem.Id,
            Title = taskItem.Title,
            Description = taskItem.Description,
            ItemStatus = taskItem.ItemStatus,
            CreatedAt = taskItem.CreatedAt,
            UpdatedAt = taskItem.UpdatedAt
        };
    }
}