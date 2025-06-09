using Collaborative_Task_Management_System.Models;

namespace Collaborative_Task_Management_System.Repositories
{
    public interface ITaskTimeEntryRepository : IRepository<TaskTimeEntry>
    {
        Task<IEnumerable<TaskTimeEntry>> GetTimeEntriesByTaskIdAsync(int taskId);
        Task<IEnumerable<TaskTimeEntry>> GetTimeEntriesByUserIdAsync(string userId);
        Task<IEnumerable<TaskTimeEntry>> GetTimeEntriesForDateRangeAsync(DateTime startDate, DateTime endDate);
        Task<TaskTimeEntry> GetActiveTimeEntryForUserAsync(string userId);
        Task EndTimeEntryAsync(int timeEntryId, DateTime endTime, string notes = null);
    }
}