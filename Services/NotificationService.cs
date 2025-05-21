using Microsoft.AspNetCore.SignalR;
using Collaborative_Task_Management_System.Data;
using Collaborative_Task_Management_System.Hubs;
using Collaborative_Task_Management_System.Models;

namespace Collaborative_Task_Management_System.Services
{
    public interface INotificationService
    {
        Task SendTaskAssignmentNotificationAsync(TaskItem task);
        Task SendTaskStatusUpdateNotificationAsync(TaskItem task, string updatedByUserId);
        Task SendTaskCommentNotificationAsync(Comment comment);
        Task CreateAuditLogAsync(string userId, string action, string details);
    }

    public class NotificationService : INotificationService
    {
        private readonly IHubContext<NotificationHub> _hubContext;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<NotificationService> _logger;

        public NotificationService(
            IHubContext<NotificationHub> hubContext,
            ApplicationDbContext context,
            ILogger<NotificationService> logger)
        {
            _hubContext = hubContext;
            _context = context;
            _logger = logger;
        }

        public async Task SendTaskAssignmentNotificationAsync(TaskItem task)
        {
            if (task.AssignedToId == null) return;

            var notification = new Notification
            {
                Title = "New Task Assignment",
                Message = $"You have been assigned to task: {task.Title}",
                UserId = task.AssignedToId,
                TaskId = task.Id,
                ProjectId = task.ProjectId,
                Type = NotificationType.TaskAssigned,
                CreatedAt = DateTime.UtcNow
            };

            await SaveAndSendNotificationAsync(notification);
        }

        public async Task SendTaskStatusUpdateNotificationAsync(TaskItem task, string updatedByUserId)
        {
            if (task.CreatedById == null) return;

            var notification = new Notification
            {
                Title = "Task Status Updated",
                Message = $"Task '{task.Title}' status has been updated to {task.Status}",
                UserId = task.CreatedById,
                TaskId = task.Id,
                ProjectId = task.ProjectId,
                Type = NotificationType.TaskStatusChanged,
                CreatedAt = DateTime.UtcNow
            };

            await SaveAndSendNotificationAsync(notification);

            // Create audit log
            await CreateAuditLogAsync(
                updatedByUserId,
                "TaskStatusUpdate",
                $"Updated status of task {task.Id} to {task.Status}");
        }

        public async Task SendTaskCommentNotificationAsync(Comment comment)
        {
            var task = await _context.Tasks
                .Include(t => t.AssignedTo)
                .FirstOrDefaultAsync(t => t.Id == comment.TaskId);

            if (task?.AssignedToId == null) return;

            var notification = new Notification
            {
                Title = "New Comment",
                Message = $"New comment on task: {task.Title}",
                UserId = task.AssignedToId,
                TaskId = task.Id,
                ProjectId = task.ProjectId,
                Type = NotificationType.TaskCommented,
                CreatedAt = DateTime.UtcNow
            };

            await SaveAndSendNotificationAsync(notification);
        }

        public async Task CreateAuditLogAsync(string userId, string action, string details)
        {
            var auditLog = new AuditLog
            {
                UserId = userId,
                Action = action,
                Details = details,
                Timestamp = DateTime.UtcNow
            };

            _context.AuditLogs.Add(auditLog);
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Audit log created: {Action} by user {UserId} - {Details}",
                action, userId, details);
        }

        private async Task SaveAndSendNotificationAsync(Notification notification)
        {
            try
            {
                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync();

                await _hubContext.Clients.User(notification.UserId)
                    .SendAsync("ReceiveNotification", notification);

                _logger.LogInformation(
                    "Notification sent to user {UserId}: {Title}",
                    notification.UserId, notification.Title);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error sending notification to user {UserId}",
                    notification.UserId);
            }
        }
    }
}