using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Collaborative_Task_Management_System.Models;
using Collaborative_Task_Management_System.Models.ViewModels;
using Collaborative_Task_Management_System.Services;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Security.Claims;
using TaskStatus = Collaborative_Task_Management_System.Models.TaskStatus;
using Microsoft.AspNetCore.SignalR;
using Collaborative_Task_Management_System.Hubs;
using Collaborative_Task_Management_System.Controllers;

namespace Collaborative_Task_Management_System.Controllers
{
    [Authorize]
    public class TasksController : BaseController
    {
        private readonly ITaskServiceWithUoW _taskService;
        private readonly IProjectServiceWithUoW _projectService;
        private readonly INotificationServiceWithUoW _notificationService;
        private readonly ILogger<TasksController> _logger;
        private readonly IHubContext<NotificationHub> _hubContext;
        private readonly HomeController _homeController;

        public TasksController(
        ITaskServiceWithUoW taskService,
        IProjectServiceWithUoW projectService,
        INotificationServiceWithUoW notificationService,
        UserManager<ApplicationUser> userManager,
        ILogger<TasksController> logger,
        IHubContext<NotificationHub> hubContext,
        HomeController homeController)
        : base(userManager)
    {
        _taskService = taskService;
        _projectService = projectService;
        _notificationService = notificationService;
        _logger = logger;
        _hubContext = hubContext;
        _homeController = homeController;
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

            // Check if user can access this project
            if (!await CanAccessTaskAsync(projectId.Value))
            {
                return Forbid();
            }

            ViewBag.ProjectId = projectId.Value;
            
            // Get project members and manager for the assignee dropdown
            project = await _projectService.GetProjectByIdAsync(projectId.Value);
            var projectMembers = await _projectService.GetProjectMembersAsync(projectId.Value);
            var memberIds = projectMembers.Select(pm => pm.UserId).ToList();
            
            // Add project manager if not already in the list
            if (!memberIds.Contains(project.CreatedById))
            {
                memberIds.Add(project.CreatedById);
            }
            
            // Filter users to only include project members and manager
            var users = await _userManager.Users
                .Where(u => memberIds.Contains(u.Id))
                .ToListAsync();
                
            ViewBag.Users = users.Select(u => new SelectListItem()
            {
                Value = u.Id,
                Text = u.FullName ?? u.UserName
            }).ToList();
            
            return View();
        }

        // POST: Tasks/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind("ProjectId,Title,Description,DueDate,Priority,Status,AssignedToId")] TaskItem task,
            IFormFile? attachment)
        {
            try
            {
                // Check if user can access this project
                if (!await CanAccessTaskAsync(task.ProjectId))
                {
                    return Forbid();
                }
                task.CreatedById = GetCurrentUserId();
                ModelState.Remove("CreatedById");
                if (ModelState.IsValid) {
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
                var errors = ModelState
                        .Where(x => x.Value.Errors.Count > 0)
                        .Select(x => new { x.Key, x.Value.Errors })
                        .ToList();
                    _logger.LogWarning("ModelState is invalid. Errors: {Errors}", 
                        string.Join(", ", errors.Select(e => $"{e.Key}: {string.Join(", ", e.Errors.Select(er => er.ErrorMessage))}")));
                


                // If we got this far, something failed; repopulate the users list with project members
                var project = await _projectService.GetProjectByIdAsync(task.ProjectId);
                var projectMembers = await _projectService.GetProjectMembersAsync(task.ProjectId);
                var memberIds = projectMembers.Select(pm => pm.UserId).ToList();
                
                // Add project manager if not already in the list
                if (!memberIds.Contains(project.CreatedById))
                {
                    memberIds.Add(project.CreatedById);
                }
                
                // Filter users to only include project members and manager
                var users = await _userManager.Users
                    .Where(u => memberIds.Contains(u.Id))
                    .ToListAsync();
                    
                ViewBag.Users = users.Select(u => new SelectListItem
                {
                    Value = u.Id,
                    Text = u.FullName ?? u.UserName,
                    Selected = u.Id == task.AssignedToId
                }).ToList();
                
                ViewBag.ProjectId = task.ProjectId;
                return View(task);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating task");
                ModelState.AddModelError("", "Error creating task. Please try again later.");
                
                // Repopulate the users list in case of error with project members
                if (task?.ProjectId != null)
                {
                    var project = await _projectService.GetProjectByIdAsync(task.ProjectId);
                    var projectMembers = await _projectService.GetProjectMembersAsync(task.ProjectId);
                    var memberIds = projectMembers.Select(pm => pm.UserId).ToList();
                    
                    // Add project manager if not already in the list
                    if (!memberIds.Contains(project.CreatedById))
                    {
                        memberIds.Add(project.CreatedById);
                    }
                    
                    // Filter users to only include project members and manager
                    var users = await _userManager.Users
                        .Where(u => memberIds.Contains(u.Id))
                        .ToListAsync();
                        
                    ViewBag.Users = users.Select(u => new SelectListItem
                    {
                        Value = u.Id,
                        Text = u.FullName ?? u.UserName,
                        Selected = u.Id == task.AssignedToId
                    }).ToList();
                }
                else
                {
                    // Fallback if project ID is null
                    var users = await _userManager.Users.ToListAsync();
                    ViewBag.Users = users.Select(u => new SelectListItem
                    {
                        Value = u.Id,
                        Text = u.FullName ?? u.UserName,
                        Selected = u.Id == task?.AssignedToId
                    }).ToList();
                }
                
                ViewBag.ProjectId = task?.ProjectId;
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

            // Check if user can access this task
            if (!await CanAccessTaskAsync(task.ProjectId))
            {
                return Forbid();
            }

            // Get project members and manager for the assignee dropdown
            var project = await _projectService.GetProjectByIdAsync(task.ProjectId);
            var projectMembers = await _projectService.GetProjectMembersAsync(task.ProjectId);
            var memberIds = projectMembers.Select(pm => pm.UserId).ToList();
            
            // Add project manager if not already in the list
            if (!memberIds.Contains(project.CreatedById))
            {
                memberIds.Add(project.CreatedById);
            }
            
            // Filter users to only include project members and manager
            var users = await _userManager.Users
                .Where(u => memberIds.Contains(u.Id))
                .ToListAsync();
                
            ViewBag.Users = users.Select(u => new SelectListItem
            {
                Value = u.Id,
                Text = u.FullName ?? u.UserName,
                Selected = u.Id == task.AssignedToId
            }).ToList();
            
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

            // Check if user can access this task
            if (!await CanAccessTaskAsync(task.ProjectId))
            {
                return Forbid();
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

                // Repopulate the users list in case of error with project members
                if (task?.ProjectId != null)
                {
                    var project = await _projectService.GetProjectByIdAsync(task.ProjectId);
                    var projectMembers = await _projectService.GetProjectMembersAsync(task.ProjectId);
                    var memberIds = projectMembers.Select(pm => pm.UserId).ToList();
                    
                    // Add project manager if not already in the list
                    if (!memberIds.Contains(project.CreatedById))
                    {
                        memberIds.Add(project.CreatedById);
                    }
                    
                    // Filter users to only include project members and manager
                    var users = await _userManager.Users
                        .Where(u => memberIds.Contains(u.Id))
                        .ToListAsync();
                        
                    ViewBag.Users = users.Select(u => new SelectListItem
                    {
                        Value = u.Id,
                        Text = u.FullName ?? u.UserName,
                        Selected = u.Id == task.AssignedToId
                    }).ToList();
                }
                else
                {
                    // Fallback if project ID is null
                    var users = await _userManager.Users.ToListAsync();
                    ViewBag.Users = users.Select(u => new SelectListItem
                    {
                        Value = u.Id,
                        Text = u.FullName ?? u.UserName,
                        Selected = u.Id == task?.AssignedToId
                    }).ToList();
                }
                
                ViewBag.ProjectId = task?.ProjectId;
                return View(task);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating task {TaskId}", id);
                ModelState.AddModelError("", "Error updating task. Please try again later.");
                // Repopulate the users list in case of error with project members
                if (task?.ProjectId != null)
                {
                    var project = await _projectService.GetProjectByIdAsync(task.ProjectId);
                    var projectMembers = await _projectService.GetProjectMembersAsync(task.ProjectId);
                    var memberIds = projectMembers.Select(pm => pm.UserId).ToList();
                    
                    // Add project manager if not already in the list
                    if (!memberIds.Contains(project.CreatedById))
                    {
                        memberIds.Add(project.CreatedById);
                    }
                    
                    // Filter users to only include project members and manager
                    var users = await _userManager.Users
                        .Where(u => memberIds.Contains(u.Id))
                        .ToListAsync();
                        
                    ViewBag.Users = users.Select(u => new SelectListItem
                    {
                        Value = u.Id,
                        Text = u.FullName ?? u.UserName,
                        Selected = u.Id == task.AssignedToId
                    }).ToList();
                }
                else
                {
                    // Fallback if project ID is null
                    var users = await _userManager.Users.ToListAsync();
                    ViewBag.Users = users.Select(u => new SelectListItem
                    {
                        Value = u.Id,
                        Text = u.FullName ?? u.UserName,
                        Selected = u.Id == task?.AssignedToId
                    }).ToList();
                }
                
                ViewBag.ProjectId = task?.ProjectId;
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

                // Check if user can access this task
                if (!await CanAccessTaskAsync(task.ProjectId))
                {
                    return Forbid();
                }

                var projectId = task.ProjectId;
                await _taskService.DeleteTaskAsync(id);

                await _notificationService.CreateAuditLogAsync(
                    GetCurrentUserId(),
                    "TaskDeleted",
                    $"Deleted task {id}");
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = true });
                }
                

                return RedirectToAction("Details", "Projects", new { id = projectId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting task {TaskId}", id);
                return Problem("Error deleting task. Please try again later.");
            }
        }

        // Helper method to check if the current user can access a task
        private async Task<bool> CanAccessTaskAsync(int projectId)
        {
            var currentUserId = GetCurrentUserId();
            
            // Admin and Manager can access all tasks
            if (await IsUserInRoleAsync("Admin") || 
                await IsUserInRoleAsync("Manager"))
            {
                return true;
            }
            
            // Project members can access tasks in their projects
            return await _projectService.IsUserProjectMemberAsync(projectId, currentUserId);
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

                // Check if user can access this task
                if (!await CanAccessTaskAsync(task.ProjectId))
                {
                    return Json(new { success = false, message = "You don't have permission to update this task." });
                }

                var currentUserId = GetCurrentUserId();
                if (task.AssignedToId != currentUserId && !await IsUserInRoleAsync("Manager"))
                {
                    return Forbid();
                }

                task = await _taskService.UpdateTaskStatusAsync(id, status, currentUserId);
                await _notificationService.SendTaskStatusUpdateNotificationAsync(task, currentUserId);
                
                // Broadcast dashboard update to all project members
                await BroadcastDashboardUpdateToProjectMembers(task.ProjectId);

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating task status {TaskId}", id);
                return Json(new { success = false, message = "Error updating task status" });
            }
        }
        
        // Helper method to broadcast dashboard updates to all project members
        private async Task BroadcastDashboardUpdateToProjectMembers(int projectId)
        {
            try
            {
                // Get all project members
                var project = await _projectService.GetProjectByIdAsync(projectId);
                if (project == null)
                {
                    _logger.LogWarning("Cannot broadcast dashboard update: Project {ProjectId} not found", projectId);
                    return;
                }
                
                // Get all project members including the creator
                var members = project.ProjectMembers.Select(pm => pm.UserId).ToList();
                if (!members.Contains(project.CreatedById))
                {
                    members.Add(project.CreatedById);
                }
                
                // Broadcast dashboard update to each member
                foreach (var userId in members)
                {
                    await _homeController.BroadcastDashboardUpdate(userId, projectId);
                }
                
                _logger.LogInformation("Dashboard update broadcast sent to {MemberCount} members of project {ProjectId}", members.Count, projectId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error broadcasting dashboard update to project members for project {ProjectId}", projectId);
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
        public async Task<IActionResult> MyTasks(int? projectId)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var userProjects = await _projectService.GetProjectsForUserAsync(currentUserId);
                
                // Get tasks assigned to the user
                var assignedTasks = await _taskService.GetTasksByAssignedUserAsync(currentUserId);
                
                // Get all tasks from projects where the user is a member
                var allUserTasks = new List<TaskItem>();
                
                foreach (var project in userProjects)
                {
                    // Skip if we're filtering by project and this isn't the selected project
                    if (projectId.HasValue && project.Id != projectId.Value)
                        continue;
                        
                    var projectTasks = await _taskService.GetTasksByProjectIdAsync(project.Id);
                    allUserTasks.AddRange(projectTasks);
                }
                
                // If filtering by project, only include tasks from that project
                if (projectId.HasValue)
                {
                    allUserTasks = allUserTasks.Where(t => t.ProjectId == projectId.Value).ToList();
                }
                else
                {
                    // Otherwise, combine assigned tasks with project tasks and remove duplicates
                    allUserTasks = allUserTasks.Concat(assignedTasks)
                        .GroupBy(t => t.Id)
                        .Select(g => g.First())
                        .ToList();
                }
                
                // Create select list for projects dropdown
                var projectSelectList = userProjects
                    .Select(p => new SelectListItem
                    {
                        Value = p.Id.ToString(),
                        Text = p.Title,
                        Selected = projectId.HasValue && p.Id == projectId.Value
                    })
                    .ToList();
                
                // Add "All Projects" option
                projectSelectList.Insert(0, new SelectListItem
                {
                    Value = "",
                    Text = "All Projects",
                    Selected = !projectId.HasValue
                });
                
                ViewData["Projects"] = projectSelectList;
                ViewData["SelectedProjectId"] = projectId;
                
                return View(allUserTasks);
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

                // Get user's projects (where they are a member or owner)
                var userProjects = await _projectService.GetProjectsForUserAsync(userId);
                var userProjectIds = userProjects.Select(p => p.Id).ToList();

                // Apply access restrictions
                if (!isManagerOrAdmin)
                {
                    query = query.Where(t =>
                        t.AssignedToId == userId || // User is assigned to the task
                        userProjectIds.Contains(t.ProjectId)); // Task is in a project where user is a member or owner
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
                var paginatedTasks = await query
                    .Skip((model.Page - 1) * model.PageSize)
                    .Take(model.PageSize)
                    .ToListAsync();

                model.Tasks = paginatedTasks;
                model.TotalTasks = totalTasks;

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