using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;

namespace Collaborative_Task_Management_System.Hubs
{
    [Authorize]
    public class NotificationHub : Hub
    {
        private readonly ILogger<NotificationHub> _logger;
        private readonly IMemoryCache _cache;
        private const string ThrottlePrefix = "NotificationThrottle_";
        private const int ThrottleSeconds = 1; // Minimum time between notifications

        public NotificationHub(ILogger<NotificationHub> logger, IMemoryCache cache)
        {
            _logger = logger;
            _cache = cache;
        }

        public override async Task OnConnectedAsync()
        {
            // Add user to their personal group (for targeted notifications)
            await Groups.AddToGroupAsync(Context.ConnectionId, Context.User.Identity.Name);
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            // Remove user from their personal group
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, Context.User.Identity.Name);
            await base.OnDisconnectedAsync(exception);
        }

        // Method to send notification to a specific user
        public async Task SendNotificationToUser(string userId, string message, string type)
        {
            await Clients.User(userId).SendAsync("ReceiveNotification", message, type);
        }

        // Method to send task notification
        public async Task SendTaskNotification(string userId, string message, int taskId, int projectId, string taskTitle)
        {
            try
            {
                var throttleKey = $"{ThrottlePrefix}{userId}_{taskId}";

                // Check if we've sent a notification recently
                if (!_cache.TryGetValue(throttleKey, out _))
                {
                    await Clients.User(userId).SendAsync("ReceiveTaskNotification",
                        new
                        {
                            message,
                            taskId,
                            projectId,
                            taskTitle,
                            timestamp = DateTime.UtcNow
                        });

                    // Set throttle cache
                    var cacheEntryOptions = new MemoryCacheEntryOptions()
                        .SetAbsoluteExpiration(TimeSpan.FromSeconds(ThrottleSeconds))
                        .SetPriority(CacheItemPriority.Low);

                    _cache.Set(throttleKey, true, cacheEntryOptions);

                    _logger.LogInformation(
                        "Task notification sent to user {UserId} for task {TaskId}",
                        userId, taskId);
                }
                else
                {
                    _logger.LogDebug(
                        "Task notification throttled for user {UserId} and task {TaskId}",
                        userId, taskId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error sending task notification to user {UserId} for task {TaskId}",
                    userId, taskId);
                throw;
            }
        }

        // Method to send comment notification
        public async Task SendCommentNotification(string userId, string taskTitle, string commenterName)
        {
            await Clients.User(userId).SendAsync("ReceiveCommentNotification", new
            {
                TaskTitle = taskTitle,
                CommenterName = commenterName,
                Timestamp = DateTime.UtcNow
            });
        }

        // Method to broadcast project update
        public async Task BroadcastProjectUpdate(string projectId, string message)
        {
            await Clients.Group($"Project_{projectId}").SendAsync("ReceiveProjectUpdate", message);
        }

        // Method to send notification to all users in a project
        public async Task SendNotificationToProject(string projectId, string message, string type)
        {
            await Clients.Group($"Project_{projectId}").SendAsync("ReceiveNotification", message, type);
        }

        // Method to join a project group (called when user opens a project)
        public async Task JoinProjectGroup(string projectId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"Project_{projectId}");
        }

        // Method to leave a project group
        public async Task LeaveProjectGroup(string projectId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"Project_{projectId}");
        }
    }
}