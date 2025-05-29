using Collaborative_Task_Management_System.Models;
using Collaborative_Task_Management_System.Services;
using Collaborative_Task_Management_System.UnitOfWork;

namespace Collaborative_Task_Management_System.Services
{
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

        public async Task<List<Notification>> GetRecentNotificationsAsync(string userId)
        {
            try
            {
                var notifications = await _unitOfWork.Notifications.GetRecentNotificationsAsync(userId);
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

        public async Task CreateAuditLogAsync(string userId, string action, string details)
        {
            try
            {
                var auditLog = new AuditLog
                {
                    UserId = userId,
                    Action = action,
                    Details = details,
                    Timestamp = DateTime.UtcNow
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
                await _unitOfWork.Comments.AddAsync(comment);
                await _unitOfWork.SaveChangesAsync();
                _logger.LogInformation("Comment created by user {UserId}", comment.UserId);;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating audit log for user {UserId}", comment.UserId);
                throw;
            }
        }
    }
}