using Collaborative_Task_Management_System.Models;
using Collaborative_Task_Management_System.UnitOfWork;

namespace Collaborative_Task_Management_System.Services
{
    public interface INotificationServiceWithUoW
    {
        Task<List<Notification>> GetAllNotificationsAsync();
        Task<Notification> GetNotificationByIdAsync(int id);
        Task<List<Notification>> GetNotificationsByUserIdAsync(string userId);
        Task<List<Notification>> GetUnreadNotificationsByUserIdAsync(string userId);
        Task<List<Notification>> GetNotificationsByTypeAsync(NotificationType type);
        Task<List<Notification>> GetRecentNotificationsAsync(int count = 10);
        Task<Notification> CreateNotificationAsync(Notification notification);
        Task<Notification> UpdateNotificationAsync(Notification notification);
        Task MarkAsReadAsync(int notificationId);
        Task MarkAllAsReadAsync(string userId);
        Task DeleteNotificationAsync(int id);
        Task<bool> NotificationExistsAsync(int id);
        Task<int> GetUnreadCountAsync(string userId);
        Task CreateTaskNotificationAsync(string userId, string title, string message, NotificationType type, int? taskId = null, int? projectId = null);
        Task CreateAuditLogAsync(string userId, string action, string details, string? ipAddress);
        Task SendTaskCommentNotificationAsync(Comment comment);
        Task SendTaskAssignmentNotificationAsync(TaskItem task);
        Task SendTaskStatusUpdateNotificationAsync(TaskItem task, string updatedByUserId);
    }

    public class NotificationServiceWithUoW : INotificationServiceWithUoW
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<NotificationServiceWithUoW> _logger;

        public NotificationServiceWithUoW(IUnitOfWork unitOfWork, ILogger<NotificationServiceWithUoW> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<List<Notification>> GetAllNotificationsAsync()
        {
            try
            {
                var notifications = await _unitOfWork.Notifications.GetAllWithIncludesAsync(
                    n => n.User,
                    n => n.Task,
                    n => n.Project);
                return notifications.OrderByDescending(n => n.CreatedAt).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all notifications");
                throw;
            }
        }

        public async Task<Notification> GetNotificationByIdAsync(int id)
        {
            try
            {
                return await _unitOfWork.Notifications.GetByIdWithIncludesAsync(id,
                    n => n.User,
                    n => n.Task,
                    n => n.Project);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving notification with ID: {NotificationId}", id);
                throw;
            }
        }

        public async Task<List<Notification>> GetNotificationsByUserIdAsync(string userId)
        {
            try
            {
                var notifications = await _unitOfWork.Notifications.GetNotificationsByUserIdAsync(userId);
                return notifications.ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving notifications for user: {UserId}", userId);
                throw;
            }
        }

        public async Task<List<Notification>> GetUnreadNotificationsByUserIdAsync(string userId)
        {
            try
            {
                var notifications = await _unitOfWork.Notifications.GetUnreadNotificationsByUserIdAsync(userId);
                return notifications.ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving unread notifications for user: {UserId}", userId);
                throw;
            }
        }

        public async Task<List<Notification>> GetNotificationsByTypeAsync(NotificationType type)
        {
            try
            {
                var notifications = await _unitOfWork.Notifications.GetNotificationsByTypeAsync(type);
                return notifications.ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving notifications by type: {Type}", type);
                throw;
            }
        }

        public async Task<List<Notification>> GetRecentNotificationsAsync(int count = 10)
        {
            try
            {
                var notifications = await _unitOfWork.Notifications.GetRecentNotificationsAsync(count.ToString());
                return notifications.ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving recent notifications");
                throw;
            }
        }

        public async Task<Notification> CreateNotificationAsync(Notification notification)
        {
            try
            {
                notification.CreatedAt = DateTime.UtcNow;
                notification.IsRead = false;
                
                await _unitOfWork.Notifications.AddAsync(notification);
                await _unitOfWork.SaveChangesAsync();
                
                _logger.LogInformation("Notification created: {NotificationId} for user {UserId}", 
                    notification.Id, notification.UserId);
                
                return notification;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating notification");
                throw;
            }
        }

        public async Task<Notification> UpdateNotificationAsync(Notification notification)
        {
            try
            {
                var existingNotification = await _unitOfWork.Notifications.GetByIdAsync(notification.Id);
                if (existingNotification == null)
                {
                    throw new KeyNotFoundException($"Notification with ID {notification.Id} not found");
                }

                existingNotification.Title = notification.Title;
                existingNotification.Message = notification.Message;
                existingNotification.Type = notification.Type;
                existingNotification.IsRead = notification.IsRead;
                // ReadAt property is not defined in Notification model, removing this line

                _unitOfWork.Notifications.Update(existingNotification);
                await _unitOfWork.SaveChangesAsync();
                
                _logger.LogInformation("Notification updated: {NotificationId}", notification.Id);
                
                return existingNotification;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating notification {NotificationId}", notification.Id);
                throw;
            }
        }

        public async Task MarkAsReadAsync(int notificationId)
        {
            try
            {
                await _unitOfWork.Notifications.MarkAsReadAsync(notificationId);
                await _unitOfWork.SaveChangesAsync();
                
                _logger.LogInformation("Notification marked as read: {NotificationId}", notificationId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking notification as read: {NotificationId}", notificationId);
                throw;
            }
        }

        public async Task MarkAllAsReadAsync(string userId)
        {
            try
            {
                await _unitOfWork.Notifications.MarkAllAsReadAsync(userId);
                await _unitOfWork.SaveChangesAsync();
                
                _logger.LogInformation("All notifications marked as read for user: {UserId}", userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking all notifications as read for user: {UserId}", userId);
                throw;
            }
        }

        public async Task DeleteNotificationAsync(int id)
        {
            try
            {
                var notification = await _unitOfWork.Notifications.GetByIdAsync(id);
                if (notification == null)
                {
                    throw new KeyNotFoundException($"Notification with ID {id} not found");
                }

                _unitOfWork.Notifications.Delete(notification);
                await _unitOfWork.SaveChangesAsync();
                
                _logger.LogInformation("Notification deleted: {NotificationId}", id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting notification {NotificationId}", id);
                throw;
            }
        }

        public async Task<bool> NotificationExistsAsync(int id)
        {
            try
            {
                return await _unitOfWork.Notifications.AnyAsync(n => n.Id == id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if notification exists: {NotificationId}", id);
                throw;
            }
        }

        public async Task<int> GetUnreadCountAsync(string userId)
        {
            try
            {
                var unreadNotifications = await _unitOfWork.Notifications.GetUnreadNotificationsByUserIdAsync(userId);
                return unreadNotifications.Count();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting unread count for user: {UserId}", userId);
                throw;
            }
        }

        public async Task CreateTaskNotificationAsync(string userId, string title, string message, NotificationType type, int? taskId = null, int? projectId = null)
        {
            try
            {
                var notification = new Notification
                {
                    UserId = userId,
                    Title = title,
                    Message = message,
                    Type = type,
                    TaskId = taskId,
                    ProjectId = projectId,
                    CreatedAt = DateTime.UtcNow,
                    IsRead = false
                };

                await _unitOfWork.Notifications.AddAsync(notification);
                await _unitOfWork.SaveChangesAsync();
                
                _logger.LogInformation("Task notification created for user: {UserId}, Type: {Type}", userId, type);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating task notification for user: {UserId}", userId);
                throw;
            }
        }

        public async Task CreateAuditLogAsync(string userId, string action, string details, string? ipAddress)
        {
            try
            {
                var auditLog = new AuditLog
                {
                    UserId = userId,
                    Action = action,
                    Details = details ?? "{}",
                    Timestamp = DateTime.UtcNow,
                    IpAddress = ipAddress
                };

                await _unitOfWork.AuditLogs.AddAsync(auditLog);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Audit log created: {Action} by user {UserId}", action, userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating audit log for user {UserId}", userId);
                throw;
            }
        }

        public async Task SendTaskCommentNotificationAsync(Comment comment)
        {
            try
            {
                // Get the task with related entities
                var task = await _unitOfWork.Tasks.GetByIdWithIncludesAsync(comment.TaskId, t => t.AssignedUser, t => t.Project);
                if (task == null) return;
                
                // Get the comment author - use Repository<ApplicationUser> instead of Users
                var commentAuthor = await _unitOfWork.Repository<ApplicationUser>().GetByIdAsync(comment.UserId);
                if (commentAuthor == null) return;
                
                // Notify the task assignee if they're not the commenter
                if (task.AssignedUser != null && task.AssignedUser.Id != comment.UserId)
                {
                    await CreateTaskNotificationAsync(
                        task.AssignedUser.Id,
                        "New Comment on Task",
                        $"{commentAuthor.FullName ?? commentAuthor.UserName} commented on task '{task.Title}'",
                        NotificationType.TaskCommented,
                        task.Id,
                        task.ProjectId);
                }
                
                // Get project members to notify them as well
                // ProjectId is not nullable, so we don't need to check HasValue
                var projectMembers = await _unitOfWork.Repository<ProjectMember>().FindAsync(pm => pm.ProjectId == task.ProjectId);
                foreach (var member in projectMembers)
                {
                    // Don't notify the commenter or the assignee (who was already notified)
                    if (member.UserId != comment.UserId && 
                        (task.AssignedUser == null || member.UserId != task.AssignedUser.Id))
                    {
                        await CreateTaskNotificationAsync(
                            member.UserId,
                            "New Comment on Task",
                            $"{commentAuthor.FullName ?? commentAuthor.UserName} commented on task '{task.Title}' in project '{task.Project.Title}'",
                            NotificationType.TaskCommented,
                            task.Id,
                            task.ProjectId);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending task comment notification for comment {CommentId}", comment.Id);
            }
        }

        public async Task SendTaskAssignmentNotificationAsync(TaskItem task)
        {
            try
            {
                if (!string.IsNullOrEmpty(task.AssignedToId))
                {
                    await CreateTaskNotificationAsync(
                        task.AssignedToId,
                        "Task Assigned",
                        $"You have been assigned to task '{task.Title}'",
                        NotificationType.TaskAssigned,
                        task.Id,
                        task.ProjectId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending task assignment notification for task {TaskId}", task.Id);
                throw;
            }
        }

        public async Task SendTaskStatusUpdateNotificationAsync(TaskItem task, string updatedByUserId)
        {
            try
            {
                if (!string.IsNullOrEmpty(task.AssignedToId) && task.AssignedToId != updatedByUserId)
                {
                    await CreateTaskNotificationAsync(
                        task.AssignedToId,
                        "Task Status Updated",
                        $"The status of task '{task.Title}' has been updated to {task.Status}",
                        NotificationType.TaskStatusChanged,
                        task.Id,
                        task.ProjectId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending task status update notification for task {TaskId}", task.Id);
                throw;
            }
        }
    }
}