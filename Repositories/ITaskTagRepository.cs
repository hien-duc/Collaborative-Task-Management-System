using Collaborative_Task_Management_System.Models;

namespace Collaborative_Task_Management_System.Repositories
{
    public interface ITaskTagRepository : IRepository<TaskTag>
    {
        Task<IEnumerable<TaskTag>> GetTaskTagsByTaskIdAsync(int taskId);
        Task<IEnumerable<TaskTag>> GetTaskTagsByTagIdAsync(int tagId);
        Task<bool> AddTaskTagAsync(int taskId, int tagId);
        Task RemoveTaskTagAsync(int taskId, int tagId);
    }
}