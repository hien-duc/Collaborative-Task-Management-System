using Collaborative_Task_Management_System.Models;

namespace Collaborative_Task_Management_System.Repositories
{
    public interface ITaskDependencyRepository : IRepository<TaskDependency>
    {
        Task<IEnumerable<TaskDependency>> GetDependenciesByTaskIdAsync(int taskId);
        Task<IEnumerable<TaskDependency>> GetBlockingDependenciesByTaskIdAsync(int taskId);
        Task<bool> DependencyExistsAsync(int taskId, int blockingTaskId);
        Task<bool> WouldCreateCircularDependencyAsync(int taskId, int blockingTaskId);
    }
}