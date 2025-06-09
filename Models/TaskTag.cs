using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Collaborative_Task_Management_System.Models
{
    public class TaskTag
    {
        public int TaskId { get; set; }
        public int TagId { get; set; }
        
        // Navigation properties
        [ForeignKey("TaskId")]
        public virtual TaskItem Task { get; set; }
        
        [ForeignKey("TagId")]
        public virtual Tag Tag { get; set; }
    }
}