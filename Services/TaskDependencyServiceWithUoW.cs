using Collaborative_Task_Management_System.Models;
using Collaborative_Task_Management_System.UnitOfWork;
using TaskStatus = Collaborative_Task_Management_System.Models.TaskStatus;

namespace Collaborative_Task_Management_System.Services
{
    public class TaskDependencyServiceWithUoW : ITaskDependencyServiceWithUoW
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<TaskDependencyServiceWithUoW> _logger;

        public TaskDependencyServiceWithUoW(IUnitOfWork unitOfWork, ILogger<TaskDependencyServiceWithUoW> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<List<TaskDependency>> GetAllDependenciesAsync()
        {
            try
            {
                var dependencies = await _unitOfWork.TaskDependencies.GetAllAsync();
                return dependencies.ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all task dependencies");
                throw;
            }
        }

        public async Task<TaskDependency> GetDependencyByIdAsync(int id)
        {
            try
            {
                return await _unitOfWork.TaskDependencies.GetByIdAsync(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving task dependency with ID: {DependencyId}", id);
                throw;
            }
        }

        public async Task<List<TaskItem>> GetBlockingTasksAsync(int taskId)
        {
            try
            {
                var dependencies = await _unitOfWork.TaskDependencies.GetDependenciesByTaskIdAsync(taskId);
                return dependencies.Select(d => d.BlockingTask).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving blocking tasks for task ID: {TaskId}", taskId);
                throw;
            }
        }

        public async Task<List<TaskItem>> GetBlockedTasksAsync(int taskId)
        {
            try
            {
                var dependencies = await _unitOfWork.TaskDependencies.GetBlockingDependenciesByTaskIdAsync(taskId);
                return dependencies.Select(d => d.Task).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving blocked tasks for task ID: {TaskId}", taskId);
                throw;
            }
        }

        public async Task<TaskDependency> AddDependencyAsync(int taskId, int blockingTaskId)
        {
            try
            {
                // Check if the dependency already exists
                if (await _unitOfWork.TaskDependencies.DependencyExistsAsync(taskId, blockingTaskId))
                {
                    throw new InvalidOperationException("This dependency already exists.");
                }

                // Check if adding this dependency would create a circular reference
                if (await _unitOfWork.TaskDependencies.WouldCreateCircularDependencyAsync(taskId, blockingTaskId))
                {
                    throw new InvalidOperationException("Adding this dependency would create a circular reference.");
                }

                // Create the new dependency
                var dependency = new TaskDependency
                {
                    TaskId = taskId,
                    BlockingTaskId = blockingTaskId,
                    CreatedAt = DateTime.UtcNow
                };

                await _unitOfWork.TaskDependencies.AddAsync(dependency);
                await _unitOfWork.SaveChangesAsync();

                return dependency;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding dependency between task ID: {TaskId} and blocking task ID: {BlockingTaskId}", taskId, blockingTaskId);
                throw;
            }
        }

        public async Task RemoveDependencyAsync(int taskId, int blockingTaskId)
        {
            try
            {
                // Find the dependency
                var dependency = await _unitOfWork.TaskDependencies.FirstOrDefaultAsync(
                    d => d.TaskId == taskId && d.BlockingTaskId == blockingTaskId);

                if (dependency == null)
                {
                    throw new KeyNotFoundException("Dependency not found.");
                }

                _unitOfWork.TaskDependencies.Delete(dependency);
                await _unitOfWork.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing dependency between task ID: {TaskId} and blocking task ID: {BlockingTaskId}", taskId, blockingTaskId);
                throw;
            }
        }

        public async Task<bool> HasCircularDependencyAsync(int taskId, int blockingTaskId)
        {
            try
            {
                return await _unitOfWork.TaskDependencies.WouldCreateCircularDependencyAsync(taskId, blockingTaskId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking for circular dependency between task ID: {TaskId} and blocking task ID: {BlockingTaskId}", taskId, blockingTaskId);
                throw;
            }
        }

        public async Task<bool> CanCompleteTaskAsync(int taskId)
        {
            try
            {
                // A task can be completed if all its blocking tasks are completed
                var blockingTasks = await GetBlockingTasksAsync(taskId);
                return blockingTasks.All(t => t.Status == TaskStatus.Completed);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if task ID: {TaskId} can be completed", taskId);
                throw;
            }
        }
    }
}