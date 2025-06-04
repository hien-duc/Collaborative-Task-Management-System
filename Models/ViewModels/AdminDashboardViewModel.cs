using System.Collections.Generic;
using System.Linq;

namespace Collaborative_Task_Management_System.Models.ViewModels
{
    public class AdminDashboardViewModel
    {
        // System Statistics
        public int TotalUsers { get; set; }
        public int TotalProjects { get; set; }
        public int TotalTasks { get; set; }
        public int ActiveProjects { get; set; }
        public int CompletedProjects { get; set; }
        public int OverdueTasks { get; set; }
        
        // User Statistics
        public int AdminCount { get; set; }
        public int ManagerCount { get; set; }
        public int TeamMemberCount { get; set; }
        
        // Project Status Distribution
        public int PlanningProjects { get; set; }
        public int OnHoldProjects { get; set; }
        public int CancelledProjects { get; set; }
        
        // Task Status Distribution
        public int TodoTasks { get; set; }
        public int InProgressTasks { get; set; }
        public int UnderReviewTasks { get; set; }
        public int CompletedTasks { get; set; }
        public int BlockedTasks { get; set; }
        
        // Recent Activity
        public List<AuditLog> RecentActivity { get; set; } = new List<AuditLog>();
        
        // Top Users by Task Completion
        public List<UserTaskSummary> TopUsers { get; set; } = new List<UserTaskSummary>();
        
        public class UserTaskSummary
        {
            public string UserId { get; set; }
            public string UserName { get; set; }
            public string FullName { get; set; }
            public int CompletedTasks { get; set; }
            public int TotalAssignedTasks { get; set; }
            public double CompletionRate => TotalAssignedTasks > 0 
                ? (double)CompletedTasks / TotalAssignedTasks * 100 
                : 0;
        }
        
        // System Health
        public int ErrorCount { get; set; }
        public DateTime? LastErrorTime { get; set; }
        public double SystemUptime { get; set; } // in hours
    }
}