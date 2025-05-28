using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Collaborative_Task_Management_System.Models;
using Collaborative_Task_Management_System.Models.ViewModels;
using Collaborative_Task_Management_System.Services;

namespace Collaborative_Task_Management_System.Controllers
{
    [Authorize]
    public class TasksController : BaseController
    {
        private readonly ITaskServiceWithUoW _taskService;
        private readonly IProjectServiceWithUoW _projectService;
        private readonly INotificationServiceWithUoW _notificationService;
        private readonly ILogger<TasksController> _logger;

        public TasksController(
        ITaskServiceWithUoW taskService,
        IProjectServiceWithUoW projectService,
        INotificationServiceWithUoW notificationService,
        UserManager<ApplicationUser> userManager,
        ILogger<TasksController> logger)
        : base(userManager)
    {
        _taskService = taskService;
        _projectService = projectService;
        _notificationService = notificationService;
        _logger = logger;
    }

        // GET: Tasks/Create/5 (projectId)
        public async Task<IActionResult> Create(int? projectId)
        {
            if (projectId == null)
            {
                return NotFound();
            }

            var project = await _projectService.GetProjectByIdAsync(projectId.Value);
            if (project == null)
            {
                return NotFound();
            }

            ViewBag.ProjectId = projectId.Value;
            ViewBag.Users = await _userManager.Users.ToListAsync();
            return View();
        }

        // POST: Tasks/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind("ProjectId,Title,Description,DueDate,Priority,Status,AssignedToId")] TaskItem task,
            IFormFile attachment)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    task.CreatedById = GetCurrentUserId();
                    task.CreatedAt = DateTime.UtcNow;

                    var createdTask = await _taskService.CreateTaskAsync(task);

                    if (attachment != null)
                    {
                        await _taskService.SaveFileAttachmentAsync(
                            createdTask.Id, attachment, GetCurrentUserId());
                    }

                    // Create audit log
                    await _notificationService.CreateAuditLogAsync(
                        GetCurrentUserId(),
                        "TaskCreated",
                        $"Created task '{createdTask.Title}' in project {task.ProjectId}"
                    );

                    await _notificationService.SendTaskAssignmentNotificationAsync(createdTask);

                    return RedirectToAction("Details", "Projects", new { id = task.ProjectId });
                }

                ViewBag.ProjectId = task.ProjectId;
                ViewBag.Users = await _userManager.Users.ToListAsync();
                return View(task);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating task");
                ModelState.AddModelError("", "Error creating task. Please try again later.");
                ViewBag.ProjectId = task.ProjectId;
                ViewBag.Users = await _userManager.Users.ToListAsync();
                return View(task);
            }
        }

        // GET: Tasks/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var task = await _taskService.GetTaskByIdAsync(id.Value);
            if (task == null)
            {
                return NotFound();
            }

            ViewBag.Users = await _userManager.Users.ToListAsync();
            return View(task);
        }

        // POST: Tasks/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id,
            [Bind("Id,ProjectId,Title,Description,DueDate,Priority,Status,AssignedToId")] TaskItem task,
            IFormFile attachment)
        {
            if (id != task.Id)
            {
                return NotFound();
            }

            try
            {
                if (ModelState.IsValid)
                {
                    var updatedTask = await _taskService.UpdateTaskAsync(task);

                    if (attachment != null)
                    {
                        await _taskService.SaveFileAttachmentAsync(
                            updatedTask.Id, attachment, GetCurrentUserId());
                    }

                    await _notificationService.SendTaskAssignmentNotificationAsync(updatedTask);
                    await _notificationService.CreateAuditLogAsync(
                        GetCurrentUserId(),
                        "TaskUpdated",
                        $"Updated task {updatedTask.Id}");

                    return RedirectToAction("Details", "Projects", new { id = task.ProjectId });
                }

                ViewBag.Users = await _userManager.Users.ToListAsync();
                return View(task);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating task {TaskId}", id);
                ModelState.AddModelError("", "Error updating task. Please try again later.");
                ViewBag.Users = await _userManager.Users.ToListAsync();
                return View(task);
            }
        }

        // POST: Tasks/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                var task = await _taskService.GetTaskByIdAsync(id);
                if (task == null)
                {
                    return NotFound();
                }

                var projectId = task.ProjectId;
                await _taskService.DeleteTaskAsync(id);

                await _notificationService.CreateAuditLogAsync(
                    GetCurrentUserId(),
                    "TaskDeleted",
                    $"Deleted task {id}");

                return RedirectToAction("Details", "Projects", new { id = projectId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting task {TaskId}", id);
                return Problem("Error deleting task. Please try again later.");
            }
        }

        // POST: Tasks/UpdateStatus/5
        [HttpPost]
        public async Task<IActionResult> UpdateStatus(int id, TaskStatus status)
        {
            try
            {
                var task = await _taskService.GetTaskByIdAsync(id);
                if (task == null)
                {
                    return NotFound();
                }

                var currentUserId = GetCurrentUserId();
                if (task.AssignedToId != currentUserId && !await IsUserInRoleAsync("Manager"))
                {
                    return Forbid();
                }

                task = await _taskService.UpdateTaskStatusAsync(id, status, currentUserId);
                await _notificationService.SendTaskStatusUpdateNotificationAsync(task, currentUserId);

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating task status {TaskId}", id);
                return Json(new { success = false, message = "Error updating task status" });
            }
        }

        // GET: Tasks/DownloadFile/5
        public async Task<IActionResult> DownloadFile(int id)
        {
            try
            {
                var file = await _taskService.GetFileAttachmentAsync(id);
                if (file == null)
                {
                    return NotFound();
                }

                var filePath = file.FilePath;
                if (!System.IO.File.Exists(filePath))
                {
                    return NotFound();
                }

                return PhysicalFile(filePath, file.ContentType, file.FileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading file {FileId}", id);
                return Problem("Error downloading file. Please try again later.");
            }
        }

        // GET: Tasks/MyTasks
        public async Task<IActionResult> MyTasks()
        {
            try
            {
                var tasks = await _taskService.GetTasksByAssignedUserAsync(GetCurrentUserId());
                return View(tasks);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user tasks");
                return Problem("Error retrieving tasks. Please try again later.");
            }
        }

        // GET: Tasks
        public async Task<IActionResult> Index()
        {
            try
            {
                var userId = GetCurrentUserId();
                var tasks = await _taskService.GetTasksByAssignedUserAsync(userId);
                var viewModel = new TaskSearchViewModel
                {
                    Tasks = tasks,
                    TotalTasks = tasks.Count(),
                    PageSize = 10
                };
                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving tasks");
                return Problem("Error retrieving tasks. Please try again later.");
            }
        }

        // POST: Tasks/Search
        [HttpPost]
        public async Task<IActionResult> Search([FromBody] TaskSearchViewModel model)
        {
            try
            {
                var userId = GetCurrentUserId();
                var isManagerOrAdmin = await IsManagerOrAdmin();
                var tasks = await _taskService.GetAllTasksAsync();
                var query = tasks.AsQueryable();

                // Apply search filters
                if (!string.IsNullOrEmpty(model.Query))
                {
                    query = query.Where(t =>
                        t.Title.Contains(model.Query) ||
                        t.Description.Contains(model.Query) ||
                        t.AssignedTo.UserName.Contains(model.Query));
                }

                if (!string.IsNullOrEmpty(model.AssigneeId))
                {
                    query = query.Where(t => t.AssignedToId == model.AssigneeId);
                }

                if (model.Status.HasValue)
                {
                    query = query.Where(t => t.Status == model.Status.Value);
                }

                if (model.FromDate.HasValue)
                {
                    query = query.Where(t => t.DueDate >= model.FromDate.Value);
                }

                if (model.ToDate.HasValue)
                {
                    query = query.Where(t => t.DueDate <= model.ToDate.Value);
                }

                if (model.ProjectId.HasValue)
                {
                    query = query.Where(t => t.ProjectId == model.ProjectId.Value);
                }

                // Apply access restrictions
                if (!isManagerOrAdmin)
                {
                    query = query.Where(t =>
                        t.AssignedToId == userId ||
                        t.CreatedById == userId);
                }

                // Apply sorting
                query = model.SortBy?.ToLower() switch
                {
                    "title" => model.SortDescending ?
                        query.OrderByDescending(t => t.Title) :
                        query.OrderBy(t => t.Title),
                    "duedate" => model.SortDescending ?
                        query.OrderByDescending(t => t.DueDate) :
                        query.OrderBy(t => t.DueDate),
                    "status" => model.SortDescending ?
                        query.OrderByDescending(t => t.Status) :
                        query.OrderBy(t => t.Status),
                    "assignee" => model.SortDescending ?
                        query.OrderByDescending(t => t.AssignedTo.UserName) :
                        query.OrderBy(t => t.AssignedTo.UserName),
                    _ => query.OrderByDescending(t => t.CreatedAt)
                };

                // Get total count for pagination
                var totalTasks = await query.CountAsync();

                // Apply pagination
                var tasks = await query
                    .Skip((model.Page - 1) * model.PageSize)
                    .Take(model.PageSize)
                    .ToListAsync();

                model.Tasks = tasks;
                model.TotalTasks = totalTasks;

                // Create audit log for search
                // Create audit log for search
                await _notificationService.CreateAuditLogAsync(
                    userId,
                    "TaskSearch",
                    $"Searched tasks with query: {model.Query}. Filters: Status={model.Status}, AssigneeId={model.AssigneeId}, ProjectId={model.ProjectId}"
                );

                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return PartialView("_TaskList", tasks);
                }

                return View("Index", model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching tasks");
                return Problem("Error searching tasks. Please try again later.");
            }
        }
    }
}