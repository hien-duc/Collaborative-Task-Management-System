using System.Collections.Generic;
using Collaborative_Task_Management_System.Models;

namespace Collaborative_Task_Management_System.Models.ViewModels
{
    public class UserViewModel
    {
        public string Id { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
        public string FullName { get; set; }
        public IList<string> Roles { get; set; }

        public static UserViewModel FromApplicationUser(ApplicationUser user, IList<string> roles)
        {
            return new UserViewModel
            {
                Id = user.Id,
                UserName = user.UserName,
                Email = user.Email,
                FullName = user.FullName,
                Roles = roles
            };
        }
    }
}