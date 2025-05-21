using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace Collaborative_Task_Management_System.Models
{
    public class Notification
    {
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; }

        [Required]
        [StringLength(1000)]
        public string Message { get; set; }

        public DateTime CreatedAt { get; set; }
        public bool IsRead { get; set; }

        public NotificationType Type { get; set; }

        // User receiving the notification
        [Required]
        public string UserId { get; set; }
        public ApplicationUser User { get; set; }

        // Optional related entities
        public int? TaskId { get; set; }
        public TaskItem Task { get; set; }

        public int? ProjectId { get; set; }
        public Project Project { get; set; }
    }

    public enum NotificationType
    {
        TaskAssigned,
        TaskStatusChanged,
        TaskCommented,
        ProjectCreated,
        ProjectUpdated,
        MentionedInComment,
        DueDateReminder
    }
}