using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Collaborative_Task_Management_System.Models
{
    public class TaskDependency
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [Display(Name = "Task")]
        public int TaskId { get; set; }

        [Required]
        [Display(Name = "Depends On")]
        public int BlockingTaskId { get; set; }

        [Display(Name = "Created At")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey("TaskId")]
        public virtual TaskItem Task { get; set; }

        [ForeignKey("BlockingTaskId")]
        [Display(Name = "Depends On")]
        public virtual TaskItem BlockingTask { get; set; }
    }
}
