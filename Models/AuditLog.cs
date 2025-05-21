using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Collaborative_Task_Management_System.Models
{
    public class AuditLog
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; }

        [ForeignKey("UserId")]
        public virtual ApplicationUser User { get; set; }

        [Required]
        [StringLength(100)]
        public string Action { get; set; }

        public string Details { get; set; }

        [Required]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}