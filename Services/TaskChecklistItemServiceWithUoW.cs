using Collaborative_Task_Management_System.Models;
using Collaborative_Task_Management_System.UnitOfWork;

namespace Collaborative_Task_Management_System.Services
{
    public class TaskChecklistItemServiceWithUoW : ITaskChecklistItemServiceWithUoW
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<TaskChecklistItemServiceWithUoW> _logger;

        public TaskChecklistItemServiceWithUoW(IUnitOfWork unitOfWork, ILogger<TaskChecklistItemServiceWithUoW> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<List<TaskChecklistItem>> GetAllChecklistItemsAsync()
        {
            try
            {
                var items = await _unitOfWork.Repository<TaskChecklistItem>().GetAllAsync();
                return items.OrderBy(i => i.Position).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all checklist items");
                throw;
            }
        }

        public async Task<TaskChecklistItem> GetChecklistItemByIdAsync(int id)
        {
            try
            {
                return await _unitOfWork.Repository<TaskChecklistItem>().GetByIdAsync(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving checklist item with ID: {ItemId}", id);
                throw;
            }
        }

        public async Task<List<TaskChecklistItem>> GetChecklistItemsByTaskIdAsync(int taskId)
        {
            try
            {
                var items = await _unitOfWork.Repository<TaskChecklistItem>().FindAsync(i => i.TaskId == taskId);
                return items.OrderBy(i => i.Position).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving checklist items for task: {TaskId}", taskId);
                throw;
            }
        }

        public async Task<TaskChecklistItem> CreateChecklistItemAsync(TaskChecklistItem item)
        {
            try
            {
                // Get the highest position for the current task
                var items = await GetChecklistItemsByTaskIdAsync(item.TaskId);
                item.Position = items.Count > 0 ? items.Max(i => i.Position) + 1 : 0;

                var newItem = await _unitOfWork.Repository<TaskChecklistItem>().AddAsync(item);
                await _unitOfWork.SaveChangesAsync();
                return newItem;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating checklist item for task: {TaskId}", item.TaskId);
                throw;
            }
        }

        public async Task<TaskChecklistItem> UpdateChecklistItemAsync(TaskChecklistItem item)
        {
            try
            {
                _unitOfWork.Repository<TaskChecklistItem>().Update(item);
                await _unitOfWork.SaveChangesAsync();
                return item;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating checklist item with ID: {ItemId}", item.Id);
                throw;
            }
        }

        public async Task<TaskChecklistItem> ToggleChecklistItemCompletionAsync(int itemId)
        {
            try
            {
                var item = await GetChecklistItemByIdAsync(itemId);
                if (item == null)
                {
                    throw new ArgumentException("Checklist item not found");
                }

                item.IsCompleted = !item.IsCompleted;
                _unitOfWork.Repository<TaskChecklistItem>().Update(item);
                await _unitOfWork.SaveChangesAsync();
                return item;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling completion for checklist item: {ItemId}", itemId);
                throw;
            }
        }

        public async Task DeleteChecklistItemAsync(int id)
        {
            try
            {
                var item = await GetChecklistItemByIdAsync(id);
                if (item == null)
                {
                    return; // Item doesn't exist, nothing to delete
                }

                int taskId = item.TaskId;
                int position = item.Position;

                // Delete the item
                await _unitOfWork.Repository<TaskChecklistItem>().DeleteByIdAsync(id);

                // Reorder remaining items
                var remainingItems = await _unitOfWork.Repository<TaskChecklistItem>().FindAsync(
                    i => i.TaskId == taskId && i.Position > position);

                foreach (var remainingItem in remainingItems)
                {
                    remainingItem.Position--;
                    _unitOfWork.Repository<TaskChecklistItem>().Update(remainingItem);
                }

                await _unitOfWork.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting checklist item with ID: {ItemId}", id);
                throw;
            }
        }

        public async Task ReorderChecklistItemsAsync(int taskId, List<int> itemIds)
        {
            try
            {
                // Verify all items exist and belong to the task
                var existingItems = await GetChecklistItemsByTaskIdAsync(taskId);
                var existingIds = existingItems.Select(i => i.Id).ToList();

                if (itemIds.Any(id => !existingIds.Contains(id)) || itemIds.Count != existingItems.Count)
                {
                    throw new ArgumentException("Invalid item IDs provided for reordering");
                }

                // Update positions based on the provided order
                for (int i = 0; i < itemIds.Count; i++)
                {
                    var item = existingItems.First(x => x.Id == itemIds[i]);
                    item.Position = i;
                    _unitOfWork.Repository<TaskChecklistItem>().Update(item);
                }

                await _unitOfWork.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reordering checklist items for task: {TaskId}", taskId);
                throw;
            }
        }
    }
}