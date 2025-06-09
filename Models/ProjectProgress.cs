namespace Collaborative_Task_Management_System.Models;

public class ProjectProgress
{
    public string ProjectTitle { get; set; }
    public int TotalTasks { get; set; }
    public int CompletedTasks { get; set; }
    public double CompletionPercentage => TotalTasks > 0 
        ? (double)CompletedTasks / TotalTasks * 100 
        : 0;
}