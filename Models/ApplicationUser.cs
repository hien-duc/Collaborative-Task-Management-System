using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace Collaborative_Task_Management_System.Models
{
    public class ApplicationUser : IdentityUser
    {
        [Required]
        [StringLength(100)]
        public string FullName { get; set; }
        
        [Required]
        public bool IsDeleted { get; set; } = false;
        

        // Navigation properties for projects created by the user
        public virtual ICollection<Project> CreatedProjects { get; set; }

        // Navigation properties for tasks
        public virtual ICollection<TaskItem> AssignedTasks { get; set; }
        public virtual ICollection<TaskItem> CreatedTasks { get; set; }

        // Navigation properties for comments and files
        public virtual ICollection<Comment> Comments { get; set; }
        public virtual ICollection<FileAttachment> FileAttachments { get; set; }

        // Navigation property for audit logs
        public virtual ICollection<AuditLog> AuditLogs { get; set; }
        
        // Navigation property for project memberships
        public virtual ICollection<ProjectMember> ProjectMemberships { get; set; }

        public ApplicationUser()
        {
            CreatedProjects = new HashSet<Project>();
            AssignedTasks = new HashSet<TaskItem>();
            CreatedTasks = new HashSet<TaskItem>();
            Comments = new HashSet<Comment>();
            FileAttachments = new HashSet<FileAttachment>();
            AuditLogs = new HashSet<AuditLog>();
            ProjectMemberships = new HashSet<ProjectMember>();
        }
    }
}