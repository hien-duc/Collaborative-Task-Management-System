using Collaborative_Task_Management_System.Models;
using Collaborative_Task_Management_System.UnitOfWork;

namespace Collaborative_Task_Management_System.Services
{
    public class TaskTimeEntryServiceWithUoW : ITaskTimeEntryServiceWithUoW
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<TaskTimeEntryServiceWithUoW> _logger;

        public TaskTimeEntryServiceWithUoW(IUnitOfWork unitOfWork, ILogger<TaskTimeEntryServiceWithUoW> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<List<TaskTimeEntry>> GetAllTimeEntriesAsync()
        {
            try
            {
                var timeEntries = await _unitOfWork.TaskTimeEntries.GetAllAsync();
                return timeEntries.ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all time entries");
                throw;
            }
        }

        public async Task<TaskTimeEntry> GetTimeEntryByIdAsync(int id)
        {
            try
            {
                return await _unitOfWork.TaskTimeEntries.GetByIdAsync(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving time entry with ID: {TimeEntryId}", id);
                throw;
            }
        }

        public async Task<List<TaskTimeEntry>> GetTimeEntriesByTaskIdAsync(int taskId)
        {
            try
            {
                var timeEntries = await _unitOfWork.TaskTimeEntries.GetTimeEntriesByTaskIdAsync(taskId);
                return timeEntries.ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving time entries for task ID: {TaskId}", taskId);
                throw;
            }
        }

        public async Task<List<TaskTimeEntry>> GetTimeEntriesByUserIdAsync(string userId)
        {
            try
            {
                var timeEntries = await _unitOfWork.TaskTimeEntries.GetTimeEntriesByUserIdAsync(userId);
                return timeEntries.ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving time entries for user ID: {UserId}", userId);
                throw;
            }
        }

        public async Task<List<TaskTimeEntry>> GetTimeEntriesByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            try
            {
                var timeEntries = await _unitOfWork.TaskTimeEntries.GetTimeEntriesForDateRangeAsync(startDate, endDate);
                return timeEntries.ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving time entries for date range: {StartDate} to {EndDate}", startDate, endDate);
                throw;
            }
        }

        public async Task<TaskTimeEntry> StartTimeEntryAsync(int taskId, string userId)
        {
            try
            {
                // Check if there's already an active time entry for this user
                var activeEntry = await _unitOfWork.TaskTimeEntries.GetActiveTimeEntryForUserAsync(userId);
                if (activeEntry != null)
                {
                    throw new InvalidOperationException("User already has an active time entry. Stop the current entry before starting a new one.");
                }

                // Create a new time entry
                var timeEntry = new TaskTimeEntry
                {
                    TaskId = taskId,
                    UserId = userId,
                    StartTime = DateTime.UtcNow
                };

                await _unitOfWork.TaskTimeEntries.AddAsync(timeEntry);
                await _unitOfWork.SaveChangesAsync();

                return timeEntry;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting time entry for task ID: {TaskId} and user ID: {UserId}", taskId, userId);
                throw;
            }
        }

        public async Task<TaskTimeEntry> StopTimeEntryAsync(int timeEntryId)
        {
            try
            {
                var timeEntry = await _unitOfWork.TaskTimeEntries.GetByIdAsync(timeEntryId);
                if (timeEntry == null)
                {
                    throw new KeyNotFoundException($"Time entry with ID {timeEntryId} not found.");
                }

                if (timeEntry.EndTime.HasValue)
                {
                    throw new InvalidOperationException("This time entry has already been stopped.");
                }

                await _unitOfWork.TaskTimeEntries.EndTimeEntryAsync(timeEntryId, DateTime.UtcNow);
                await _unitOfWork.SaveChangesAsync();

                // Refresh the time entry to get the updated values
                return await _unitOfWork.TaskTimeEntries.GetByIdAsync(timeEntryId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error stopping time entry with ID: {TimeEntryId}", timeEntryId);
                throw;
            }
        }

        public async Task<TaskTimeEntry> CreateManualTimeEntryAsync(TaskTimeEntry timeEntry)
        {
            try
            {
                // Validate the time entry
                if (timeEntry.StartTime >= timeEntry.EndTime)
                {
                    throw new ArgumentException("End time must be after start time.");
                }

                await _unitOfWork.TaskTimeEntries.AddAsync(timeEntry);
                await _unitOfWork.SaveChangesAsync();

                return timeEntry;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating manual time entry");
                throw;
            }
        }

        public async Task<TaskTimeEntry> UpdateTimeEntryAsync(TaskTimeEntry timeEntry)
        {
            try
            {
                // Validate the time entry
                if (timeEntry.EndTime.HasValue && timeEntry.StartTime >= timeEntry.EndTime.Value)
                {
                    throw new ArgumentException("End time must be after start time.");
                }

                _unitOfWork.TaskTimeEntries.Update(timeEntry);
                await _unitOfWork.SaveChangesAsync();

                return timeEntry;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating time entry with ID: {TimeEntryId}", timeEntry.Id);
                throw;
            }
        }

        public async Task DeleteTimeEntryAsync(int id)
        {
            try
            {
                await _unitOfWork.TaskTimeEntries.DeleteByIdAsync(id);
                await _unitOfWork.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting time entry with ID: {TimeEntryId}", id);
                throw;
            }
        }

        public async Task<double> GetTotalTimeForTaskAsync(int taskId)
        {
            try
            {
                var timeEntries = await _unitOfWork.TaskTimeEntries.GetTimeEntriesByTaskIdAsync(taskId);
                double totalHours = 0;

                foreach (var entry in timeEntries)
                {
                    if (entry.EndTime.HasValue)
                    {
                        totalHours += (entry.EndTime.Value - entry.StartTime).TotalHours;
                    }
                }

                return totalHours;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating total time for task ID: {TaskId}", taskId);
                throw;
            }
        }

        public async Task<double> GetTotalTimeForUserAsync(string userId, DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                var timeEntries = await _unitOfWork.TaskTimeEntries.GetTimeEntriesByUserIdAsync(userId);
                double totalHours = 0;

                foreach (var entry in timeEntries)
                {
                    if (entry.EndTime.HasValue)
                    {
                        // Apply date filters if provided
                        if ((startDate == null || entry.StartTime >= startDate) &&
                            (endDate == null || entry.EndTime <= endDate))
                        {
                            totalHours += (entry.EndTime.Value - entry.StartTime).TotalHours;
                        }
                    }
                }

                return totalHours;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating total time for user ID: {UserId}", userId);
                throw;
            }
        }
    }
}