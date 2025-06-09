using Collaborative_Task_Management_System.Models;
using TaskStatus = Collaborative_Task_Management_System.Models.TaskStatus;

namespace Collaborative_Task_Management_System.Services
{
    public interface ITaskActivityLogServiceWithUoW
    {
        Task<List<TaskActivityLog>> GetAllActivityLogsAsync();
        Task<TaskActivityLog> GetActivityLogByIdAsync(int id);
        Task<List<TaskActivityLog>> GetActivityLogsByTaskIdAsync(int taskId);
        Task<List<TaskActivityLog>> GetActivityLogsByUserIdAsync(string userId);
        Task<List<TaskActivityLog>> GetRecentActivityLogsAsync(int count = 50);
        Task<TaskActivityLog> CreateActivityLogAsync(TaskActivityLog activityLog);
        Task<TaskActivityLog> LogTaskCreatedAsync(int taskId, string userId);
        Task<TaskActivityLog> LogTaskUpdatedAsync(int taskId, string userId, string changes);
        Task<TaskActivityLog> LogTaskStatusChangedAsync(int taskId, string userId, TaskStatus oldStatus, TaskStatus newStatus);
        Task<TaskActivityLog> LogTaskAssignedAsync(int taskId, string userId, string assignedToUserId);
        Task<TaskActivityLog> LogTaskCommentAddedAsync(int taskId, string userId);
        Task<TaskActivityLog> LogTaskAttachmentAddedAsync(int taskId, string userId, string fileName);
    }
}