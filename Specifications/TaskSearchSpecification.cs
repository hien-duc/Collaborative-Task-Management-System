using Collaborative_Task_Management_System.Models;
using Collaborative_Task_Management_System.Models.ViewModels;

namespace Collaborative_Task_Management_System.Specifications
{
    public class TaskSearchSpecification : BaseSpecification<TaskItem>
    {
        public TaskSearchSpecification(TaskSearchViewModel model, string currentUserId, bool isManagerOrAdmin, List<int> userProjectIds)
            : base(t => 
                (string.IsNullOrEmpty(model.Query) || t.Title.Contains(model.Query) || t.Description.Contains(model.Query) || t.AssignedTo.UserName.Contains(model.Query)) &&
                (string.IsNullOrEmpty(model.AssigneeId) || t.AssignedToId == model.AssigneeId) &&
                (!model.Status.HasValue || t.Status == model.Status.Value) &&
                (!model.FromDate.HasValue || t.DueDate >= model.FromDate.Value) &&
                (!model.ToDate.HasValue || t.DueDate <= model.ToDate.Value) &&
                (!model.ProjectId.HasValue || t.ProjectId == model.ProjectId.Value) &&
                (isManagerOrAdmin || t.AssignedToId == currentUserId || userProjectIds.Contains(t.ProjectId)))
        {
            AddInclude(t => t.AssignedTo);
            AddInclude(t => t.Project);

            var sortBy = model.SortBy?.ToLower();
            if (model.SortDescending)
            {
                switch (sortBy)
                {
                    case "title":
                        ApplyOrderByDescending(t => t.Title);
                        break;
                    case "duedate":
                        ApplyOrderByDescending(t => t.DueDate);
                        break;
                    case "status":
                        ApplyOrderByDescending(t => t.Status);
                        break;
                    case "assignee":
                        ApplyOrderByDescending(t => t.AssignedTo.UserName);
                        break;
                    default:
                        ApplyOrderByDescending(t => t.CreatedAt);
                        break;
                }
            }
            else
            {
                switch (sortBy)
                {
                    case "title":
                        ApplyOrderBy(t => t.Title);
                        break;
                    case "duedate":
                        ApplyOrderBy(t => t.DueDate);
                        break;
                    case "status":
                        ApplyOrderBy(t => t.Status);
                        break;
                    case "assignee":
                        ApplyOrderBy(t => t.AssignedTo.UserName);
                        break;
                    default:
                        ApplyOrderByDescending(t => t.CreatedAt);
                        break;
                }
            }

            ApplyPaging((model.Page - 1) * model.PageSize, model.PageSize);
        }
    }

    public class TaskSearchCountSpecification : BaseSpecification<TaskItem>
    {
        public TaskSearchCountSpecification(TaskSearchViewModel model, string currentUserId, bool isManagerOrAdmin, List<int> userProjectIds)
            : base(t => 
                (string.IsNullOrEmpty(model.Query) || t.Title.Contains(model.Query) || t.Description.Contains(model.Query) || t.AssignedTo.UserName.Contains(model.Query)) &&
                (string.IsNullOrEmpty(model.AssigneeId) || t.AssignedToId == model.AssigneeId) &&
                (!model.Status.HasValue || t.Status == model.Status.Value) &&
                (!model.FromDate.HasValue || t.DueDate >= model.FromDate.Value) &&
                (!model.ToDate.HasValue || t.DueDate <= model.ToDate.Value) &&
                (!model.ProjectId.HasValue || t.ProjectId == model.ProjectId.Value) &&
                (isManagerOrAdmin || t.AssignedToId == currentUserId || userProjectIds.Contains(t.ProjectId)))
        {
        }
    }
}
