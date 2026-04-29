using Collaborative_Task_Management_System.Models;

namespace Collaborative_Task_Management_System.Specifications
{
    public class TasksByProjectIdsSpecification : BaseSpecification<TaskItem>
    {
        public TasksByProjectIdsSpecification(IEnumerable<int> projectIds)
            : base(t => projectIds.Contains(t.ProjectId))
        {
            AddInclude(t => t.AssignedUser);
            AddInclude(t => t.Project);
            ApplyOrderByDescending(t => t.CreatedAt);
        }
    }
}
