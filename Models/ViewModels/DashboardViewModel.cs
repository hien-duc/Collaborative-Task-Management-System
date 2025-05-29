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

        public List<ProjectProgress> ProjectProgresses { get; set; } = new();

        public class TaskStatusSummary
        {
            public int PendingTasks { get; set; }
            public int InProgressTasks { get; set; }
            public int CompletedTasks { get; set; }
            public int BlockedTasks { get; set; }
        }

        public TaskStatusSummary TaskStatusSummaries { get; set; } = new();

        public int TotalProjects { get; set; }
        public int TotalTasks { get; set; }
        public int CompletedTasks { get; set; }
        public double OverallCompletionRate => TotalTasks > 0
            ? (double)CompletedTasks / TotalTasks * 100
            : 0;
    }
}