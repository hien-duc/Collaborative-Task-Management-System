using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Collaborative_Task_Management_System.Models
{
    public class TaskItem
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Title { get; set; }

        public string Description { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime DueDate { get; set; }

        [Required]
        [RegularExpression("^(Low|Medium|High)$", ErrorMessage = "Priority must be Low, Medium, or High")]
        public string Priority { get; set; } = "Medium";

        [Required]
        public TaskStatus Status { get; set; } = TaskStatus.ToDo;

        // Project relationship
        [Required]
        public int ProjectId { get; set; }

        [ForeignKey("ProjectId")]
        public virtual Project? Project { get; set; }

        // Assignment
        public string AssignedUserId { get; set; }
        
        // Alias for compatibility
        public string AssignedToId 
        { 
            get => AssignedUserId; 
            set => AssignedUserId = value; 
        }
        
        // Alias for compatibility  
        public string AssigneeId 
        { 
            get => AssignedUserId; 
            set => AssignedUserId = value; 
        }

        [ForeignKey("AssignedUserId")]
        public virtual ApplicationUser? AssignedUser { get; set; }
        
        // Alias for compatibility
        [NotMapped]
        public virtual ApplicationUser? AssignedTo 
        { 
            get => AssignedUser; 
            set => AssignedUser = value; 
        }

        // Creator
        [Required]
        public string CreatedById { get; set; }

        [ForeignKey("CreatedById")]
        public virtual ApplicationUser? CreatedBy { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual ICollection<Comment>? Comments { get; set; }
        public virtual ICollection<Notification>? Notifications { get; set; }
        public virtual ICollection<FileAttachment>? FileAttachments { get; set; }
        
        [Display(Name = "Blocked By")]
        public virtual ICollection<TaskDependency> BlockedByTasks { get; set; }
        
        [Display(Name = "Blocks")]
        public virtual ICollection<TaskDependency> BlockingTasks { get; set; }
        
        public virtual ICollection<TaskActivityLog> ActivityLogs { get; set; }
        public virtual ICollection<TaskTimeEntry> TimeEntries { get; set; }
        public virtual ICollection<TaskChecklistItem> ChecklistItems { get; set; }
        public virtual ICollection<TaskTag> TaskTags { get; set; }
        
        [NotMapped]
        public bool IsBlocked => BlockedByTasks?.Any() == true;
        
        [NotMapped]
        public string BlockedByTaskIds => string.Join(",", BlockedByTasks?.Select(t => t.BlockingTaskId) ?? Enumerable.Empty<int>());
        
        [NotMapped]
        public double TotalTimeSpent => 
            TimeEntries?.Where(t => t.Duration.HasValue).Sum(t => t.Duration.Value.TotalHours) ?? 0;
            
        [NotMapped]
        public double CompletionPercentage
        {
            get
            {
                var items = ChecklistItems?.ToList();
                if (items == null || !items.Any())
                    return Status == TaskStatus.Completed ? 100 : 0;
                    
                return (double)items.Count(i => i.IsCompleted) / items.Count * 100;
            }
        }

        public TaskItem()
        {
            Comments = new HashSet<Comment>();
            FileAttachments = new HashSet<FileAttachment>();
            BlockedByTasks = new HashSet<TaskDependency>();
            BlockingTasks = new HashSet<TaskDependency>();
            ActivityLogs = new HashSet<TaskActivityLog>();
            TimeEntries = new HashSet<TaskTimeEntry>();
            ChecklistItems = new HashSet<TaskChecklistItem>();
            TaskTags = new HashSet<TaskTag>();
        }
    }
}

namespace Collaborative_Task_Management_System.Models
{
    public enum TaskPriority
    {
        Low,
        Medium,
        High,
        Urgent
    }

    public enum TaskStatus
    {
        ToDo,
        InProgress,
        UnderReview,
        Completed,
        Blocked
    }
}
