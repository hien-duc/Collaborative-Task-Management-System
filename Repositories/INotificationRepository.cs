using Collaborative_Task_Management_System.Models;

namespace Collaborative_Task_Management_System.Repositories
{
    public interface INotificationRepository : IRepository<Notification>
    {
        Task<IEnumerable<Notification>> GetNotificationsByUserIdAsync(string userId);
        Task<IEnumerable<Notification>> GetUnreadNotificationsByUserIdAsync(string userId);
        Task<int> GetUnreadNotificationCountAsync(string userId);
        Task<IEnumerable<Notification>> GetNotificationsByTypeAsync(NotificationType type);
        Task MarkAsReadAsync(int notificationId);
        Task MarkAllAsReadAsync(string userId);
        Task<IEnumerable<Notification>> GetRecentNotificationsAsync(string userId, int count = 10);
    }
}