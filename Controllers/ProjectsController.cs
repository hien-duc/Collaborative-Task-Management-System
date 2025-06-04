using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Collaborative_Task_Management_System.Data;
using Collaborative_Task_Management_System.Models;
using Collaborative_Task_Management_System.Models.ViewModels;
using Collaborative_Task_Management_System.Services;
using Microsoft.AspNetCore.Mvc.Rendering;
using Serilog;

namespace Collaborative_Task_Management_System.Controllers
{
    [Authorize(Policy = "ManagerOrAdmin")]
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

                return View(project);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving project details for ID: {ProjectId}", id);
                return Problem("Error retrieving project details. Please try again later.");
            }
        }

        // GET: Projects/Create
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

            await _projectService.CreateProjectAsync(project, ipAddress);

            TempData["SuccessMessage"] = "Project created successfully!";
            return RedirectToAction(nameof(Index));
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
    }
}