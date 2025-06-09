using Collaborative_Task_Management_System.Models;

namespace Collaborative_Task_Management_System.Services
{
    public interface ITaskTimeEntryServiceWithUoW
    {
        Task<List<TaskTimeEntry>> GetAllTimeEntriesAsync();
        Task<TaskTimeEntry> GetTimeEntryByIdAsync(int id);
        Task<List<TaskTimeEntry>> GetTimeEntriesByTaskIdAsync(int taskId);
        Task<List<TaskTimeEntry>> GetTimeEntriesByUserIdAsync(string userId);
        Task<List<TaskTimeEntry>> GetTimeEntriesByDateRangeAsync(DateTime startDate, DateTime endDate);
        Task<TaskTimeEntry> StartTimeEntryAsync(int taskId, string userId);
        Task<TaskTimeEntry> StopTimeEntryAsync(int timeEntryId);
        Task<TaskTimeEntry> CreateManualTimeEntryAsync(TaskTimeEntry timeEntry);
        Task<TaskTimeEntry> UpdateTimeEntryAsync(TaskTimeEntry timeEntry);
        Task DeleteTimeEntryAsync(int id);
        Task<double> GetTotalTimeForTaskAsync(int taskId);
        Task<double> GetTotalTimeForUserAsync(string userId, DateTime? startDate = null, DateTime? endDate = null);
    }
}