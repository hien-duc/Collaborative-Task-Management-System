using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using Collaborative_Task_Management_System.Data;
using Collaborative_Task_Management_System.Models;
using Collaborative_Task_Management_System.Models.ViewModels;
using Collaborative_Task_Management_System.Services;
using Microsoft.AspNetCore.Mvc.Rendering;
using Serilog;

namespace Collaborative_Task_Management_System.Controllers
{
    public class ProjectsController : BaseController
    {
        private readonly IProjectServiceWithUoW _projectService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<ProjectsController> _logger;

        public ProjectsController(
            IProjectServiceWithUoW projectService,
            UserManager<ApplicationUser> userManager,
            ILogger<ProjectsController> logger) : base(userManager)
        {
            _projectService = projectService;
            _userManager = userManager;
            _logger = logger;
        }

        // GET: Projects
        [Authorize(Policy = "ManagerOrAdmin")]
        public async Task<IActionResult> Index()
        {
            try
            {
                var projects = await _projectService.GetAllProjectsAsync();

                return View(projects);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving projects list");
                return Problem("Error retrieving projects. Please try again later.");
            }
        }

        // GET: Projects/Details/5
        [Authorize(Policy = "ManagerOrAdmin")]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            try
            {
                var project = await _projectService.GetProjectByIdAsync(id.Value);

                if (project == null)
                {
                    return NotFound();
                }

                // Get project members
                var projectMembers = await _projectService.GetProjectMembersAsync(id.Value);
                
                // Get all users for the dropdown to add new members
                var allUsers = await _userManager.Users.ToListAsync();
                
                // Filter out users who are already members
                var memberUserIds = projectMembers.Select(pm => pm.UserId).ToList();
                var availableUsers = allUsers
                    .Where(u => !memberUserIds.Contains(u.Id))
                    .Select(u => new SelectListItem
                    {
                        Value = u.Id,
                        Text = string.IsNullOrEmpty(u.FullName) ? u.UserName : u.FullName
                    })
                    .ToList();

                // Determine if current user is a project member or manager
                var currentUserId = GetCurrentUserId();
                bool isManager = project.CreatedById == currentUserId || await IsUserInRoleAsync("Admin");
                bool isMember = isManager || await _projectService.IsUserProjectMemberAsync(id.Value, currentUserId);

                // Filter tasks based on user role and membership
                var filteredTasks = project.Tasks;
                if (!isManager && !await IsUserInRoleAsync("Admin"))
                {
                    // If not manager or admin, only show tasks assigned to the user or if they're a team member
                    if (isMember)
                    {
                        // Show all tasks for team members
                    }
                    else
                    {
                        // Only show tasks assigned to the user
                        filteredTasks = project.Tasks.Where(t => t.AssignedToId == currentUserId).ToList();
                    }
                }

                // Update the project with filtered tasks
                project.Tasks = filteredTasks;

                // Pass data to view
                ViewData["ProjectMembers"] = projectMembers;
                ViewData["AvailableUsers"] = availableUsers;
                ViewData["IsManager"] = isManager;
                ViewData["IsMember"] = isMember;

                return View(project);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving project details for ID: {ProjectId}", id);
                return Problem("Error retrieving project details. Please try again later.");
            }
        }

        // GET: Projects/Create
        [Authorize(Policy = "AuthenticatedUser")]
        public async Task<IActionResult> Create()
        {
            var model = new ProjectCreateViewModel()
            {
                AvailableUsers = await GetAvailableUsers()
            };
            return View(model);
        }

        // POST: Projects/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "AuthenticatedUser")]
    public async Task<IActionResult> Create(ProjectCreateViewModel model)
    {
        if (!ModelState.IsValid)
        {
            model.AvailableUsers = await GetAvailableUsers();
            return View(model);
        }

        try
        {
            var currentUserId = GetCurrentUserId();
            var currentUser = await _userManager.GetUserAsync(User);

            var project = new Project
            {
                Title = model.Title,
                Description = model.Description,
                Deadline = model.Deadline,
                Priority = model.Priority,
                CreatedById = currentUserId,
                OwnerId = currentUserId,
                Status = ProjectStatus.Planning,
                CreatedAt = DateTime.UtcNow,
                TeamMembers = new List<ApplicationUser> { currentUser }
            };

            // Add selected team members
            if (model.SelectedTeamMemberIds?.Any() == true)
            {
                var users = await _userManager.Users
                    .Where(u => model.SelectedTeamMemberIds.Contains(u.Id))
                    .ToListAsync();
                
                foreach (var user in users)
                {
                    project.TeamMembers.Add(user);
                }
            }
            string? ipAddress = Request.HttpContext.Connection.RemoteIpAddress?.ToString();

            // Check if the current user is already a Manager, if not, assign the role
            if (!await _userManager.IsInRoleAsync(currentUser, "Manager"))
            {
                await _userManager.AddToRoleAsync(currentUser, "Manager");
                _logger.LogInformation("User {UserId} was automatically assigned the Manager role upon project creation", currentUserId);
            }

            // Create the project first
            var createdProject = await _projectService.CreateProjectAsync(project, ipAddress);

            // Create a ProjectMember entry for the creator
            await _projectService.AddProjectMemberAsync(createdProject.Id, currentUserId, ipAddress);

            TempData["SuccessMessage"] = "Project created successfully!";
            return RedirectToAction(nameof(Details), new { id = createdProject.Id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating project");
            ModelState.AddModelError("", "An error occurred while creating the project. Please try again.");
            model.AvailableUsers = await GetAvailableUsers();
            return View(model);
        }
    }

    private async Task<List<SelectListItem>> GetAvailableUsers()
    {
        return (await _userManager.Users
                .OrderBy(u => u.UserName)
                .Select(u => new SelectListItem
                {
                    Value = u.Id,
                    Text = u.UserName
                })
                .ToListAsync());
    }

// GET: Projects/Edit/5
[Authorize(Policy = "ManagerOrAdmin")]
public async Task<IActionResult> Edit(int? id)
{
    if (id == null)
    {
        return NotFound();
    }

    try
    {
        var project = await _projectService.GetProjectByIdAsync(id.Value);
        if (project == null)
        {
            return NotFound();
        }

        // Check if user has permission to edit
        if (!await CanManageProjectAsync(project.OwnerId))
        {
            return Forbid();
        }

        return View(project);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error retrieving project for editing {ProjectId}", id);
        return Problem("Error loading project for editing. Please try again later.");
    }
}

// POST: Projects/Edit/5
[HttpPost]
[ValidateAntiForgeryToken]
[Authorize(Policy = "ManagerOrAdmin")]
public async Task<IActionResult> Edit(int id, [Bind("Id,Title,Description,Deadline,Status,Priority")] Project project)
{
    if (id != project.Id)
    {
        return NotFound();
    }

    try
    {
        // Check if user has permission to edit
        var existingProject = await _projectService.GetProjectByIdAsync(id);
        if (existingProject == null)
        {
            return NotFound();
        }

        if (!await CanManageProjectAsync(existingProject.OwnerId))
        {
            return Forbid();
        }

        if (ModelState.IsValid)
        {
            // Preserve original creation data
            project.CreatedAt = existingProject.CreatedAt;
            project.CreatedById = existingProject.CreatedById;
            project.OwnerId = existingProject.OwnerId;
            project.UpdatedAt = DateTime.UtcNow;

            await _projectService.UpdateProjectAsync(project);

            TempData["SuccessMessage"] = "Project updated successfully!";
            return RedirectToAction(nameof(Details), new { id = project.Id });
        }
        return View(project);
    }
    catch (DbUpdateConcurrencyException ex)
    {
        if (!await ProjectExists(project.Id))
        {
            return NotFound();
        }
        _logger.LogError(ex, "Concurrency error updating project {ProjectId}", id);
        ModelState.AddModelError("", "The project was modified by another user. Please refresh and try again.");
        return View(project);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error updating project {ProjectId}", id);
        ModelState.AddModelError("", "Error updating project. Please try again later.");
        return View(project);
    }
}

// GET: Projects/Delete/5
[Authorize(Policy = "ManagerOrAdmin")]
public async Task<IActionResult> Delete(int? id)
{
    if (id == null)
    {
        return NotFound();
    }

    try
    {
        var project = await _projectService.GetProjectByIdAsync(id.Value);
        if (project == null)
        {
            return NotFound();
        }

        // Check if user has permission to delete
        if (!await CanManageProjectAsync(project.OwnerId))
        {
            return Forbid();
        }

        return View(project);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error retrieving project for deletion {ProjectId}", id);
        return Problem("Error loading project for deletion. Please try again later.");
    }
}

// POST: Projects/Delete/5
[HttpPost, ActionName("Delete")]
[ValidateAntiForgeryToken]
[Authorize(Policy = "ManagerOrAdmin")]
public async Task<IActionResult> DeleteConfirmed(int id)
{
    try
    {
        var project = await _projectService.GetProjectByIdAsync(id);
        if (project == null)
        {
            return NotFound();
        }

        // Check if user has permission to delete
        if (!await CanManageProjectAsync(project.OwnerId))
        {
            return Forbid();
        }

        await _projectService.DeleteProjectAsync(id);

        TempData["SuccessMessage"] = "Project deleted successfully!";
        return RedirectToAction(nameof(Index));
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error deleting project {ProjectId}", id);
        return Problem("Error deleting project. Please try again later.");
    }
}

private async Task<bool> ProjectExists(int id)
{
    return await _projectService.ProjectExistsAsync(id);
}

        // POST: Projects/AddMember/{projectId}
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "ManagerOrAdmin")]
        public async Task<IActionResult> AddMember(int projectId, string userId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                TempData["ErrorMessage"] = "No user selected."; 
                return RedirectToAction(nameof(Details), new { id = projectId });
            }

            try
            {
                // Get the project to check if current user is the manager
                var project = await _projectService.GetProjectByIdAsync(projectId);
                if (project == null)
                {
                    return NotFound();
                }

                // Validate that the current user is the project's Manager
                var currentUserId = GetCurrentUserId();
                if (project.CreatedById != currentUserId && !await IsUserInRoleAsync("Admin"))
                {
                    _logger.LogWarning("User {UserId} attempted to add a member to project {ProjectId} without permission", currentUserId, projectId);
                    return Forbid();
                }

                // Check if user is already a member
                if (await _projectService.IsUserProjectMemberAsync(projectId, userId))
                {
                    TempData["WarningMessage"] = "User is already a member of this project.";
                    return RedirectToAction(nameof(Details), new { id = projectId });
                }

                // Add the member to the project
                string? ipAddress = Request.HttpContext.Connection.RemoteIpAddress?.ToString();
                var projectMember = await _projectService.AddProjectMemberAsync(projectId, userId, ipAddress);

                // Get user details for notification
                var user = await _userManager.FindByIdAsync(userId);
                if (user != null)
                {
                    // Send SignalR notification to the added user
                    var hubContext = HttpContext.RequestServices.GetService<Microsoft.AspNetCore.SignalR.IHubContext<Hubs.NotificationHub>>();
                    if (hubContext != null)
                    {
                        await hubContext.Clients.User(userId).SendAsync("ReceiveNotification", 
                            $"You were added to project: {project.Title}", 
                            "project-membership");
                    }
                }

                TempData["SuccessMessage"] = "Team member added successfully!";
                return RedirectToAction(nameof(Details), new { id = projectId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding user {UserId} to project {ProjectId}", userId, projectId);
                TempData["ErrorMessage"] = "An error occurred while adding the team member.";
                return RedirectToAction(nameof(Details), new { id = projectId });
            }
        }

        // POST: Projects/RemoveMember/{projectId}/{userId}
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "ManagerOrAdmin")]
        public async Task<IActionResult> RemoveMember(int projectId, string userId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                TempData["ErrorMessage"] = "No user specified.";
                return RedirectToAction(nameof(Details), new { id = projectId });
            }

            try
            {
                // Get the project to check if current user is the manager
                var project = await _projectService.GetProjectByIdAsync(projectId);
                if (project == null)
                {
                    return NotFound();
                }

                // Validate that the current user is the project's Manager
                var currentUserId = GetCurrentUserId();
                if (project.CreatedById != currentUserId && !await IsUserInRoleAsync("Admin"))
                {
                    _logger.LogWarning("User {UserId} attempted to remove a member from project {ProjectId} without permission", currentUserId, projectId);
                    return Forbid();
                }

                // Don't allow removing the project creator
                if (project.CreatedById == userId)
                {
                    TempData["ErrorMessage"] = "Cannot remove the project creator.";
                    return RedirectToAction(nameof(Details), new { id = projectId });
                }

                // Remove the member from the project
                string? ipAddress = Request.HttpContext.Connection.RemoteIpAddress?.ToString();
                await _projectService.RemoveProjectMemberAsync(projectId, userId, ipAddress);

                TempData["SuccessMessage"] = "Team member removed successfully!";
                return RedirectToAction(nameof(Details), new { id = projectId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing user {UserId} from project {ProjectId}", userId, projectId);
                TempData["ErrorMessage"] = "An error occurred while removing the team member.";
                return RedirectToAction(nameof(Details), new { id = projectId });
            }
        }
    }
}