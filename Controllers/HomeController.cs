using System.Diagnostics;
using Collaborative_Task_Management_System.Models;
using Microsoft.AspNetCore.Mvc;

namespace Collaborative_Task_Management_System.Controllers
{
    public class HomeController : BaseController
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IProjectService _projectService;
        private readonly ITaskService _taskService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IMemoryCache _cache;
        private const string DashboardCacheKey = "DashboardData_";
        private readonly TimeSpan _cacheDuration = TimeSpan.FromMinutes(5);

        public HomeController(
            ILogger<HomeController> logger,
            IProjectService projectService,
            ITaskService taskService,
            UserManager<ApplicationUser> userManager,
            IMemoryCache cache)
            : base(userManager)
        {
            _logger = logger;
            _projectService = projectService;
            _taskService = taskService;
            _userManager = userManager;
            _cache = cache;
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
        public async Task<IActionResult> Dashboard()
        {
            try
            {
                var userId = GetCurrentUserId();
                var cacheKey = $"{DashboardCacheKey}{userId}";

                if (_cache.TryGetValue(cacheKey, out DashboardViewModel cachedViewModel))
                {
                    _logger.LogInformation("Retrieved dashboard data from cache for user {UserId}", userId);
                    return View(cachedViewModel);
                }

                var isManagerOrAdmin = await IsManagerOrAdmin();

                // Get user's tasks
                var tasks = await _taskService.GetTasksByUserIdAsync(userId);

                // Get user's projects
                var projects = isManagerOrAdmin
                    ? await _projectService.GetAllProjectsAsync()
                    : await _projectService.GetProjectsByUserIdAsync(userId);

                var viewModel = new DashboardViewModel
                {
                    Tasks = tasks,
                    Projects = projects,
                    TotalProjects = projects.Count(),
                    TotalTasks = tasks.Count(),
                    CompletedTasks = tasks.Count(t => t.Status == TaskStatus.Completed),
                    ProjectProgress = projects.Select(p => new DashboardViewModel.ProjectProgress
                    {
                        ProjectName = p.Name,
                        TotalTasks = p.Tasks.Count,
                        CompletedTasks = p.Tasks.Count(t => t.Status == TaskStatus.Completed)
                    }).ToList(),
                    TaskStatusSummary = new DashboardViewModel.TaskStatusSummary
                    {
                        PendingTasks = tasks.Count(t => t.Status == TaskStatus.Pending),
                        InProgressTasks = tasks.Count(t => t.Status == TaskStatus.InProgress),
                        CompletedTasks = tasks.Count(t => t.Status == TaskStatus.Completed),
                        BlockedTasks = tasks.Count(t => t.Status == TaskStatus.Blocked)
                    }
                };

                var cacheEntryOptions = new MemoryCacheEntryOptions()
                    .SetSlidingExpiration(_cacheDuration)
                    .SetPriority(CacheItemPriority.Normal);

                _cache.Set(cacheKey, viewModel, cacheEntryOptions);
                _logger.LogInformation("Cached dashboard data for user {UserId}", userId);

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
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier,
                ShowRequestId = true
            });
        }
    }
}
