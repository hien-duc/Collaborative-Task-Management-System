using Microsoft.EntityFrameworkCore;
using Collaborative_Task_Management_System.Data;
using Collaborative_Task_Management_System.Models;

namespace Collaborative_Task_Management_System.Repositories
{
    public class TaskActivityLogRepository : Repository<TaskActivityLog>, ITaskActivityLogRepository
    {
        public TaskActivityLogRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<TaskActivityLog>> GetActivityLogsByTaskIdAsync(int taskId)
        {
            return await _dbSet
                .Where(log => log.TaskId == taskId)
                .Include(log => log.User)
                .OrderByDescending(log => log.Timestamp)
                .ToListAsync();
        }

        public async Task<IEnumerable<TaskActivityLog>> GetActivityLogsByUserIdAsync(string userId)
        {
            return await _dbSet
                .Where(log => log.UserId == userId)
                .Include(log => log.Task)
                .OrderByDescending(log => log.Timestamp)
                .ToListAsync();
        }

        public async Task<IEnumerable<TaskActivityLog>> GetRecentActivityLogsAsync(int count = 20)
        {
            return await _dbSet
                .Include(log => log.Task)
                .Include(log => log.User)
                .OrderByDescending(log => log.Timestamp)
                .Take(count)
                .ToListAsync();
        }

        public async Task<IEnumerable<TaskActivityLog>> GetActivityLogsByActionAsync(string action)
        {
            return await _dbSet
                .Where(log => log.Action == action)
                .Include(log => log.Task)
                .Include(log => log.User)
                .OrderByDescending(log => log.Timestamp)
                .ToListAsync();
        }
    }
}