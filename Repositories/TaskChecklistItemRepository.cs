using Microsoft.EntityFrameworkCore;
using Collaborative_Task_Management_System.Data;
using Collaborative_Task_Management_System.Models;

namespace Collaborative_Task_Management_System.Repositories
{
    public class TaskChecklistItemRepository : Repository<TaskChecklistItem>, ITaskChecklistItemRepository
    {
        public TaskChecklistItemRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<TaskChecklistItem>> GetChecklistItemsByTaskIdAsync(int taskId)
        {
            return await _dbSet
                .Where(item => item.TaskId == taskId)
                .OrderBy(item => item.Position)
                .ToListAsync();
        }

        public async Task<int> GetMaxPositionForTaskAsync(int taskId)
        {
            var maxPosition = await _dbSet
                .Where(item => item.TaskId == taskId)
                .MaxAsync(item => (int?)item.Position);

            return maxPosition ?? 0;
        }

        public async Task ReorderChecklistItemsAsync(int taskId, int oldPosition, int newPosition)
        {
            var items = await _dbSet
                .Where(item => item.TaskId == taskId)
                .OrderBy(item => item.Position)
                .ToListAsync();

            if (oldPosition < newPosition)
            {
                // Moving down: items between old+1 and new position move up one spot
                foreach (var item in items.Where(i => i.Position > oldPosition && i.Position <= newPosition))
                {
                    item.Position--;
                }
            }
            else if (oldPosition > newPosition)
            {
                // Moving up: items between new and old-1 position move down one spot
                foreach (var item in items.Where(i => i.Position >= newPosition && i.Position < oldPosition))
                {
                    item.Position++;
                }
            }

            // Set the moved item to its new position
            var movedItem = items.FirstOrDefault(i => i.Position == oldPosition);
            if (movedItem != null)
            {
                movedItem.Position = newPosition;
            }
        }

        public async Task ToggleCompletionStatusAsync(int itemId)
        {
            var item = await _dbSet.FindAsync(itemId);
            if (item != null)
            {
                item.IsCompleted = !item.IsCompleted;
            }
        }
    }
}