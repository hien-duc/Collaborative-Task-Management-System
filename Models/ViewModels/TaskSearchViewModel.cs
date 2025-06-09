using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Collaborative_Task_Management_System.Models.ViewModels
{
    public class TaskSearchViewModel
    {
        public string Query { get; set; }

        public string AssigneeId { get; set; }

        public TaskStatus? Status { get; set; }
        
        [Display(Name = "Priority")]
        public string Priority { get; set; }

        [Display(Name = "From Date")]
        [DataType(DataType.Date)]
        public DateTime? FromDate { get; set; }

        [Display(Name = "To Date")]
        [DataType(DataType.Date)]
        public DateTime? ToDate { get; set; }

        [Display(Name = "Project")]
        public int? ProjectId { get; set; }

        [Display(Name = "Sort By")]
        public string SortBy { get; set; }

        [Display(Name = "Sort Descending")]
        public bool SortDescending { get; set; }

        public int Page { get; set; } = 1;

        [Display(Name = "Page Size")]
        public int PageSize { get; set; } = 10;

        public IEnumerable<TaskItem> Tasks { get; set; }
        public int TotalTasks { get; set; }
        public int TotalPages => (int)Math.Ceiling(TotalTasks / (double)PageSize);
        public bool HasPreviousPage => Page > 1;
        public bool HasNextPage => Page < TotalPages;
        
        // For populating dropdowns in the view
        public List<SelectListItem> PriorityList => new List<SelectListItem>
        {
            new SelectListItem { Value = "", Text = "All Priorities" },
            new SelectListItem { Value = "High", Text = "High" },
            new SelectListItem { Value = "Medium", Text = "Medium" },
            new SelectListItem { Value = "Low", Text = "Low" },
            new SelectListItem { Value = "Urgent", Text = "Urgent" }
        };
    }
}