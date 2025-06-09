using Microsoft.EntityFrameworkCore;
using Collaborative_Task_Management_System.Data;
using Collaborative_Task_Management_System.Models;

namespace Collaborative_Task_Management_System.Repositories
{
    public class TaskTimeEntryRepository : Repository<TaskTimeEntry>, ITaskTimeEntryRepository
    {
        public TaskTimeEntryRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<TaskTimeEntry>> GetTimeEntriesByTaskIdAsync(int taskId)
        {
            return await _dbSet
                .Where(entry => entry.TaskId == taskId)
                .Include(entry => entry.User)
                .OrderByDescending(entry => entry.StartTime)
                .ToListAsync();
        }

        public async Task<IEnumerable<TaskTimeEntry>> GetTimeEntriesByUserIdAsync(string userId)
        {
            return await _dbSet
                .Where(entry => entry.UserId == userId)
                .Include(entry => entry.Task)
                .OrderByDescending(entry => entry.StartTime)
                .ToListAsync();
        }

        public async Task<IEnumerable<TaskTimeEntry>> GetTimeEntriesForDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            return await _dbSet
                .Where(entry => entry.StartTime >= startDate && 
                               (entry.EndTime == null || entry.EndTime <= endDate))
                .Include(entry => entry.Task)
                .Include(entry => entry.User)
                .OrderByDescending(entry => entry.StartTime)
                .ToListAsync();
        }

        public async Task<TaskTimeEntry> GetActiveTimeEntryForUserAsync(string userId)
        {
            return await _dbSet
                .Where(entry => entry.UserId == userId && entry.EndTime == null)
                .Include(entry => entry.Task)
                .FirstOrDefaultAsync();
        }

        public async Task EndTimeEntryAsync(int timeEntryId, DateTime endTime, string notes = null)
        {
            var timeEntry = await _dbSet.FindAsync(timeEntryId);
            if (timeEntry != null)
            {
                timeEntry.EndTime = endTime;
                if (!string.IsNullOrEmpty(notes))
                {
                    timeEntry.Notes = notes;
                }
            }
        }
    }
}