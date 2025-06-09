using Collaborative_Task_Management_System.Models;

namespace Collaborative_Task_Management_System.Repositories
{
    public interface ITaskActivityLogRepository : IRepository<TaskActivityLog>
    {
        Task<IEnumerable<TaskActivityLog>> GetActivityLogsByTaskIdAsync(int taskId);
        Task<IEnumerable<TaskActivityLog>> GetActivityLogsByUserIdAsync(string userId);
        Task<IEnumerable<TaskActivityLog>> GetRecentActivityLogsAsync(int count = 20);
        Task<IEnumerable<TaskActivityLog>> GetActivityLogsByActionAsync(string action);
    }
}