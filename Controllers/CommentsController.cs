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
using Collaborative_Task_Management_System.UnitOfWork;

namespace Collaborative_Task_Management_System.Controllers
{
    [Authorize(Policy = "TeamMemberOrHigher")]
    public class CommentsController : BaseController
    {
        private readonly ITaskServiceWithUoW _taskService;
        private readonly INotificationServiceWithUoW _notificationService;
        private readonly ITaskActivityLogServiceWithUoW _taskActivityLogService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<CommentsController> _logger;

        public CommentsController(
            ITaskServiceWithUoW taskService,
            INotificationServiceWithUoW notificationService,
            ITaskActivityLogServiceWithUoW taskActivityLogService,
            IUnitOfWork unitOfWork,
            UserManager<ApplicationUser> userManager,
            ILogger<CommentsController> logger)
            : base(userManager)
        {
            _taskService = taskService;
            _notificationService = notificationService;
            _taskActivityLogService = taskActivityLogService;
            _unitOfWork = unitOfWork;
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

                // Use the repository through UnitOfWork instead of direct context access
                _unitOfWork.Comments.AddAsync(comment);
                await _unitOfWork.SaveChangesAsync();

                // Log comment activity
                await _taskActivityLogService.LogTaskCommentAddedAsync(model.TaskId, userId);

                // Create audit log
                await _notificationService.CreateAuditLogAsync(
                    userId,
                    "CommentCreated",
                    $"Added comment to task '{task.Title}' (Task ID: {task.Id}, Project ID: {task.ProjectId})");

                // Send notification to task assignee and creator
                await _notificationService.SendTaskCommentNotificationAsync(comment);

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