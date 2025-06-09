// Models/TaskActivityLog.cs
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Collaborative_Task_Management_System.Models
{
    public class TaskActivityLog
    {
        public int Id { get; set; }
        
        [Required]
        public int TaskId { get; set; }
        
        [Required]
        public string UserId { get; set; }
        
        [Required]
        [StringLength(50)]
        public string Action { get; set; }  // Created, Updated, StatusChanged, Commented, etc.
        
        [StringLength(500)]
        public string Description { get; set; }
        
        [StringLength(500)]
        public string OldValues { get; set; }
        
        [StringLength(500)]
        public string NewValues { get; set; }
        
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        
        // Navigation properties
        [ForeignKey("TaskId")]
        public virtual TaskItem Task { get; set; }
        
        [ForeignKey("UserId")]
        public virtual ApplicationUser User { get; set; }
    }
}