using Collaborative_Task_Management_System.Models;

namespace Collaborative_Task_Management_System.Repositories
{
    public interface ITaskChecklistItemRepository : IRepository<TaskChecklistItem>
    {
        Task<IEnumerable<TaskChecklistItem>> GetChecklistItemsByTaskIdAsync(int taskId);
        Task<int> GetMaxPositionForTaskAsync(int taskId);
        Task ReorderChecklistItemsAsync(int taskId, int oldPosition, int newPosition);
        Task ToggleCompletionStatusAsync(int itemId);
    }
}