using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics;
using Collaborative_Task_Management_System.Models;
using Collaborative_Task_Management_System.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.AspNetCore.SignalR;
using Collaborative_Task_Management_System.Hubs;
using TaskStatus = Collaborative_Task_Management_System.Models.TaskStatus;

namespace Collaborative_Task_Management_System.Controllers
{
    public class HomeController : BaseController
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IProjectServiceWithUoW _projectService;
        private readonly ITaskServiceWithUoW _taskService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IMemoryCache _cache;
        private readonly IHubContext<NotificationHub> _hubContext;
        private const string DashboardCacheKey = "DashboardData_";
        private readonly TimeSpan _cacheDuration = TimeSpan.FromMinutes(5);

    public HomeController(
        ILogger<HomeController> logger,
        IProjectServiceWithUoW projectService,
        ITaskServiceWithUoW taskService,
        UserManager<ApplicationUser> userManager,
        IMemoryCache cache,
        IHubContext<NotificationHub> hubContext)
        : base(userManager)
    {
        _logger = logger;
        _projectService = projectService;
        _taskService = taskService;
        _userManager = userManager;
        _cache = cache;
        _hubContext = hubContext;
    }

        public IActionResult Index()
        {
            if (User.Identity.IsAuthenticated)
            {
                return RedirectToAction(nameof(Dashboard));
            }
            return View();
        }

        [Authorize]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public async Task<IActionResult> Dashboard(int? projectId = null)
        {
            try
            {
                var userId = GetCurrentUserId();
                /*var cacheKey = $"{DashboardCacheKey}{userId}_{projectId}";

                if (!projectId.HasValue && _cache.TryGetValue(cacheKey, out DashboardViewModel cachedViewModel))
                {
                    _logger.LogInformation("Retrieved dashboard data from cache for user {UserId}", userId);
                    return View(cachedViewModel);
                }*/

                var user = await _userManager.GetUserAsync(User);
                var isManagerOrAdmin = user != null && 
                    (await _userManager.IsInRoleAsync(user, "Manager") || 
                     await _userManager.IsInRoleAsync(user, "Admin"));

                // Get user's projects (where they are owner or member)
                var userProjects = await _projectService.GetProjectsForUserAsync(userId);
                
                // Filter projects if projectId is specified
                var filteredProjects = projectId.HasValue
                    ? userProjects.Where(p => p.Id == projectId.Value).ToList()
                    : userProjects;

                // Get tasks assigned to the user
                var assignedTasks = await _taskService.GetTasksByAssignedUserAsync(userId);
                
                // Get all tasks from projects where the user is a member
                var allUserTasks = new List<TaskItem>();
                
                foreach (var project in filteredProjects)
                {
                    var projectTasks = await _taskService.GetTasksByProjectIdAsync(project.Id);
                    allUserTasks.AddRange(projectTasks);
                }
                
                // Combine assigned tasks with project tasks and remove duplicates
                var combinedTasks = allUserTasks.Concat(assignedTasks)
                    .GroupBy(t => t.Id)
                    .Select(g => g.First())
                    .ToList();
                
                // Create project select list for dropdown
                var projectSelectList = userProjects
                    .Select(p => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
                    {
                        Value = p.Id.ToString(),
                        Text = p.Title,
                        Selected = projectId.HasValue && p.Id == projectId.Value
                    })
                    .ToList();
                
                // Add "All Projects" option
                projectSelectList.Insert(0, new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
                {
                    Value = "",
                    Text = "All Projects",
                    Selected = !projectId.HasValue
                });

                var viewModel = new DashboardViewModel
                {
                    Tasks = combinedTasks,
                    Projects = filteredProjects,
                    ProjectProgress = filteredProjects.Select(p => new ProjectProgress()
                    {
                        ProjectTitle = p.Title,
                        TotalTasks = p.Tasks.Count,
                        CompletedTasks = p.Tasks.Count(t => t.Status == TaskStatus.Completed)
                    }).ToList(),
                    TaskStatusSummary = new TaskStatusSummary()
                    {
                        ToDoCount = combinedTasks.Count(t => t.Status == TaskStatus.ToDo),
                        InProgressCount = combinedTasks.Count(t => t.Status == TaskStatus.InProgress),
                        UnderReviewCount = combinedTasks.Count(t => t.Status == TaskStatus.UnderReview),
                        CompletedCount = combinedTasks.Count(t => t.Status == TaskStatus.Completed),
                        BlockedCount = combinedTasks.Count(t => t.Status == TaskStatus.Blocked)
                    }
                };
                
                ViewData["Projects"] = projectSelectList;
                ViewData["SelectedProjectId"] = projectId;
                //Remove cache
                /*var cacheEntryOptions = new MemoryCacheEntryOptions()
                    .SetSlidingExpiration(_cacheDuration)
                    .SetPriority(CacheItemPriority.Normal);
                
                _cache.Set(cacheKey, viewModel, cacheEntryOptions);
                _logger.LogInformation("Cached dashboard data for user {UserId}", userId);*/

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading dashboard data");
                return Problem("Error loading dashboard data. Please try again later.");
            }
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            var exceptionHandlerPathFeature = HttpContext.Features.Get<IExceptionHandlerPathFeature>();
            var error = exceptionHandlerPathFeature?.Error;

            if (error != null)
            {
                _logger.LogError(error, "Error occurred while processing request");
            }

            return View(new ErrorViewModel
            {
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
            });
        }
        
        // API endpoint to get updated dashboard data
        [Authorize]
        [HttpGet]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public async Task<IActionResult> GetDashboardData(int? projectId = null)
        {
            try
            {
                var userId = GetCurrentUserId();
                
                // Get user's projects (where they are owner or member)
                var userProjects = await _projectService.GetProjectsForUserAsync(userId);
                
                // Filter projects if projectId is specified
                var filteredProjects = projectId.HasValue
                    ? userProjects.Where(p => p.Id == projectId.Value).ToList()
                    : userProjects;

                // Get tasks assigned to the user
                var assignedTasks = await _taskService.GetTasksByAssignedUserAsync(userId);
                
                // Get all tasks from projects where the user is a member
                var allUserTasks = new List<TaskItem>();
                
                foreach (var project in filteredProjects)
                {
                    var projectTasks = await _taskService.GetTasksByProjectIdAsync(project.Id);
                    allUserTasks.AddRange(projectTasks);
                }
                
                // Combine assigned tasks with project tasks and remove duplicates
                var combinedTasks = allUserTasks.Concat(assignedTasks)
                    .GroupBy(t => t.Id)
                    .Select(g => g.First())
                    .ToList();
                
                // Create dashboard data
                var dashboardData = new
                {
                    TotalProjects = filteredProjects.Count,
                    TotalTasks = combinedTasks.Count,
                    CompletedTasks = combinedTasks.Count(t => t.Status == TaskStatus.Completed),
                    overallCompletionRate = combinedTasks.Count > 0 
                        ? (double)combinedTasks.Count(t => t.Status == TaskStatus.Completed) / combinedTasks.Count * 100 
                        : 0,
                    TaskStatusSummary = new
                    {
                        ToDoCount = combinedTasks.Count(t => t.Status == TaskStatus.ToDo),
                        InProgressCount = combinedTasks.Count(t => t.Status == TaskStatus.InProgress),
                        UnderReviewCount = combinedTasks.Count(t => t.Status == TaskStatus.UnderReview),
                        CompletedCount = combinedTasks.Count(t => t.Status == TaskStatus.Completed),
                        BlockedCount = combinedTasks.Count(t => t.Status == TaskStatus.Blocked)
                    },
                    ProjectProgress = filteredProjects.Select(p => new
                    {
                        ProjectTitle = p.Title,
                        TotalTasks = p.Tasks.Count,
                        CompletedTasks = p.Tasks.Count(t => t.Status == TaskStatus.Completed),
                        CompletionPercentage = p.Tasks.Count > 0 
                            ? (double)p.Tasks.Count(t => t.Status == TaskStatus.Completed) / p.Tasks.Count * 100 
                            : 0
                    }).ToList()
                };
                
                return Json(dashboardData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting dashboard data");
                return StatusCode(500, new { error = "Error loading dashboard data" });
            }
        }
        
        // Method to broadcast dashboard update to a specific user
        public async Task BroadcastDashboardUpdate(string userId, int? projectId = null)
        {
            try
            {
                // Clear the dashboard cache for this user
                var cacheKey = $"{DashboardCacheKey}{userId}_{projectId}";
                _cache.Remove(cacheKey);
                
                // Send the dashboard update notification
                await _hubContext.Clients.User(userId).SendAsync("DashboardDataUpdated", projectId);
                _logger.LogInformation("Dashboard update broadcast sent to user {UserId}", userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error broadcasting dashboard update to user {UserId}", userId);
            }
        }
    }
}
