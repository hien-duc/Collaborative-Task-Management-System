using Collaborative_Task_Management_System.Models;
using Collaborative_Task_Management_System.UnitOfWork;
using TaskStatus = Collaborative_Task_Management_System.Models.TaskStatus;

namespace Collaborative_Task_Management_System.Services
{
    public interface ITaskServiceWithUoW
    {
        Task<List<TaskItem>> GetAllTasksAsync();
        Task<TaskItem> GetTaskByIdAsync(int id);
        Task<TaskItem> GetTaskWithDetailsAsync(int id);
        Task<List<TaskItem>> GetTasksByProjectIdAsync(int projectId);
        Task<List<TaskItem>> GetTasksByAssignedUserAsync(string userId);
        Task<List<TaskItem>> GetTasksByStatusAsync(TaskStatus status);
        Task<List<TaskItem>> GetOverdueTasksAsync();
        Task<List<TaskItem>> GetTasksDueSoonAsync(int days = 7);
        Task<TaskItem> CreateTaskAsync(TaskItem task);
        Task<TaskItem> UpdateTaskAsync(TaskItem task);
        Task<TaskItem> UpdateTaskStatusAsync(int taskId, TaskStatus status, string userId);
        Task DeleteTaskAsync(int id);
        Task<bool> TaskExistsAsync(int id);
        Task<List<TaskItem>> SearchTasksAsync(string searchTerm);
        Task AssignTaskAsync(int taskId, string userId, string assignedByUserId);
        Task SaveFileAttachmentAsync(int taskId, IFormFile file, string uploadedByUserId);
        Task<FileAttachment> GetFileAttachmentAsync(int attachmentId);
    }

    public class TaskServiceWithUoW : ITaskServiceWithUoW
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<TaskServiceWithUoW> _logger;

        public TaskServiceWithUoW(IUnitOfWork unitOfWork, ILogger<TaskServiceWithUoW> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<List<TaskItem>> GetAllTasksAsync()
        {
            try
            {
                var tasks = await _unitOfWork.Tasks.GetAllWithIncludesAsync(
                    t => t.AssignedUser,
                    t => t.CreatedBy,
                    t => t.Project);
                return tasks.OrderByDescending(t => t.CreatedAt).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all tasks");
                throw;
            }
        }

        public async Task<TaskItem> GetTaskByIdAsync(int id)
        {
            try
            {
                return await _unitOfWork.Tasks.GetByIdWithIncludesAsync(id,
                    t => t.AssignedUser,
                    t => t.CreatedBy,
                    t => t.Project);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving task with ID: {TaskId}", id);
                throw;
            }
        }

        public async Task<TaskItem> GetTaskWithDetailsAsync(int id)
        {
            try
            {
                return await _unitOfWork.Tasks.GetTaskWithAllDetailsAsync(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving task details for ID: {TaskId}", id);
                throw;
            }
        }

        public async Task<List<TaskItem>> GetTasksByProjectIdAsync(int projectId)
        {
            try
            {
                var tasks = await _unitOfWork.Tasks.GetTasksByProjectIdAsync(projectId);
                return tasks.ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving tasks for project: {ProjectId}", projectId);
                throw;
            }
        }

        public async Task<List<TaskItem>> GetTasksByAssignedUserAsync(string userId)
        {
            try
            {
                var tasks = await _unitOfWork.Tasks.GetTasksByAssignedUserAsync(userId);
                return tasks.ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving tasks for user: {UserId}", userId);
                throw;
            }
        }

        public async Task<List<TaskItem>> GetTasksByStatusAsync(TaskStatus status)
        {
            try
            {
                var tasks = await _unitOfWork.Tasks.GetTasksByStatusAsync(status);
                return tasks.ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving tasks with status: {Status}", status);
                throw;
            }
        }

        public async Task<List<TaskItem>> GetOverdueTasksAsync()
        {
            try
            {
                var tasks = await _unitOfWork.Tasks.GetOverdueTasksAsync();
                return tasks.ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving overdue tasks");
                throw;
            }
        }

        public async Task<List<TaskItem>> GetTasksDueSoonAsync(int days = 7)
        {
            try
            {
                var tasks = await _unitOfWork.Tasks.GetTasksDueSoonAsync(days);
                return tasks.ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving tasks due soon");
                throw;
            }
        }

        public async Task<TaskItem> CreateTaskAsync(TaskItem task)
        {
            try
            {
                await _unitOfWork.BeginTransactionAsync();
                
                task.CreatedAt = DateTime.UtcNow;
                await _unitOfWork.Tasks.AddAsync(task);
                await _unitOfWork.SaveChangesAsync();
                
                // Create notification if task is assigned
                if (!string.IsNullOrEmpty(task.AssignedUserId))
                {
                    var notification = new Notification
                    {
                        UserId = task.AssignedUserId,
                        Title = "New Task Assigned",
                        Message = $"You have been assigned a new task: {task.Title}",
                        Type = NotificationType.TaskAssigned,
                        TaskId = task.Id,
                        ProjectId = task.ProjectId,
                        CreatedAt = DateTime.UtcNow,
                        IsRead = false
                    };
                    await _unitOfWork.Notifications.AddAsync(notification);
                }
                
                // Log the creation
                var auditLog = new AuditLog
                {
                    UserId = task.CreatedById,
                    Action = "Task Created",
                    Details = $"Created task: {task.Title}",
                    Timestamp = DateTime.UtcNow
                };
                await _unitOfWork.AuditLogs.AddAsync(auditLog);
                
                await _unitOfWork.CommitTransactionAsync();
                
                _logger.LogInformation("Task created: {TaskId} by user {UserId}", 
                    task.Id, task.CreatedById);
                
                return task;
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError(ex, "Error creating task");
                throw;
            }
        }

        public async Task<TaskItem> UpdateTaskAsync(TaskItem task)
        {
            try
            {
                await _unitOfWork.BeginTransactionAsync();
                
                var existingTask = await _unitOfWork.Tasks.GetByIdAsync(task.Id);
                if (existingTask == null)
                {
                    throw new KeyNotFoundException($"Task with ID {task.Id} not found");
                }

                var oldAssignedUserId = existingTask.AssignedUserId;
                var oldStatus = existingTask.Status;

                existingTask.Title = task.Title;
                existingTask.Description = task.Description;
                existingTask.DueDate = task.DueDate;
                existingTask.Priority = task.Priority;
                existingTask.Status = task.Status;
                existingTask.AssignedUserId = task.AssignedUserId;

                _unitOfWork.Tasks.Update(existingTask);
                await _unitOfWork.SaveChangesAsync();
                
                // Create notifications for assignment changes
                if (oldAssignedUserId != task.AssignedUserId && !string.IsNullOrEmpty(task.AssignedUserId))
                {
                    var notification = new Notification
                    {
                        UserId = task.AssignedUserId,
                        Title = "Task Assigned",
                        Message = $"You have been assigned to task: {task.Title}",
                        Type = NotificationType.TaskAssigned,
                        TaskId = task.Id,
                        ProjectId = task.ProjectId,
                        CreatedAt = DateTime.UtcNow,
                        IsRead = false
                    };
                    await _unitOfWork.Notifications.AddAsync(notification);
                }
                
                // Create notifications for status changes
                if (oldStatus != task.Status)
                {
                    if (!string.IsNullOrEmpty(task.AssignedUserId))
                    {
                        var notification = new Notification
                        {
                            UserId = task.AssignedUserId,
                            Title = "Task Status Updated",
                            Message = $"Task '{task.Title}' status changed to {task.Status}",
                            Type = NotificationType.TaskStatusChanged,
                            TaskId = task.Id,
                            ProjectId = task.ProjectId,
                            CreatedAt = DateTime.UtcNow,
                            IsRead = false
                        };
                        await _unitOfWork.Notifications.AddAsync(notification);
                    }
                }
                
                // Log the update
                var auditLog = new AuditLog
                {
                    UserId = task.CreatedById, // You might want to pass the current user ID
                    Action = "Task Updated",
                    Details = $"Updated task: {task.Title}",
                    Timestamp = DateTime.UtcNow
                };
                await _unitOfWork.AuditLogs.AddAsync(auditLog);
                
                await _unitOfWork.CommitTransactionAsync();
                
                _logger.LogInformation("Task updated: {TaskId}", task.Id);
                
                return existingTask;
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError(ex, "Error updating task {TaskId}", task.Id);
                throw;
            }
        }

        public async Task<TaskItem> UpdateTaskStatusAsync(int taskId, TaskStatus status, string userId)
        {
            try
            {
                await _unitOfWork.BeginTransactionAsync();
                
                var task = await _unitOfWork.Tasks.GetByIdAsync(taskId);
                if (task == null)
                {
                    throw new KeyNotFoundException($"Task with ID {taskId} not found");
                }

                var oldStatus = task.Status;
                task.Status = status;

                _unitOfWork.Tasks.Update(task);
                await _unitOfWork.SaveChangesAsync();
                
                // Create notification for status change
                if (!string.IsNullOrEmpty(task.AssignedUserId) && task.AssignedUserId != userId)
                {
                    var notification = new Notification
                    {
                        UserId = task.AssignedUserId,
                        Title = "Task Status Updated",
                        Message = $"Task '{task.Title}' status changed to {status}",
                        Type = NotificationType.TaskStatusChanged,
                        TaskId = taskId,
                        ProjectId = task.ProjectId,
                        CreatedAt = DateTime.UtcNow,
                        IsRead = false
                    };
                    await _unitOfWork.Notifications.AddAsync(notification);
                }
                
                // Log the status change
                var auditLog = new AuditLog
                {
                    UserId = userId,
                    Action = "Task Status Changed",
                    Details = $"Changed task '{task.Title}' status from {oldStatus} to {status}",
                    Timestamp = DateTime.UtcNow
                };
                await _unitOfWork.AuditLogs.AddAsync(auditLog);
                
                await _unitOfWork.CommitTransactionAsync();
                
                _logger.LogInformation("Task status updated: {TaskId} to {Status} by {UserId}", 
                    taskId, status, userId);
                
                return task;
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError(ex, "Error updating task status {TaskId}", taskId);
                throw;
            }
        }

        public async Task DeleteTaskAsync(int id)
        {
            try
            {
                await _unitOfWork.BeginTransactionAsync();
                
                var task = await _unitOfWork.Tasks.GetByIdAsync(id);
                if (task == null)
                {
                    throw new KeyNotFoundException($"Task with ID {id} not found");
                }

                _unitOfWork.Tasks.Delete(task);
                await _unitOfWork.SaveChangesAsync();
                
                // Log the deletion
                var auditLog = new AuditLog
                {
                    UserId = task.CreatedById,
                    Action = "Task Deleted",
                    Details = $"Deleted task: {task.Title}",
                    Timestamp = DateTime.UtcNow
                };
                await _unitOfWork.AuditLogs.AddAsync(auditLog);
                
                await _unitOfWork.CommitTransactionAsync();
                
                _logger.LogInformation("Task deleted: {TaskId}", id);
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError(ex, "Error deleting task {TaskId}", id);
                throw;
            }
        }

        public async Task<bool> TaskExistsAsync(int id)
        {
            try
            {
                return await _unitOfWork.Tasks.AnyAsync(t => t.Id == id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if task exists: {TaskId}", id);
                throw;
            }
        }

        public async Task<List<TaskItem>> SearchTasksAsync(string searchTerm)
        {
            try
            {
                var tasks = await _unitOfWork.Tasks.SearchTasksAsync(searchTerm);
                return tasks.ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching tasks with term: {SearchTerm}", searchTerm);
                throw;
            }
        }

        public async Task AssignTaskAsync(int taskId, string userId, string assignedByUserId)
        {
            try
            {
                await _unitOfWork.BeginTransactionAsync();
                
                var task = await _unitOfWork.Tasks.GetByIdAsync(taskId);
                if (task == null)
                {
                    throw new KeyNotFoundException($"Task with ID {taskId} not found");
                }

                var oldAssignedUserId = task.AssignedUserId;
                task.AssignedUserId = userId;

                _unitOfWork.Tasks.Update(task);
                await _unitOfWork.SaveChangesAsync();
                
                // Create notification for assignment
                if (!string.IsNullOrEmpty(userId))
                {
                    var notification = new Notification
                    {
                        UserId = userId,
                        Title = "Task Assigned",
                        Message = $"You have been assigned to task: {task.Title}",
                        Type = NotificationType.TaskAssigned,
                        TaskId = taskId,
                        ProjectId = task.ProjectId,
                        CreatedAt = DateTime.UtcNow,
                        IsRead = false
                    };
                    await _unitOfWork.Notifications.AddAsync(notification);
                }
                
                // Log the assignment
                var auditLog = new AuditLog
                {
                    UserId = assignedByUserId,
                    Action = "Task Assigned",
                    Details = $"Assigned task '{task.Title}' to user {userId}",
                    Timestamp = DateTime.UtcNow
                };
                await _unitOfWork.AuditLogs.AddAsync(auditLog);
                
                await _unitOfWork.CommitTransactionAsync();
                
                _logger.LogInformation("Task assigned: {TaskId} to {UserId} by {AssignedByUserId}", 
                    taskId, userId, assignedByUserId);
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError(ex, "Error assigning task {TaskId}", taskId);
                throw;
            }
        }

        public async Task SaveFileAttachmentAsync(int taskId, IFormFile file, string uploadedByUserId)
        {
            try
            {
                if (file == null || file.Length == 0)
                    throw new ArgumentException("File is required");

                // Validate file size (10MB limit)
                if (file.Length > 10 * 1024 * 1024)
                    throw new ArgumentException("File size cannot exceed 10MB");

                // Create uploads directory if it doesn't exist
                var uploadsPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
                Directory.CreateDirectory(uploadsPath);

                // Generate unique filename
                var fileName = $"{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}";
                var filePath = Path.Combine(uploadsPath, fileName);

                // Save file to disk
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                // Create file attachment record
                var attachment = new FileAttachment
                {
                    TaskId = taskId,
                    FileName = file.FileName,
                    FilePath = $"/uploads/{fileName}",
                    UploadedById = uploadedByUserId,
                    UploadedAt = DateTime.UtcNow
                };

                await _unitOfWork.FileAttachments.AddAsync(attachment);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("File attachment saved: {FileName} for task {TaskId}", 
                    file.FileName, taskId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving file attachment for task {TaskId}", taskId);
                throw;
            }
        }

        public async Task<FileAttachment> GetFileAttachmentAsync(int attachmentId)
        {
            try
            {
                var attachment = await _unitOfWork.FileAttachments.GetByIdAsync(attachmentId);
                return attachment;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving file attachment {AttachmentId}", attachmentId);
                throw;
            }
        }
    }
}