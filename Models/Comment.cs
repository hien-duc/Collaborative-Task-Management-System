using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Collaborative_Task_Management_System.Models
{
    public class Comment
    {
        public int Id { get; set; }

        [Required]
        [StringLength(1000)]
        public string Text { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Task relationship
        [Required]
        public int TaskId { get; set; }

        [ForeignKey("TaskId")]
        public virtual TaskItem Task { get; set; }

        // User who created the comment
        [Required]
        public string UserId { get; set; }

        [ForeignKey("UserId")]
        public virtual ApplicationUser User { get; set; }
    }
}