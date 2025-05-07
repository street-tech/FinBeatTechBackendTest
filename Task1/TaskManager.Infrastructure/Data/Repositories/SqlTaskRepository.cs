using Microsoft.EntityFrameworkCore;
using TaskManager.Application.Interfaces;
using TaskManager.Domain.Entities;

namespace TaskManager.Infrastructure.Data.Repositories;

/// <summary>
/// Implements ITaskRepository using Entity Framework Core for SQL Server.
/// </summary>
public class SqlTaskRepository(TaskDbContext context) : ITaskRepository
{
    public async Task<TaskItem?> GetById(int id)
    {
        return await context.Tasks.FindAsync(id);
    }

    public async Task<List<TaskItem>> GetAll()
    {
        return await context.Tasks.ToListAsync();
    }

    public async Task<TaskItem> Create(TaskItem task)
    {
        context.Tasks.Add(task);
        await context.SaveChangesAsync();
        return task;
    }

    public async Task Update(TaskItem task)
    {
        context.Entry(task).State = EntityState.Modified;
        context.Entry(task).Property(x => x.CreatedAt).IsModified = false;
        await context.SaveChangesAsync();
    }

    public async Task<bool> Delete(int id)
    {
        var task = await context.Tasks.FindAsync(id);
        if (task == null)
        {
            return false;
        }

        context.Tasks.Remove(task);
        await context.SaveChangesAsync();
        return true;
    }
}