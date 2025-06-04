using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Collaborative_Task_Management_System.Models;
using Collaborative_Task_Management_System.Models.ViewModels;
using Collaborative_Task_Management_System.Data;
using System.IO;
using System.Text.RegularExpressions;
using TaskStatus = Collaborative_Task_Management_System.Models.TaskStatus;

namespace Collaborative_Task_Management_System.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : BaseController
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AdminController> _logger;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly string _logsDirectory;

        public AdminController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            ILogger<AdminController> logger,
            IWebHostEnvironment webHostEnvironment)
            : base(userManager)
        {
            _context = context;
            _logger = logger;
            _roleManager = roleManager;
            _logsDirectory = Path.Combine(webHostEnvironment.ContentRootPath, "Logs");
        }

        // GET: Admin/AuditLogs
        public async Task<IActionResult> AuditLogs(int page = 1)
        {
            try
            {
                const int pageSize = 50;
                var query = _context.AuditLogs
                    .Include(a => a.User)
                    .OrderByDescending(a => a.Timestamp)
                    .AsNoTracking();

                var totalLogs = await query.CountAsync();
                var totalPages = (int)Math.Ceiling(totalLogs / (double)pageSize);

                var logs = await query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                ViewBag.CurrentPage = page;
                ViewBag.TotalPages = totalPages;
                ViewBag.HasPreviousPage = page > 1;
                ViewBag.HasNextPage = page < totalPages;

                return View(logs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving audit logs");
                return Problem("Error retrieving audit logs. Please try again later.");
            }
        }

        // GET: Admin/AuditLogs/Filter
        public async Task<IActionResult> FilterAuditLogs(
            string userId = null,
            string action = null,
            DateTime? fromDate = null,
            DateTime? toDate = null,
            int page = 1)
        {
            try
            {
                const int pageSize = 50;
                var query = _context.AuditLogs
                    .Include(a => a.User)
                    .AsNoTracking();

                if (!string.IsNullOrEmpty(userId))
                {
                    query = query.Where(a => a.UserId == userId);
                }

                if (!string.IsNullOrEmpty(action))
                {
                    query = query.Where(a => a.Action.Contains(action));
                }

                if (fromDate.HasValue)
                {
                    query = query.Where(a => a.Timestamp >= fromDate.Value);
                }

                if (toDate.HasValue)
                {
                    query = query.Where(a => a.Timestamp <= toDate.Value);
                }

                query = query.OrderByDescending(a => a.Timestamp);

                var totalLogs = await query.CountAsync();
                var totalPages = (int)Math.Ceiling(totalLogs / (double)pageSize);

                var logs = await query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                ViewBag.CurrentPage = page;
                ViewBag.TotalPages = totalPages;
                ViewBag.HasPreviousPage = page > 1;
                ViewBag.HasNextPage = page < totalPages;
                ViewBag.FilterUserId = userId;
                ViewBag.FilterAction = action;
                ViewBag.FilterFromDate = fromDate?.ToString("yyyy-MM-dd");
                ViewBag.FilterToDate = toDate?.ToString("yyyy-MM-dd");

                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return PartialView("_AuditLogsList", logs);
                }

                return View("AuditLogs", logs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error filtering audit logs");
                return Problem("Error filtering audit logs. Please try again later.");
            }
        }

        // GET: Admin/Users
        // GET: Admin/Users
        public async Task<IActionResult> Users()
        {
            try
            {
                var users = await _userManager.Users
                    .OrderBy(u => u.UserName)
                    .AsNoTracking()
                    .ToListAsync();

                var userViewModels = new List<UserViewModel>();
                foreach (var user in users)
                {
                    var roles = await _userManager.GetRolesAsync(user);
                    userViewModels.Add(UserViewModel.FromApplicationUser(user, roles));
                }

                return View(userViewModels);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving users list");
                return Problem("Error retrieving users list. Please try again later.");
            }
        }

        // GET: Admin/EditUser/5
        public async Task<IActionResult> EditUser(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return BadRequest();
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            var roles = await _userManager.GetRolesAsync(user);
            var viewModel = UserViewModel.FromApplicationUser(user, roles);

            return View(viewModel);
        }

        // POST: Admin/EditUser/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditUser(string id, UserViewModel model)
        {
            if (id != model.Id)
            {
                return BadRequest();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var user = await _userManager.FindByIdAsync(id);
                    if (user == null)
                    {
                        return NotFound();
                    }

                    user.Email = model.Email;
                    user.FullName = model.FullName;

                    var result = await _userManager.UpdateAsync(user);
                    if (result.Succeeded)
                    {
                        // Update roles
                        var currentRoles = await _userManager.GetRolesAsync(user);
                        var rolesToRemove = currentRoles.Except(model.Roles);
                        var rolesToAdd = model.Roles.Except(currentRoles);

                        await _userManager.RemoveFromRolesAsync(user, rolesToRemove);
                        await _userManager.AddToRolesAsync(user, rolesToAdd);

                        _logger.LogInformation("User {UserId} updated successfully", id);
                        return RedirectToAction(nameof(Users));
                    }

                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError("", error.Description);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating user {UserId}", id);
                    ModelState.AddModelError("", "An error occurred while updating the user.");
                }
            }

            return View(model);
        }

        // POST: Admin/DeleteUser/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUser(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return Json(new { success = false, message = "Invalid user ID" });
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return Json(new { success = false, message = "User not found" });
            }

            try
            {
                var result = await _userManager.DeleteAsync(user);
                if (result.Succeeded)
                {
                    _logger.LogInformation("User {UserId} deleted by admin", id);
                    return Json(new { success = true });
                }
                else
                {
                    return Json(new { success = false, message = "Failed to delete user" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user {UserId}", id);
                return Json(new { success = false, message = "An error occurred while deleting the user" });
            }
        }
        
        // GET: Admin/Dashboard
        public async Task<IActionResult> Dashboard()
        {
            try
            {
                var viewModel = new AdminDashboardViewModel();
                
                // Get user statistics
                var users = await _userManager.Users.ToListAsync();
                viewModel.TotalUsers = users.Count;
                
                // Get role counts
                var adminRole = await _roleManager.FindByNameAsync("Admin");
                var managerRole = await _roleManager.FindByNameAsync("Manager");
                
                if (adminRole != null)
                {
                    viewModel.AdminCount = await _context.UserRoles
                        .Where(ur => ur.RoleId == adminRole.Id)
                        .CountAsync();
                }
                
                if (managerRole != null)
                {
                    viewModel.ManagerCount = await _context.UserRoles
                        .Where(ur => ur.RoleId == managerRole.Id)
                        .CountAsync();
                }
                
                viewModel.TeamMemberCount = viewModel.TotalUsers - viewModel.AdminCount - viewModel.ManagerCount;
                
                // Get project statistics
                var projects = await _context.Projects.ToListAsync();
                viewModel.TotalProjects = projects.Count;
                viewModel.ActiveProjects = projects.Count(p => p.Status == ProjectStatus.Active);
                viewModel.CompletedProjects = projects.Count(p => p.Status == ProjectStatus.Completed);
                viewModel.PlanningProjects = projects.Count(p => p.Status == ProjectStatus.Planning);
                viewModel.OnHoldProjects = projects.Count(p => p.Status == ProjectStatus.OnHold);
                viewModel.CancelledProjects = projects.Count(p => p.Status == ProjectStatus.Cancelled);
                
                // Get task statistics
                var tasks = await _context.Tasks.ToListAsync();
                viewModel.TotalTasks = tasks.Count;
                viewModel.TodoTasks = tasks.Count(t => t.Status == TaskStatus.ToDo);
                viewModel.InProgressTasks = tasks.Count(t => t.Status == TaskStatus.InProgress);
                viewModel.UnderReviewTasks = tasks.Count(t => t.Status == TaskStatus.UnderReview);
                viewModel.CompletedTasks = tasks.Count(t => t.Status == TaskStatus.Completed);
                viewModel.BlockedTasks = tasks.Count(t => t.Status == TaskStatus.Blocked);
                viewModel.OverdueTasks = tasks.Count(t => t.DueDate < DateTime.Today && t.Status != TaskStatus.Completed);
                
                // Get recent activity
                viewModel.RecentActivity = await _context.AuditLogs
                    .Include(a => a.User)
                    .OrderByDescending(a => a.Timestamp)
                    .Take(10)
                    .ToListAsync();
                
                // Get top users by task completion
                var userTaskSummaries = new List<AdminDashboardViewModel.UserTaskSummary>();
                foreach (var user in users)
                {
                    var assignedTasks = tasks.Where(t => t.AssignedToId == user.Id).ToList();
                    if (assignedTasks.Any())
                    {
                        userTaskSummaries.Add(new AdminDashboardViewModel.UserTaskSummary
                        {
                            UserId = user.Id,
                            UserName = user.UserName,
                            FullName = user.FullName,
                            TotalAssignedTasks = assignedTasks.Count,
                            CompletedTasks = assignedTasks.Count(t => t.Status == TaskStatus.Completed)
                        });
                    }
                }
                
                viewModel.TopUsers = userTaskSummaries
                    .OrderByDescending(u => u.CompletionRate)
                    .Take(5)
                    .ToList();
                
                // Get system health information
                viewModel.ErrorCount = await GetErrorCountFromLogs();
                viewModel.LastErrorTime = await GetLastErrorTimeFromLogs();
                viewModel.SystemUptime = GetSystemUptime();
                
                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating admin dashboard");
                return Problem("Error generating admin dashboard. Please try again later.");
            }
        }
        
        private async Task<int> GetErrorCountFromLogs()
        {
            try
            {
                if (!Directory.Exists(_logsDirectory))
                    return 0;
                    
                int errorCount = 0;
                var logFiles = Directory.GetFiles(_logsDirectory, "log-*.txt");
                
                foreach (var logFile in logFiles)
                {
                    var content = await System.IO.File.ReadAllTextAsync(logFile);
                    errorCount += Regex.Matches(content, @"\[ERR\]").Count;
                }
                
                return errorCount;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error counting errors from log files");
                return 0;
            }
        }
        
        private async Task<DateTime?> GetLastErrorTimeFromLogs()
        {
            try
            {
                if (!Directory.Exists(_logsDirectory))
                    return null;
                    
                DateTime? lastErrorTime = null;
                var logFiles = Directory.GetFiles(_logsDirectory, "log-*.txt");
                
                foreach (var logFile in logFiles)
                {
                    var lines = await System.IO.File.ReadAllLinesAsync(logFile);
                    foreach (var line in lines)
                    {
                        if (line.Contains("[ERR]"))
                        {
                            var match = Regex.Match(line, @"\[(\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2})\]");
                            if (match.Success && DateTime.TryParse(match.Groups[1].Value, out DateTime errorTime))
                            {
                                if (!lastErrorTime.HasValue || errorTime > lastErrorTime.Value)
                                {
                                    lastErrorTime = errorTime;
                                }
                            }
                        }
                    }
                }
                
                return lastErrorTime;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting last error time from log files");
                return null;
            }
        }
        
        private double GetSystemUptime()
        {
            try
            {
                var process = System.Diagnostics.Process.GetCurrentProcess();
                var uptime = (DateTime.Now - process.StartTime).TotalHours;
                return Math.Round(uptime, 2);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating system uptime");
                return 0;
            }
        }
    }
}