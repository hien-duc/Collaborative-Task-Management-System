using Collaborative_Task_Management_System.Models;

namespace Collaborative_Task_Management_System.Services
{
    public interface ITaskDependencyServiceWithUoW
    {
        Task<List<TaskDependency>> GetAllDependenciesAsync();
        Task<TaskDependency> GetDependencyByIdAsync(int id);
        Task<List<TaskItem>> GetBlockingTasksAsync(int taskId);
        Task<List<TaskItem>> GetBlockedTasksAsync(int taskId);
        Task<TaskDependency> AddDependencyAsync(int taskId, int blockingTaskId);
        Task RemoveDependencyAsync(int taskId, int blockingTaskId);
        Task<bool> HasCircularDependencyAsync(int taskId, int blockingTaskId);
        Task<bool> CanCompleteTaskAsync(int taskId);
    }
}