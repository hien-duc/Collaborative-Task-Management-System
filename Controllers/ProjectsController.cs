using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Collaborative_Task_Management_System.Data;
using Collaborative_Task_Management_System.Models;
using Collaborative_Task_Management_System.Services;
using Serilog;

namespace Collaborative_Task_Management_System.Controllers
{
    [Authorize(Policy = "ManagerOrAdmin")]
    public class ProjectsController : BaseController
    {
        private readonly IProjectServiceWithUoW _projectService;
        private readonly ILogger<ProjectsController> _logger;

        public ProjectsController(
            IProjectServiceWithUoW projectService,
            UserManager<ApplicationUser> userManager,
            ILogger<ProjectsController> logger)
            : base(userManager)
        {
            _projectService = projectService;
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
        public IActionResult Create()
        {
            return View();
        }

        // POST: Projects/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Title,Description,Deadline")] Project project)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    project.CreatedAt = DateTime.UtcNow;
                    project.OwnerId = GetCurrentUserId();
                    project.Status = ProjectStatus.Planning;

                    await _projectService.CreateProjectAsync(project);

                    _logger.LogInformation("Project created: {ProjectId} by user {UserId}", 
                        project.Id, GetCurrentUserId());

                    return RedirectToAction(nameof(Index));
                }
                return View(project);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating project");
                ModelState.AddModelError("", "Error creating project. Please try again later.");
                return View(project);
            }
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

                return View(project);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving project for editing {ProjectId}", id);
                return Problem("Error loading project details. Please try again later.");
            }
        }

        // POST: Projects/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Title,Description,Deadline,Status")] Project project)
        {
            if (id != project.Id)
            {
                return NotFound();
            }

            try
            {
                if (ModelState.IsValid)
                {
                    await _projectService.UpdateProjectAsync(project);

                    _logger.LogInformation("Project updated: {ProjectId} by user {UserId}", 
                        project.Id, GetCurrentUserId());

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

        // POST: Projects/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                await _projectService.DeleteProjectAsync(id);

                _logger.LogInformation("Project deleted: {ProjectId} by user {UserId}", 
                    id, GetCurrentUserId());

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