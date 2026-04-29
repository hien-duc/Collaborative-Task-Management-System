using Collaborative_Task_Management_System.Models;
using System.Linq.Expressions;

namespace Collaborative_Task_Management_System.Repositories
{
    public interface IProjectRepository : IRepository<Project>
    {
        Task<IEnumerable<Project>> GetProjectsByOwnerAsync(string ownerId);
        Task<IEnumerable<Project>> GetProjectsByStatusAsync(ProjectStatus status);
        Task<IEnumerable<Project>> GetProjectsWithTasksAsync();
        Task<Project> GetProjectWithTasksAsync(int projectId);
        Task<IEnumerable<Project>> GetProjectsByDeadlineRangeAsync(DateTime startDate, DateTime endDate);
        Task<IEnumerable<Project>> SearchProjectsAsync(string searchTerm);
        
        // Project member management
        Task<ProjectMember> AddProjectMemberAsync(int projectId, string userId);
        Task RemoveProjectMemberAsync(int projectId, string userId);
        Task<IEnumerable<ProjectMember>> GetProjectMembersAsync(int projectId);
        Task<IEnumerable<Project>> GetProjectsByMemberAsync(string userId);
        Task<bool> IsUserProjectMemberAsync(int projectId, string userId);
        Task RemoveProjectAsync(Project project);
    }
}