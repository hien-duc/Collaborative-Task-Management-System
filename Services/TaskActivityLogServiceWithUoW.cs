using System.Text.Json;
using Collaborative_Task_Management_System.Models;
using Collaborative_Task_Management_System.UnitOfWork;
using TaskStatus = Collaborative_Task_Management_System.Models.TaskStatus;

namespace Collaborative_Task_Management_System.Services
{
    public class TaskActivityLogServiceWithUoW : ITaskActivityLogServiceWithUoW
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<TaskActivityLogServiceWithUoW> _logger;

        public TaskActivityLogServiceWithUoW(IUnitOfWork unitOfWork, ILogger<TaskActivityLogServiceWithUoW> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<List<TaskActivityLog>> GetAllActivityLogsAsync()
        {
            try
            {
                var logs = await _unitOfWork.TaskActivityLogs.GetAllAsync();
                return logs.ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all activity logs");
                throw;
            }
        }

        public async Task<TaskActivityLog> GetActivityLogByIdAsync(int id)
        {
            try
            {
                return await _unitOfWork.TaskActivityLogs.GetByIdAsync(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving activity log with ID: {LogId}", id);
                throw;
            }
        }

        public async Task<List<TaskActivityLog>> GetActivityLogsByTaskIdAsync(int taskId)
        {
            try
            {
                var logs = await _unitOfWork.TaskActivityLogs.GetActivityLogsByTaskIdAsync(taskId);
                return logs.ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving activity logs for task ID: {TaskId}", taskId);
                throw;
            }
        }

        public async Task<List<TaskActivityLog>> GetActivityLogsByUserIdAsync(string userId)
        {
            try
            {
                var logs = await _unitOfWork.TaskActivityLogs.GetActivityLogsByUserIdAsync(userId);
                return logs.ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving activity logs for user ID: {UserId}", userId);
                throw;
            }
        }

        public async Task<List<TaskActivityLog>> GetRecentActivityLogsAsync(int count = 50)
        {
            try
            {
                var logs = await _unitOfWork.TaskActivityLogs.GetRecentActivityLogsAsync(count);
                return logs.ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving recent activity logs");
                throw;
            }
        }

        public async Task<TaskActivityLog> CreateActivityLogAsync(TaskActivityLog activityLog)
        {
            try
            {
                // Set the timestamp if not already set
                if (activityLog.Timestamp == default)
                {
                    activityLog.Timestamp = DateTime.UtcNow;
                }

                await _unitOfWork.TaskActivityLogs.AddAsync(activityLog);
                await _unitOfWork.SaveChangesAsync();

                return activityLog;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating activity log");
                throw;
            }
        }

        public async Task<TaskActivityLog> LogTaskCreatedAsync(int taskId, string userId)
        {
            try
            {
                var task = await _unitOfWork.Tasks.GetByIdAsync(taskId);
                if (task == null)
                {
                    throw new KeyNotFoundException($"Task with ID {taskId} not found.");
                }

                var log = new TaskActivityLog
                {
                    TaskId = taskId,
                    UserId = userId,
                    Action = "Created",
                    Description = "A new task was created",
                    OldValues = "{}", // Empty JSON object
                    NewValues = JsonSerializer.Serialize(new 
                    {
                        TaskId = taskId,
                        Action = "Created",
                        Timestamp = DateTime.UtcNow
                    }),
                    Timestamp = DateTime.UtcNow
                };

                return await CreateActivityLogAsync(log);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error logging task created for task ID: {TaskId}", taskId);
                throw;
            }
        }

        public async Task<TaskActivityLog> LogTaskUpdatedAsync(int taskId, string userId, string changes)
        {
            try
            {
                var task = await _unitOfWork.Tasks.GetByIdAsync(taskId);
                if (task == null)
                {
                    throw new KeyNotFoundException($"Task with ID {taskId} not found.");
                }

                var log = new TaskActivityLog
                {
                    TaskId = taskId,
                    UserId = userId,
                    Action = "Updated",
                    Description = $"Task '{task.Title}' was updated",
                    NewValues = changes,
                    Timestamp = DateTime.UtcNow
                };

                return await CreateActivityLogAsync(log);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error logging task updated for task ID: {TaskId}", taskId);
                throw;
            }
        }

        public async Task<TaskActivityLog> LogTaskStatusChangedAsync(int taskId, string userId, TaskStatus oldStatus, TaskStatus newStatus)
        {
            try
            {
                var task = await _unitOfWork.Tasks.GetByIdAsync(taskId);
                if (task == null)
                {
                    throw new KeyNotFoundException($"Task with ID {taskId} not found.");
                }

                var log = new TaskActivityLog
                {
                    TaskId = taskId,
                    UserId = userId,
                    Action = "StatusChanged",
                    Description = $"Task '{task.Title}' status changed from {oldStatus} to {newStatus}",
                    OldValues = oldStatus.ToString(),
                    NewValues = newStatus.ToString(),
                    Timestamp = DateTime.UtcNow
                };

                return await CreateActivityLogAsync(log);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error logging task status change for task ID: {TaskId}", taskId);
                throw;
            }
        }

        public async Task<TaskActivityLog> LogTaskAssignedAsync(int taskId, string userId, string assignedToUserId)
        {
            try
            {
                var task = await _unitOfWork.Tasks.GetByIdAsync(taskId);
                if (task == null)
                {
                    throw new KeyNotFoundException($"Task with ID {taskId} not found.");
                }

                var assignedUser = await _unitOfWork.Repository<ApplicationUser>().GetByIdAsync(assignedToUserId);
                if (assignedUser == null)
                {
                    throw new KeyNotFoundException($"User with ID {assignedToUserId} not found.");
                }

                var log = new TaskActivityLog
                {
                    TaskId = taskId,
                    UserId = userId,
                    Action = "Assigned",
                    Description = $"Task '{task.Title}' was assigned to {assignedUser.UserName}",
                    NewValues = assignedToUserId,
                    Timestamp = DateTime.UtcNow
                };

                return await CreateActivityLogAsync(log);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error logging task assignment for task ID: {TaskId}", taskId);
                throw;
            }
        }

        public async Task<TaskActivityLog> LogTaskCommentAddedAsync(int taskId, string userId)
        {
            try
            {
                var task = await _unitOfWork.Tasks.GetByIdAsync(taskId);
                if (task == null)
                {
                    throw new KeyNotFoundException($"Task with ID {taskId} not found.");
                }

                var log = new TaskActivityLog
                {
                    TaskId = taskId,
                    UserId = userId,
                    Action = "CommentAdded",
                    Description = $"Comment added to task '{task.Title}'",
                    Timestamp = DateTime.UtcNow
                };

                return await CreateActivityLogAsync(log);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error logging comment added for task ID: {TaskId}", taskId);
                throw;
            }
        }

        public async Task<TaskActivityLog> LogTaskAttachmentAddedAsync(int taskId, string userId, string fileName)
        {
            try
            {
                var task = await _unitOfWork.Tasks.GetByIdAsync(taskId);
                if (task == null)
                {
                    throw new KeyNotFoundException($"Task with ID {taskId} not found.");
                }

                var log = new TaskActivityLog
                {
                    TaskId = taskId,
                    UserId = userId,
                    Action = "AttachmentAdded",
                    Description = $"File '{fileName}' attached to task '{task.Title}'",
                    NewValues = fileName,
                    Timestamp = DateTime.UtcNow
                };

                return await CreateActivityLogAsync(log);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error logging attachment added for task ID: {TaskId}", taskId);
                throw;
            }
        }
    }
}