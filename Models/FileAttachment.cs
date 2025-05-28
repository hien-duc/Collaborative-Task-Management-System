using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Collaborative_Task_Management_System.Models
{
    public class FileAttachment
    {
        public int Id { get; set; }

        [Required]
        public int TaskId { get; set; }

        [ForeignKey("TaskId")]
        public virtual TaskItem Task { get; set; }

        [Required]
        [StringLength(255)]
        public string FileName { get; set; }

        [Required]
        [StringLength(255)]
        public string FilePath { get; set; }

        [Required]
        public string UploadedById { get; set; }

        [ForeignKey("UploadedById")]
        public virtual ApplicationUser UploadedBy { get; set; }

        [Required]
        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
        
        [Required]
        public long FileSize { get; set; }
        
        [Required]
        [StringLength(100)]
        public string ContentType { get; set; }
    }
}