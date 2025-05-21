using System.ComponentModel.DataAnnotations;

namespace Collaborative_Task_Management_System.Models.ViewModels
{
    public class CommentCreateViewModel
    {
        [Required]
        public int TaskId { get; set; }

        [Required]
        [StringLength(1000)]
        public string Content { get; set; }
    }
}