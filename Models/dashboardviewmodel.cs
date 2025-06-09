using System.Collections.Generic;
using System.Linq;

namespace Collaborative_Task_Management_System.Models
{
    public class DashboardViewModel
    {
        public List<TaskItem> Tasks { get; set; } = new();
        public List<Project> Projects { get; set; } = new();
        
        
        public List<ProjectProgress> ProjectProgress { get; set; } = new();
        
        public TaskStatusSummary TaskStatusSummary { get; set; } = new();
        
        public int TotalProjects => Projects?.Count ?? 0;
        public int TotalTasks => Tasks?.Count ?? 0;
        public int CompletedTasks => Tasks?.Count(t => t.Status == TaskStatus.Completed) ?? 0;
        public double OverallCompletionRate => TotalTasks > 0 
            ? (double)CompletedTasks / TotalTasks * 100 
            : 0;
    }
}