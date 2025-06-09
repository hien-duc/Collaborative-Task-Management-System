// Models/TaskChecklistItem.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Collaborative_Task_Management_System.Models
{
    public class TaskChecklistItem
    {
        public int Id { get; set; }
        
        [Required]
        public int TaskId { get; set; }
        
        [Required]
        [StringLength(500)]
        public string Description { get; set; }
        
        public bool IsCompleted { get; set; }
        
        public int Position { get; set; }  // For ordering
        
        // Navigation property
        [ForeignKey("TaskId")]
        public virtual TaskItem Task { get; set; }
    }
}