using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Collaborative_Task_Management_System.Models;
using Collaborative_Task_Management_System.Models.ViewModels;
using Collaborative_Task_Management_System.Services;
using Collaborative_Task_Management_System.Data;
using Collaborative_Task_Management_System.Hubs;

namespace Collaborative_Task_Management_System.Controllers
{
    [Authorize(Policy = "TeamMemberOrHigher")]
    public class CommentsController : BaseController
    {
        private readonly ITaskServiceWithUoW _taskService;
        private readonly INotificationServiceWithUoW _notificationService;
        private readonly ILogger<CommentsController> _logger;
        private readonly IHubContext<NotificationHub> _hubContext;

        private readonly ApplicationDbContext _context;

        public CommentsController(
            ITaskServiceWithUoW taskService,
            INotificationServiceWithUoW notificationService,
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IHubContext<NotificationHub> hubContext,
            ILogger<CommentsController> logger)
            : base(userManager)
        {
            _taskService = taskService;
            _notificationService = notificationService;
            _context = context;
            _hubContext = hubContext;
            _logger = logger;
        }

        // POST: Comments/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CommentCreateViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var userId = GetCurrentUserId();
                var task = await _taskService.GetTaskByIdAsync(model.TaskId);

                if (task == null)
                {
                    return NotFound();
                }

                var comment = new Comment
                {
                    TaskId = model.TaskId,
                    Text = model.Content,
                    UserId = userId,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Comments.Add(comment);

                // Create audit log
                var auditLog = new AuditLog
                {
                    UserId = userId,
                    Action = "CommentCreated",
                    Details = $"Added comment to task '{task.Title}' (Task ID: {task.Id}, Project ID: {task.ProjectId})",
                    Timestamp = DateTime.UtcNow
                };
                _context.AuditLogs.Add(auditLog);

                await _context.SaveChangesAsync();

                // Send notification to task assignee and project members
                await _notificationService.SendTaskCommentNotificationAsync(comment);
                
                // Get current user for the real-time notification
                var currentUser = await _userManager.FindByIdAsync(userId);
                string userName = currentUser?.FullName ?? currentUser?.UserName ?? "Unknown User";
                
                // Broadcast the comment to all connected clients
                await _hubContext.Clients.All.SendAsync("CommentAdded", task.Id, new
                {
                    taskId = task.Id,
                    projectId = task.ProjectId,
                    text = comment.Text,
                    authorName = userName,
                    timestamp = comment.CreatedAt
                });

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating comment for task {TaskId}", model.TaskId);
                return Json(new { success = false, message = "An error occurred while creating the comment." });
            }
        }
    }
}