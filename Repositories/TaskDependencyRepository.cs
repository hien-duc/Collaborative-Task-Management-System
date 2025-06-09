using Microsoft.EntityFrameworkCore;
using Collaborative_Task_Management_System.Data;
using Collaborative_Task_Management_System.Models;

namespace Collaborative_Task_Management_System.Repositories
{
    public class TaskDependencyRepository : Repository<TaskDependency>, ITaskDependencyRepository
    {
        public TaskDependencyRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<TaskDependency>> GetDependenciesByTaskIdAsync(int taskId)
        {
            return await _dbSet
                .Where(dep => dep.TaskId == taskId)
                .Include(dep => dep.BlockingTask)
                .ToListAsync();
        }

        public async Task<IEnumerable<TaskDependency>> GetBlockingDependenciesByTaskIdAsync(int taskId)
        {
            return await _dbSet
                .Where(dep => dep.BlockingTaskId == taskId)
                .Include(dep => dep.Task)
                .ToListAsync();
        }

        public async Task<bool> DependencyExistsAsync(int taskId, int blockingTaskId)
        {
            return await _dbSet
                .AnyAsync(dep => dep.TaskId == taskId && dep.BlockingTaskId == blockingTaskId);
        }

        public async Task<bool> WouldCreateCircularDependencyAsync(int taskId, int blockingTaskId)
        {
            // Check if adding this dependency would create a circular reference
            // First, check if the blocking task depends on the task directly
            if (await _dbSet.AnyAsync(dep => dep.TaskId == blockingTaskId && dep.BlockingTaskId == taskId))
                return true;

            // Then check for indirect circular dependencies using a recursive approach
            return await CheckForCircularDependencyAsync(blockingTaskId, taskId, new HashSet<int>());
        }

        private async Task<bool> CheckForCircularDependencyAsync(int currentTaskId, int targetTaskId, HashSet<int> visitedTasks)
        {
            // If we've already visited this task, skip it to avoid infinite loops
            if (visitedTasks.Contains(currentTaskId))
                return false;

            // Mark this task as visited
            visitedTasks.Add(currentTaskId);

            // Get all tasks that the current task depends on
            var dependencies = await _dbSet
                .Where(dep => dep.TaskId == currentTaskId)
                .Select(dep => dep.BlockingTaskId)
                .ToListAsync();

            // Check if any of these dependencies is our target or depends on our target
            foreach (var dependencyId in dependencies)
            {
                if (dependencyId == targetTaskId || 
                    await CheckForCircularDependencyAsync(dependencyId, targetTaskId, visitedTasks))
                    return true;
            }

            return false;
        }
    }
}