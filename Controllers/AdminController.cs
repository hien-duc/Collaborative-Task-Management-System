using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Collaborative_Task_Management_System.Models;
using Collaborative_Task_Management_System.Models.ViewModels;
using Collaborative_Task_Management_System.Data;

namespace Collaborative_Task_Management_System.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : BaseController
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AdminController> _logger;

        public AdminController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            ILogger<AdminController> logger)
            : base(userManager)
        {
            _context = context;
            _logger = logger;
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
    }
}