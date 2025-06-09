namespace Collaborative_Task_Management_System.Models;

public class TaskStatusSummary
{
    public int ToDoCount { get; set; }
    public int InProgressCount { get; set; }
    public int UnderReviewCount { get; set; }
    public int CompletedCount { get; set; }
    public int BlockedCount { get; set; }
}