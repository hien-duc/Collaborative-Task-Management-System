using Microsoft.AspNetCore.Identity;
using Collaborative_Task_Management_System.Models;

namespace Collaborative_Task_Management_System.Data
{
    public static class DataSeeder
    {
        public static async Task SeedRolesAsync(RoleManager<IdentityRole> roleManager)
        {
            string[] roles = { "Admin", "Manager", "Team Member" };

            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }
        }

        public static async Task SeedAdminUserAsync(
            UserManager<ApplicationUser> userManager,
            IConfiguration configuration)
        {
            var adminEmail = configuration["AdminUser:Email"] ?? "admin@taskmanager.com";
            var adminPassword = configuration["AdminUser:Password"] ?? "Admin@123456";

            var adminUser = await userManager.FindByEmailAsync(adminEmail);

            if (adminUser == null)
            {
                adminUser = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    EmailConfirmed = true,
                    FullName = "System Administrator"
                };

                var result = await userManager.CreateAsync(adminUser, adminPassword);

                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, "Admin");
                }
            }
        }

        public static async Task EnsureNewUserRoleAsync(
            UserManager<ApplicationUser> userManager,
            ApplicationUser user)
        {
            var roles = await userManager.GetRolesAsync(user);
            
            if (!roles.Any())
            {
                await userManager.AddToRoleAsync(user, "Team Member");
            }
        }
    }
}