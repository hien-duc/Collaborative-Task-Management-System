using Collaborative_Task_Management_System.Models;

namespace Collaborative_Task_Management_System.Services
{
    public interface ITaskChecklistItemServiceWithUoW
    {
        Task<List<TaskChecklistItem>> GetAllChecklistItemsAsync();
        Task<TaskChecklistItem> GetChecklistItemByIdAsync(int id);
        Task<List<TaskChecklistItem>> GetChecklistItemsByTaskIdAsync(int taskId);
        Task<TaskChecklistItem> CreateChecklistItemAsync(TaskChecklistItem item);
        Task<TaskChecklistItem> UpdateChecklistItemAsync(TaskChecklistItem item);
        Task<TaskChecklistItem> ToggleChecklistItemCompletionAsync(int itemId);
        Task DeleteChecklistItemAsync(int id);
        Task ReorderChecklistItemsAsync(int taskId, List<int> itemIds);
    }
}