using Collaborative_Task_Management_System.Models;
using System.Linq.Expressions;

namespace Collaborative_Task_Management_System.Repositories
{
    public interface ITaskRepository : IRepository<TaskItem>
    {
        Task<IEnumerable<TaskItem>> GetTasksByProjectIdAsync(int projectId);
        Task<IEnumerable<TaskItem>> GetTasksByAssignedUserAsync(string userId);
        Task<IEnumerable<TaskItem>> GetTasksByCreatorAsync(string userId);
        Task<IEnumerable<TaskItem>> GetTasksByStatusAsync(TaskStatus status);
        Task<IEnumerable<TaskItem>> GetTasksByPriorityAsync(string priority);
        Task<IEnumerable<TaskItem>> GetOverdueTasksAsync();
        Task<IEnumerable<TaskItem>> GetTasksDueSoonAsync(int days = 7);
        Task<TaskItem> GetTaskWithCommentsAsync(int taskId);
        Task<TaskItem> GetTaskWithAttachmentsAsync(int taskId);
        Task<TaskItem> GetTaskWithAllDetailsAsync(int taskId);
        Task<IEnumerable<TaskItem>> SearchTasksAsync(string searchTerm);
        Task<IEnumerable<TaskItem>> GetTasksByDateRangeAsync(DateTime startDate, DateTime endDate);
    }
}