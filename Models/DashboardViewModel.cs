using System.Collections.Generic;

namespace Collaborative_Task_Management_System.Models
{
    public class DashboardViewModel
    {
        public List<TaskItem> Tasks { get; set; } = new();
        public List<Project> Projects { get; set; } = new();
        
        public class ProjectProgress
        {
            public string ProjectName { get; set; }
            public int TotalTasks { get; set; }
            public int CompletedTasks { get; set; }
            public double CompletionPercentage => TotalTasks > 0 
                ? (double)CompletedTasks / TotalTasks * 100 
                : 0;
        }
        
        public List<ProjectProgress> ProjectAnalytics { get; set; } = new();
        
        public class TaskStatusSummary
        {
            public int Pending { get; set; }
            public int InProgress { get; set; }
            public int Completed { get; set; }
            public int Blocked { get; set; }
        }
        
        public TaskStatusSummary StatusSummary { get; set; } = new();
        
        public int TotalProjects => Projects?.Count ?? 0;
        public int TotalTasks => Tasks?.Count ?? 0;
        public int CompletedTasks => Tasks?.Count(t => t.Status == TaskStatus.Completed) ?? 0;
        public double OverallCompletionRate => TotalTasks > 0 
            ? (double)CompletedTasks / TotalTasks * 100 
            : 0;
    }
}