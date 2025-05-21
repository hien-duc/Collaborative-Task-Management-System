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

        public string Description { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime Deadline { get; set; }

        [Required]
        public string CreatedById { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey("CreatedById")]
        public virtual ApplicationUser CreatedBy { get; set; }
        
        public virtual ICollection<TaskItem> Tasks { get; set; }
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