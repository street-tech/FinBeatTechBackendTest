using System.Text.Json;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NUnit.Framework;
using TaskManager.Application.DTOs;
using TaskManager.Application.Interfaces;
using TaskManager.Infrastructure.Services;
using TaskManager.Domain.Entities;
using TaskManager.Domain.Enums;

namespace TaskManager.Tests.Services;

[TestFixture]
public class TaskServiceTests
{
    [Test]
    public async Task CreateTaskAsync_ValidInput_ReturnsTaskDtoAndSendsMessage()
    {
        // Arrange
        var environment = new TestEnvironment();
        var createTaskDto = new CreateTaskDto { Title = "New Test Task", Description = "Desc", ItemStatus = TaskItemStatus.New };
        var taskItem = new TaskItem
        {
            Id = 1,
            Title = "New Test Task",
            Description = "Desc",
            ItemStatus = TaskItemStatus.New,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        var expectedDto = new TaskDto
        {
            Id = 1,
            Title = "New Test Task",
            Description = "Desc",
            ItemStatus = TaskItemStatus.New,
            CreatedAt = taskItem.CreatedAt,
            UpdatedAt = taskItem.UpdatedAt
        };

        environment.TaskRepository.Create(Arg.Any<TaskItem>())
                .Returns(Task.FromResult(taskItem));

        // Act
        var result = await environment.Target.CreateTask(createTaskDto);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Id, Is.EqualTo(expectedDto.Id));
        Assert.That(result.Title, Is.EqualTo(expectedDto.Title));
        Assert.That(result.ItemStatus, Is.EqualTo(expectedDto.ItemStatus));

        await environment.TaskRepository.Received(1).Create(Arg.Is<TaskItem>(t =>
            t.Title == createTaskDto.Title &&
            t.Description == createTaskDto.Description &&
            t.ItemStatus == createTaskDto.ItemStatus
        ));

        await environment.MessageProducer.Received(1).SendMessageAsync(
            Arg.Is("tasks"),
            Arg.Is("task.created"),
            Arg.Do<string>(
                msg => VerifyMessageContent(msg, "TaskCreated", expectedDto)
            )
        );
    }

    [Test]
    public async Task CreateTaskAsync_RepositoryThrowsException_DoesNotSendMessageAndThrows()
    {
        // Arrange
        var environment = new TestEnvironment();
        var createTaskDto = new CreateTaskDto { Title = "Faulty Task" };
        var exception = new InvalidOperationException("Database error");

        environment.TaskRepository.Create(Arg.Any<TaskItem>())
                    .ThrowsAsync(exception);

        // Act & Assert
        Assert.ThrowsAsync<InvalidOperationException>(async () => 
            await environment.Target.CreateTask(createTaskDto));

        await environment.MessageProducer.DidNotReceive()
            .SendMessageAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>());
    }

    [Test]
    public async Task GetTaskByIdAsync_ExistingId_ReturnsTaskDto()
    {
        // Arrange
        var environment = new TestEnvironment();
        const int taskId = 1;
        var taskItem = new TaskItem { 
            Id = taskId, 
            Title = "Existing Task", 
            ItemStatus = TaskItemStatus.InProgress, 
            CreatedAt = DateTime.UtcNow, 
            UpdatedAt = DateTime.UtcNow 
        };
        environment.TaskRepository.GetById(taskId)
            .Returns(Task.FromResult<TaskItem?>(taskItem));

        // Act
        var result = await environment.Target.GetTaskById(taskId);

        // Assert
        Assert.That(result, Is.Not.Null);
        if (result != null)
        {
            Assert.That(result.Id, Is.EqualTo(taskId));
            Assert.That(result.Title, Is.EqualTo(taskItem.Title));
        }

        await environment.TaskRepository.Received(1).GetById(taskId);
    }

    [Test]
    public async Task GetTaskByIdAsync_NonExistingId_ReturnsNull()
    {
        // Arrange
        var environment = new TestEnvironment();
        int taskId = 99;
        environment.TaskRepository.GetById(taskId)
            .Returns(Task.FromResult<TaskItem?>(null));

        // Act
        var result = await environment.Target.GetTaskById(taskId);

        // Assert
        Assert.That(result, Is.Null);
        await environment.TaskRepository.Received(1).GetById(taskId);
    }

    [Test]
    public async Task GetAllTasksAsync_ReturnsAllTaskDtos()
    {
        // Arrange
        var environment = new TestEnvironment();
        var taskItems = new List<TaskItem>
        {
            new()
            {
                Id = 1,
                Title = "Task 1",
                ItemStatus = TaskItemStatus.New,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new()
            {
                Id = 2,
                Title = "Task 2",
                ItemStatus = TaskItemStatus.Completed,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        };
        environment.TaskRepository.GetAll().Returns(Task.FromResult(taskItems));

        // Act
        var results = await environment.Target.GetAllTasksAsync();
        var resultsList = results.ToList();

        // Assert
        Assert.That(resultsList, Is.Not.Null);
        Assert.That(resultsList, Has.Count.EqualTo(2));
        Assert.That(resultsList[0].Title, Is.EqualTo("Task 1"));
        Assert.That(resultsList[1].Title, Is.EqualTo("Task 2"));
        await environment.TaskRepository.Received(1).GetAll();
    }

    [Test]
    public async Task UpdateTaskAsync_ExistingId_ReturnsTrueUpdatesRepoAndSendsMessage()
    {
        // Arrange
        var environment = new TestEnvironment();
        const int taskId = 1;
        var updateDto = new UpdateTaskDto { 
            Title = "Updated Title", 
            Description = "Updated Desc", 
            ItemStatus = TaskItemStatus.InProgress 
        };
        var existingTask = new TaskItem { 
            Id = taskId, 
            Title = "Old Title", 
            Description = "Old Desc", 
            ItemStatus = TaskItemStatus.New, 
            CreatedAt = DateTime.UtcNow.AddDays(-1), 
            UpdatedAt = DateTime.UtcNow.AddDays(-1) 
        };

        environment.TaskRepository.GetById(taskId)
            .Returns(Task.FromResult<TaskItem?>(existingTask));

        // Act
        var result = await environment.Target.UpdateTaskAsync(taskId, updateDto);

        // Assert
        Assert.That(result, Is.True);
        await environment.TaskRepository.Received(1).GetById(taskId);
        await environment.TaskRepository.Received(1).Update(Arg.Is<TaskItem>(t =>
            t.Id == taskId &&
            t.Title == updateDto.Title &&
            t.Description == updateDto.Description &&
            t.ItemStatus == updateDto.ItemStatus
        ));

        await environment.MessageProducer.Received(1).SendMessageAsync(
            Arg.Is("tasks"),
            Arg.Is("task.updated"),
            Arg.Is<string>(
                msg => VerifyMessageContent(
                    msg,
                    "TaskUpdated",
                    null,
                    taskId,
                    updateDto.Title
                )
            )
        );
    }

    [Test]
    public async Task DeleteTaskAsync_ExistingId_ReturnsTrueDeletesFromRepoAndSendsMessage()
    {
        // Arrange
        var environment = new TestEnvironment();
        const int taskId = 1;
        var taskToDelete = new TaskItem { 
            Id = taskId, 
            Title = "Task to Delete", 
            ItemStatus = TaskItemStatus.Completed, 
            CreatedAt = DateTime.UtcNow, 
            UpdatedAt = DateTime.UtcNow 
        };

        environment.TaskRepository.GetById(taskId)
            .Returns(Task.FromResult<TaskItem?>(taskToDelete));
        environment.TaskRepository.Delete(taskId)
            .Returns(Task.FromResult(true));

        // Act
        var result = await environment.Target.DeleteTaskAsync(taskId);

        // Assert
        Assert.That(result, Is.True);
        await environment.TaskRepository.Received(1).GetById(taskId);
        await environment.TaskRepository.Received(1).Delete(taskId);

        await environment.MessageProducer.Received(1).SendMessageAsync(
            Arg.Is("tasks"),
            Arg.Is("task.deleted"),
            Arg.Is<string>(msg => VerifyDeleteMessageContent(msg, "TaskDeleted", taskId))
        );
    }

    [Test]
    public async Task DeleteTaskAsync_NonExistingId_ReturnsFalse()
    {
        // Arrange
        var environment = new TestEnvironment();
        const int taskId = 99;
        environment.TaskRepository.GetById(taskId)
            .Returns(Task.FromResult<TaskItem?>(null));

        // Act
        var result = await environment.Target.DeleteTaskAsync(taskId);

        // Assert
        Assert.That(result, Is.False);
        await environment.TaskRepository.Received(1).GetById(taskId);
        await environment.TaskRepository.DidNotReceive().Delete(taskId);
        await environment.MessageProducer.DidNotReceive()
            .SendMessageAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>());
    }

    private class TestEnvironment
    {
        public readonly ITaskRepository TaskRepository;
        public readonly IMessageProducer MessageProducer;
        public readonly TaskService Target;

        public TestEnvironment()
        {
            TaskRepository = Substitute.For<ITaskRepository>();
            MessageProducer = Substitute.For<IMessageProducer>();
            var logger = Substitute.For<ILogger<TaskService>>();

            Target = new TaskService(
                TaskRepository,
                MessageProducer,
                logger
            );
        }
    }

    private static bool VerifyMessageContent(
        string jsonMessage,
        string expectedEventType, 
        TaskDto? expectedPayload,
        int? expectedId = null,
        string? expectedTitle = null
    )
    {
        try
        {
            using var doc = JsonDocument.Parse(jsonMessage);
            var root = doc.RootElement;

            if (!root.TryGetProperty("EventType", out var eventTypeProp) || 
                eventTypeProp.GetString() != expectedEventType)
                return false;

            if (!root.TryGetProperty("Payload", out var payloadProp))
                return false;

            if (expectedPayload != null)
            {
                var payload = JsonSerializer.Deserialize<TaskDto>(
                    payloadProp.GetRawText(),
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                );
                return payload != null &&
                       payload.Id == expectedPayload.Id &&
                       payload.Title == expectedPayload.Title &&
                       payload.ItemStatus == expectedPayload.ItemStatus;
            }
            else if (expectedId.HasValue && expectedTitle != null)
            {
                var payload = JsonSerializer.Deserialize<TaskDto>(
                    payloadProp.GetRawText(),
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                );
                return payload != null &&
                       payload.Id == expectedId.Value &&
                       payload.Title == expectedTitle;
            }

            return false;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static bool VerifyDeleteMessageContent(string jsonMessage, string expectedEventType, int expectedTaskId)
    {
        try
        {
            using var doc = JsonDocument.Parse(jsonMessage);
            var root = doc.RootElement;

            if (!root.TryGetProperty("EventType", out var eventTypeProp) || 
                eventTypeProp.GetString() != expectedEventType)
                return false;

            if (!root.TryGetProperty("Payload", out var payloadProp))
                return false;

            return payloadProp.TryGetProperty("TaskId", out var taskIdProp) && 
                   taskIdProp.TryGetInt32(out var taskId) && 
                   taskId == expectedTaskId;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}