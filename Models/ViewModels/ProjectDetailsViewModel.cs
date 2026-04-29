using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;

namespace Collaborative_Task_Management_System.Models.ViewModels
{
    public class ProjectDetailsViewModel
    {
        public Project Project { get; set; }
        public List<TaskItem> Tasks { get; set; }
        public List<ProjectMember> ProjectMembers { get; set; }
        public List<SelectListItem> AvailableUsers { get; set; }
        public bool IsManager { get; set; }
        public bool IsMember { get; set; }
    }
}