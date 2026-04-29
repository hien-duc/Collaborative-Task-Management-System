using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Collaborative_Task_Management_System.Models;
using Collaborative_Task_Management_System.Models.ViewModels;
using System.IO;
using System.Text.RegularExpressions;
using Collaborative_Task_Management_System.UnitOfWork;
using Collaborative_Task_Management_System.Specifications;
using TaskStatus = Collaborative_Task_Management_System.Models.TaskStatus;

namespace Collaborative_Task_Management_System.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : BaseController
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<AdminController> _logger;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly string _logsDirectory;

        public AdminController(
            IUnitOfWork unitOfWork,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            ILogger<AdminController> logger,
            IWebHostEnvironment webHostEnvironment)
            : base(userManager)
        {
            _unitOfWork = unitOfWork;
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
                var spec = new AuditLogSearchSpecification(null, null, null, null, (page - 1) * pageSize, pageSize);
                var countSpec = new AuditLogSearchCountSpecification(null, null, null, null);

                var totalLogs = await _unitOfWork.AuditLogs.CountAsync(countSpec);
                var totalPages = (int)Math.Ceiling(totalLogs / (double)pageSize);

                var logs = await _unitOfWork.AuditLogs.ListAsync(spec);

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
                // Adjust toDate to include the full day
                var adjustedToDate = toDate?.AddDays(1).AddTicks(-1);
                
                var spec = new AuditLogSearchSpecification(userId, action, fromDate, adjustedToDate, (page - 1) * pageSize, pageSize);
                var countSpec = new AuditLogSearchCountSpecification(userId, action, fromDate, adjustedToDate);

                var totalLogs = await _unitOfWork.AuditLogs.CountAsync(countSpec);
                var totalPages = (int)Math.Ceiling(totalLogs / (double)pageSize);

                var logs = await _unitOfWork.AuditLogs.ListAsync(spec);

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
        public async Task<IActionResult> EditUser(string id, UserViewModel model, string? newPassword)
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
                    if (newPassword != null)
                    {
                        user.PasswordHash = _userManager.PasswordHasher.HashPassword(user, newPassword);
                    }

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
                    viewModel.AdminCount = (await _userManager.GetUsersInRoleAsync("Admin")).Count;
                }
                
                if (managerRole != null)
                {
                    viewModel.ManagerCount = (await _userManager.GetUsersInRoleAsync("Manager")).Count;
                }
                
                viewModel.TeamMemberCount = viewModel.TotalUsers - viewModel.AdminCount - viewModel.ManagerCount;
                
                // Get project statistics
                var projects = (await _unitOfWork.Projects.GetAllAsync()).ToList();
                viewModel.TotalProjects = projects.Count;
                viewModel.ActiveProjects = projects.Count(p => p.Status == ProjectStatus.Active);
                viewModel.CompletedProjects = projects.Count(p => p.Status == ProjectStatus.Completed);
                viewModel.PlanningProjects = projects.Count(p => p.Status == ProjectStatus.Planning);
                viewModel.OnHoldProjects = projects.Count(p => p.Status == ProjectStatus.OnHold);
                viewModel.CancelledProjects = projects.Count(p => p.Status == ProjectStatus.Cancelled);
                
                // Get task statistics
                var tasks = (await _unitOfWork.Tasks.GetAllAsync()).ToList();
                viewModel.TotalTasks = tasks.Count;
                viewModel.TodoTasks = tasks.Count(t => t.Status == TaskStatus.ToDo);
                viewModel.InProgressTasks = tasks.Count(t => t.Status == TaskStatus.InProgress);
                viewModel.UnderReviewTasks = tasks.Count(t => t.Status == TaskStatus.UnderReview);
                viewModel.CompletedTasks = tasks.Count(t => t.Status == TaskStatus.Completed);
                viewModel.BlockedTasks = tasks.Count(t => t.Status == TaskStatus.Blocked);
                viewModel.OverdueTasks = tasks.Count(t => t.DueDate < DateTime.Today && t.Status != TaskStatus.Completed);
                
                // Get recent activity
                var recentSpec = new AuditLogRecentSpecification(10);
                viewModel.RecentActivity = (await _unitOfWork.AuditLogs.ListAsync(recentSpec)).ToList();
                
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
                    using (var fileStream = new FileStream(logFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    using (var reader = new StreamReader(fileStream))
                    {
                        var content = await reader.ReadToEndAsync();
                        errorCount += Regex.Matches(content, @"\[ERR\]").Count;
                    }
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
                    using (var fileStream = new FileStream(logFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    using (var reader = new StreamReader(fileStream))
                    {
                        string line;
                        while ((line = await reader.ReadLineAsync()) != null)
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