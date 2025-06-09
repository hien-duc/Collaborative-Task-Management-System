using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Collaborative_Task_Management_System.Models;
using Collaborative_Task_Management_System.Models.ViewModels;
using Collaborative_Task_Management_System.Services;
using Microsoft.AspNetCore.Mvc.Rendering;
using TaskStatus = Collaborative_Task_Management_System.Models.TaskStatus;

namespace Collaborative_Task_Management_System.Controllers
{
    [Authorize]
    public class TasksController : BaseController
    {
        private readonly ITaskServiceWithUoW _taskService;
        private readonly IProjectServiceWithUoW _projectService;
        private readonly INotificationServiceWithUoW _notificationService;
        private readonly ITaskActivityLogServiceWithUoW _taskActivityLogService;
        private readonly ITaskChecklistItemServiceWithUoW _taskChecklistItemService;
        private readonly ITaskTimeEntryServiceWithUoW _taskTimeEntryService;
        private readonly ITaskDependencyServiceWithUoW _taskDependencyService;
        private readonly ITagServiceWithUoW _tagService;
        private readonly ILogger<TasksController> _logger;

        public TasksController(
            ITaskServiceWithUoW taskService,
            IProjectServiceWithUoW projectService,
            INotificationServiceWithUoW notificationService,
            ITaskActivityLogServiceWithUoW taskActivityLogService,
            ITaskChecklistItemServiceWithUoW taskChecklistItemService,
            ITaskTimeEntryServiceWithUoW taskTimeEntryService,
            ITaskDependencyServiceWithUoW taskDependencyService,
            ITagServiceWithUoW tagService,
            UserManager<ApplicationUser> userManager,
            ILogger<TasksController> logger)
            : base(userManager)
        {
            _taskService = taskService;
            _projectService = projectService;
            _notificationService = notificationService;
            _taskActivityLogService = taskActivityLogService;
            _taskChecklistItemService = taskChecklistItemService;
            _taskTimeEntryService = taskTimeEntryService;
            _taskDependencyService = taskDependencyService;
            _tagService = tagService;
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
            
            // Convert users to SelectListItems
            var users = await _userManager.Users.ToListAsync();
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
            IFormFile? attachment,
            string[]? tags,
            string[]? checklistItems)
        {
            try
            {
                task.CreatedById = GetCurrentUserId();
                ModelState.Remove("CreatedById");
                if (ModelState.IsValid) {
                    task.CreatedAt = DateTime.UtcNow;

                    var createdTask = await _taskService.CreateTaskAsync(task);

                    // Log task creation
                    await _taskActivityLogService.LogTaskCreatedAsync(createdTask.Id, GetCurrentUserId());

                    // Add tags if provided
                    if (tags != null && tags.Length > 0)
                    {
                        foreach (var tagName in tags)
                        {
                            if (!string.IsNullOrWhiteSpace(tagName))
                            {
                                var tag = await _tagService.GetTagByNameAsync(tagName.Trim());
                                if (tag == null)
                                {
                                    tag = await _tagService.CreateTagAsync(new Tag { Name = tagName.Trim() });
                                }
                                await _tagService.AddTagToTaskAsync(createdTask.Id, tag.Id);
                            }
                        }
                    }

                    // Add checklist items if provided
                    if (checklistItems != null && checklistItems.Length > 0)
                    {
                        int position = 1;
                        foreach (var itemDescription in checklistItems)
                        {
                            if (!string.IsNullOrWhiteSpace(itemDescription))
                            {
                                await _taskChecklistItemService.CreateChecklistItemAsync(new TaskChecklistItem
                                {
                                    TaskId = createdTask.Id,
                                    Description = itemDescription.Trim(),
                                    IsCompleted = false,
                                    Position = position++
                                });
                            }
                        }
                    }

                    if (attachment != null)
                    {
                        await _taskService.SaveFileAttachmentAsync(
                            createdTask.Id, attachment, GetCurrentUserId());
                        
                        // Log attachment added
                        await _taskActivityLogService.LogTaskAttachmentAddedAsync(
                            createdTask.Id, GetCurrentUserId(), attachment.FileName);
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
                


                // If we got this far, something failed; repopulate the users list
                var users = await _userManager.Users.ToListAsync();
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
                
                // Repopulate the users list in case of error
                var users = await _userManager.Users.ToListAsync();
                ViewBag.Users = users.Select(u => new SelectListItem
                {
                    Value = u.Id,
                    Text = u.FullName ?? u.UserName,
                    Selected = u.Id == task?.AssignedToId
                }).ToList();
                
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

            var users = await _userManager.Users.ToListAsync();
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
            IFormFile attachment,
            string[]? tags,
            string[]? checklistItems)
        {
            if (id != task.Id)
            {
                return NotFound();
            }

            try
            {
                if (ModelState.IsValid)
                {
                    var originalTask = await _taskService.GetTaskByIdAsync(id);
                    string changes = GetChanges(originalTask, task);
                    
                    var updatedTask = await _taskService.UpdateTaskAsync(task);
                    
                    // Log task update
                    await _taskActivityLogService.LogTaskUpdatedAsync(updatedTask.Id, GetCurrentUserId(), changes);
                    
                    // If assignee changed, log it
                    if (originalTask.AssignedToId != task.AssignedToId)
                    {
                        await _taskActivityLogService.LogTaskAssignedAsync(
                            updatedTask.Id, GetCurrentUserId(), task.AssignedToId);
                    }
                    
                    // If status changed, log it
                    if (originalTask.Status != task.Status)
                    {
                        await _taskActivityLogService.LogTaskStatusChangedAsync(
                            updatedTask.Id, GetCurrentUserId(), originalTask.Status, task.Status);
                    }

                    // Update tags
                    if (tags != null)
                    {
                        // Get current tags
                        var currentTags = await _tagService.GetTagsByTaskIdAsync(id);
                        
                        // Remove tags that are no longer present
                        foreach (var tag in currentTags)
                        {
                            if (!tags.Contains(tag.Name))
                            {
                                await _tagService.RemoveTagFromTaskAsync(id, tag.Id);
                            }
                        }
                        
                        // Add new tags
                        foreach (var tagName in tags)
                        {
                            if (!string.IsNullOrWhiteSpace(tagName))
                            {
                                var tag = await _tagService.GetTagByNameAsync(tagName.Trim());
                                if (tag == null)
                                {
                                    tag = await _tagService.CreateTagAsync(new Tag { Name = tagName.Trim() });
                                }
                                
                                // Check if this tag is already associated with the task
                                if (!currentTags.Any(t => t.Id == tag.Id))
                                {
                                    await _tagService.AddTagToTaskAsync(id, tag.Id);
                                }
                            }
                        }
                    }

                    // Update checklist items if provided
                    if (checklistItems != null)
                    {
                        // Get current checklist items
                        var currentItems = await _taskChecklistItemService.GetChecklistItemsByTaskIdAsync(id);
                        
                        // Create a list to track which items to keep
                        var itemsToKeep = new List<int>();
                        
                        // Process each checklist item
                        int position = 1;
                        foreach (var itemDescription in checklistItems)
                        {
                            if (!string.IsNullOrWhiteSpace(itemDescription))
                            {
                                // Check if this item already exists (by description)
                                var existingItem = currentItems.FirstOrDefault(i => 
                                    i.Description.Equals(itemDescription.Trim(), StringComparison.OrdinalIgnoreCase));
                                
                                if (existingItem != null)
                                {
                                    // Update position of existing item
                                    existingItem.Position = position++;
                                    await _taskChecklistItemService.UpdateChecklistItemAsync(existingItem);
                                    itemsToKeep.Add(existingItem.Id);
                                }
                                else
                                {
                                    // Create new item
                                    var newItem = await _taskChecklistItemService.CreateChecklistItemAsync(new TaskChecklistItem
                                    {
                                        TaskId = id,
                                        Description = itemDescription.Trim(),
                                        IsCompleted = false,
                                        Position = position++
                                    });
                                    itemsToKeep.Add(newItem.Id);
                                }
                            }
                        }
                        
                        // Delete items that weren't in the updated list
                        foreach (var item in currentItems)
                        {
                            if (!itemsToKeep.Contains(item.Id))
                            {
                                await _taskChecklistItemService.DeleteChecklistItemAsync(item.Id);
                            }
                        }
                    }

                    if (attachment != null)
                    {
                        await _taskService.SaveFileAttachmentAsync(
                            updatedTask.Id, attachment, GetCurrentUserId());
                            
                        // Log attachment added
                        await _taskActivityLogService.LogTaskAttachmentAddedAsync(
                            updatedTask.Id, GetCurrentUserId(), attachment.FileName);
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

        // Helper method to get changes between original and updated task
        private string GetChanges(TaskItem original, TaskItem updated)
        {
            var changes = new List<string>();
            
            if (original.Title != updated.Title)
                changes.Add($"Title changed from '{original.Title}' to '{updated.Title}'");
                
            if (original.Description != updated.Description)
                changes.Add("Description updated");
                
            if (original.DueDate != updated.DueDate)
                changes.Add($"Due date changed from '{original.DueDate:d}' to '{updated.DueDate:d}'");
                
            if (original.Priority != updated.Priority)
                changes.Add($"Priority changed from '{original.Priority}' to '{updated.Priority}'");
                
            if (original.Status != updated.Status)
                changes.Add($"Status changed from '{original.Status}' to '{updated.Status}'");
                
            if (original.AssignedToId != updated.AssignedToId)
                changes.Add("Assignment changed");
                
            return string.Join(", ", changes);
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

                var oldStatus = task.Status;
                task = await _taskService.UpdateTaskStatusAsync(id, status, currentUserId);
                
                // Log status change
                await _taskActivityLogService.LogTaskStatusChangedAsync(id, currentUserId, oldStatus, status);
                
                await _notificationService.SendTaskStatusUpdateNotificationAsync(task, currentUserId);

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating task status {TaskId}", id);
                return Json(new { success = false, message = "Error updating task status" });
            }
        }

        // GET: Tasks/Checklist/5
        public async Task<IActionResult> Checklist(int id)
        {
            try
            {
                var task = await _taskService.GetTaskByIdAsync(id);
                if (task == null)
                {
                    return NotFound();
                }
                
                var checklistItems = await _taskChecklistItemService.GetChecklistItemsByTaskIdAsync(id);
                return PartialView("_TaskChecklist", checklistItems);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving checklist for task {TaskId}", id);
                return Problem("Error retrieving checklist. Please try again later.");
            }
        }

        // POST: Tasks/ToggleChecklistItem/5
        [HttpPost]
        public async Task<IActionResult> ToggleChecklistItem(int id)
        {
            try
            {
                var item = await _taskChecklistItemService.ToggleChecklistItemCompletionAsync(id);
                return Json(new { success = true, isCompleted = item.IsCompleted });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling checklist item {ItemId}", id);
                return Json(new { success = false, message = "Error updating checklist item" });
            }
        }

        // POST: Tasks/ReorderChecklistItems
        [HttpPost]
        public async Task<IActionResult> ReorderChecklistItems(int taskId, [FromBody] List<int> itemIds)
        {
            try
            {
                await _taskChecklistItemService.ReorderChecklistItemsAsync(taskId, itemIds);
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reordering checklist items for task {TaskId}", taskId);
                return Json(new { success = false, message = "Error reordering checklist items" });
            }
        }

        // GET: Tasks/TimeEntries/5
        public async Task<IActionResult> TimeEntries(int id)
        {
            try
            {
                var task = await _taskService.GetTaskByIdAsync(id);
                if (task == null)
                {
                    return NotFound();
                }
                
                var timeEntries = await _taskTimeEntryService.GetTimeEntriesByTaskIdAsync(id);
                return PartialView("_TaskTimeEntries", timeEntries);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving time entries for task {TaskId}", id);
                return Problem("Error retrieving time entries. Please try again later.");
            }
        }

        // POST: Tasks/StartTimeEntry/5
        [HttpPost]
        public async Task<IActionResult> StartTimeEntry(int id)
        {
            try
            {
                var timeEntry = await _taskTimeEntryService.StartTimeEntryAsync(id, GetCurrentUserId());
                return Json(new { success = true, timeEntryId = timeEntry.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting time entry for task {TaskId}", id);
                return Json(new { success = false, message = "Error starting time entry" });
            }
        }

        // POST: Tasks/StopTimeEntry/5
        [HttpPost]
        public async Task<IActionResult> StopTimeEntry(int id)
        {
            try
            {
                var timeEntry = await _taskTimeEntryService.StopTimeEntryAsync(id);
                return Json(new { success = true, duration = timeEntry.Duration });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error stopping time entry {TimeEntryId}", id);
                return Json(new { success = false, message = "Error stopping time entry" });
            }
        }

        // GET: Tasks/Dependencies/5
        public async Task<IActionResult> Dependencies(int id)
        {
            try
            {
                var task = await _taskService.GetTaskByIdAsync(id);
                if (task == null)
                {
                    return NotFound();
                }
                
                var blockingTasks = await _taskDependencyService.GetBlockingTasksAsync(id);
                var blockedTasks = await _taskDependencyService.GetBlockedTasksAsync(id);
                
                var viewModel = new TaskDependenciesViewModel
                {
                    TaskId = id,
                    BlockingTasks = blockingTasks,
                    BlockedTasks = blockedTasks
                };
                
                return PartialView("_TaskDependencies", viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving dependencies for task {TaskId}", id);
                return Problem("Error retrieving dependencies. Please try again later.");
            }
        }

        // POST: Tasks/AddDependency
        [HttpPost]
        public async Task<IActionResult> AddDependency(int taskId, int blockingTaskId)
        {
            try
            {
                // Check for circular dependency
                if (await _taskDependencyService.HasCircularDependencyAsync(taskId, blockingTaskId))
                {
                    return Json(new { success = false, message = "Adding this dependency would create a circular reference" });
                }
                
                var dependency = await _taskDependencyService.AddDependencyAsync(taskId, blockingTaskId);
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding dependency between tasks {TaskId} and {BlockingTaskId}", taskId, blockingTaskId);
                return Json(new { success = false, message = "Error adding dependency" });
            }
        }

        // POST: Tasks/RemoveDependency
        [HttpPost]
        public async Task<IActionResult> RemoveDependency(int taskId, int blockingTaskId)
        {
            try
            {
                await _taskDependencyService.RemoveDependencyAsync(taskId, blockingTaskId);
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing dependency between tasks {TaskId} and {BlockingTaskId}", taskId, blockingTaskId);
                return Json(new { success = false, message = "Error removing dependency" });
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

                // Add priority filter
                if (!string.IsNullOrEmpty(model.Priority))
                {
                    query = query.Where(t => t.Priority == model.Priority);
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
    
    // Add this view model for task dependencies
    public class TaskDependenciesViewModel
    {
        public int TaskId { get; set; }
        public List<TaskItem> BlockingTasks { get; set; } = new List<TaskItem>();
        public List<TaskItem> BlockedTasks { get; set; } = new List<TaskItem>();
    }
}