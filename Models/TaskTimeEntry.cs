// Models/TaskTimeEntry.cs
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Collaborative_Task_Management_System.Models
{
    public class TaskTimeEntry
    {
        public int Id { get; set; }
        
        [Required]
        public int TaskId { get; set; }
        
        [Required]
        public string UserId { get; set; }
        
        [Required]
        public DateTime StartTime { get; set; }
        
        public DateTime? EndTime { get; set; }
        
        [StringLength(500)]
        public string Notes { get; set; }
        
        // Calculated property for duration
        [NotMapped]
        public TimeSpan? Duration => 
            EndTime.HasValue ? EndTime.Value - StartTime : null;
            
        // Navigation properties
        [ForeignKey("TaskId")]
        public virtual TaskItem Task { get; set; }
        
        [ForeignKey("UserId")]
        public virtual ApplicationUser User { get; set; }
    }
}