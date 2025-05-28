using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Collaborative_Task_Management_System.Models
{
    public class Project
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Title { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        public string Description { get; set; }

        [Required]
        [DataType(DataType.Date)]
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
        
        public virtual ICollection<TaskItem> Tasks { get; set; }
        
        public virtual ICollection<ApplicationUser> TeamMembers { get; set; }
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