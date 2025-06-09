using Microsoft.EntityFrameworkCore;
using Collaborative_Task_Management_System.Data;
using Collaborative_Task_Management_System.Models;

namespace Collaborative_Task_Management_System.Repositories
{
    public class TaskTagRepository : Repository<TaskTag>, ITaskTagRepository
    {
        public TaskTagRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<TaskTag>> GetTaskTagsByTaskIdAsync(int taskId)
        {
            return await _dbSet
                .Where(tt => tt.TaskId == taskId)
                .Include(tt => tt.Tag)
                .ToListAsync();
        }

        public async Task<IEnumerable<TaskTag>> GetTaskTagsByTagIdAsync(int tagId)
        {
            return await _dbSet
                .Where(tt => tt.TagId == tagId)
                .Include(tt => tt.Task)
                .ToListAsync();
        }

        public async Task<bool> AddTaskTagAsync(int taskId, int tagId)
        {
            var existingTaskTag = await _dbSet
                .FirstOrDefaultAsync(tt => tt.TaskId == taskId && tt.TagId == tagId);

            if (existingTaskTag != null)
                return false; // Already exists

            var taskTag = new TaskTag
            {
                TaskId = taskId,
                TagId = tagId
            };

            await _dbSet.AddAsync(taskTag);
            return true;
        }

        public async Task RemoveTaskTagAsync(int taskId, int tagId)
        {
            var taskTag = await _dbSet
                .FirstOrDefaultAsync(tt => tt.TaskId == taskId && tt.TagId == tagId);

            if (taskTag != null)
                _dbSet.Remove(taskTag);
        }
    }
}