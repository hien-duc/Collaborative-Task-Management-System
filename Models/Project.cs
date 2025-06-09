using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Collaborative_Task_Management_System.Attributes;

namespace Collaborative_Task_Management_System.Models
{
    public class Project
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Title { get; set; }

        public string Description { get; set; }

        [Required]
        [DataType(DataType.Date)]
        [FutureDate(ErrorMessage = "Deadline must be a future date")]
        public DateTime Deadline { get; set; }

        [Required]
        public string CreatedById { get; set; }

        [Required]
        public string OwnerId { get; set; }

        [Required]
        public ProjectStatus Status { get; set; } = ProjectStatus.Planning;

        [Required]
        public string Priority { get; set; } = "Medium";

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        [ForeignKey("CreatedById")]
        public virtual ApplicationUser CreatedBy { get; set; }

        [ForeignKey("OwnerId")]
        public virtual ApplicationUser Owner { get; set; }
    
        public virtual ICollection<TaskItem> Tasks { get; set; } = new List<TaskItem>();
    
        public virtual ICollection<ApplicationUser> TeamMembers { get; set; } = new List<ApplicationUser>();
        
        public virtual ICollection<ProjectMember> ProjectMembers { get; set; } = new List<ProjectMember>();
    }

    public enum ProjectStatus
    {
        Planning,
        Active,
        OnHold,
        Completed,
        Cancelled
    }
}