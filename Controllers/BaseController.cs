using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Collaborative_Task_Management_System.Models;

namespace Collaborative_Task_Management_System.Controllers
{
    public abstract class BaseController : Controller
    {
        protected readonly UserManager<ApplicationUser> _userManager;

        protected BaseController(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        protected async Task<ApplicationUser> GetCurrentUserAsync()
        {
            return await _userManager.GetUserAsync(User);
        }

        protected string GetCurrentUserId()
        {
            return _userManager.GetUserId(User);
        }

        protected async Task<bool> IsUserInRoleAsync(string role)
        {
            var user = await GetCurrentUserAsync();
            return user != null && await _userManager.IsInRoleAsync(user, role);
        }

        protected async Task<bool> CanManageProjectAsync(string projectOwnerId)
        {
            if (await IsUserInRoleAsync("Admin"))
                return true;

            if (await IsUserInRoleAsync("Manager") && projectOwnerId == GetCurrentUserId())
                return true;

            return false;
        }

        protected async Task<bool> IsManagerOrAdmin()
        {
            return await IsUserInRoleAsync("Manager") || await IsUserInRoleAsync("Admin");
        }

        protected void AddModelError(string message)
        {
            ModelState.AddModelError(string.Empty, message);
        }
    }
}