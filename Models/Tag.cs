using System.ComponentModel.DataAnnotations;

namespace Collaborative_Task_Management_System.Models
{
    public class Tag
    {
        public int Id { get; set; }
        
        [Required]
        [StringLength(50)]
        public string Name { get; set; }
        
        [StringLength(7)]  // For hex color codes
        public string Color { get; set; } = "#007bff";  // Default blue
        
        // Navigation property for many-to-many relationship
        public virtual ICollection<TaskTag> TaskTags { get; set; }
    }
}