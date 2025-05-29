using Collaborative_Task_Management_System.Models;

namespace Collaborative_Task_Management_System.Services;

public interface INotificationServiceWithUoW
{
    Task<List<Notification>> GetAllNotificationsAsync();
    Task<Notification> GetNotificationByIdAsync(int id);
    Task<List<Notification>> GetNotificationsByUserIdAsync(string userId);
    Task<List<Notification>> GetUnreadNotificationsByUserIdAsync(string userId);
    Task<List<Notification>> GetNotificationsByTypeAsync(NotificationType type);
    Task<List<Notification>> GetRecentNotificationsAsync(string userId);
    Task<Notification> CreateNotificationAsync(Notification notification);
    Task<Notification> UpdateNotificationAsync(Notification notification);
    Task MarkAsReadAsync(int notificationId);
    Task MarkAllAsReadAsync(string userId);
    Task DeleteNotificationAsync(int id);
    Task<bool> NotificationExistsAsync(int id);
    Task<int> GetUnreadCountAsync(string userId);
    Task CreateTaskNotificationAsync(string userId, string title, string message, NotificationType type, int? taskId = null, int? projectId = null);
    Task CreateAuditLogAsync(string userId, string action, string details);
    Task SendTaskCommentNotificationAsync(Comment comment);
}