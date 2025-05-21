using System.ComponentModel.DataAnnotations;

namespace Collaborative_Task_Management_System.Models.ViewModels
{
    public class TaskSearchViewModel
    {
        public string Query { get; set; }

        public string AssigneeId { get; set; }

        public TaskStatus? Status { get; set; }

        public DateTime? FromDate { get; set; }

        public DateTime? ToDate { get; set; }

        public int? ProjectId { get; set; }

        public string SortBy { get; set; }

        public bool SortDescending { get; set; }

        public int Page { get; set; } = 1;

        public int PageSize { get; set; } = 10;

        public IEnumerable<TaskItem> Tasks { get; set; }

        public int TotalTasks { get; set; }

        public int TotalPages => (int)Math.Ceiling(TotalTasks / (double)PageSize);

        public bool HasPreviousPage => Page > 1;

        public bool HasNextPage => Page < TotalPages;
    }
}