using Collaborative_Task_Management_System.Models;

namespace Collaborative_Task_Management_System.Services;

public interface IProjectServiceWithUoW
{
    Task<List<Project>> GetAllProjectsAsync();
    Task<Project> GetProjectByIdAsync(int id);
    Task<Project> GetProjectWithTasksAsync(int id);
    Task<List<Project>> GetProjectsByOwnerAsync(string ownerId);
    Task<List<Project>> GetProjectsByStatusAsync(ProjectStatus status);
    Task<Project> CreateProjectAsync(Project project, string? ipAddress);
    Task<Project> UpdateProjectAsync(Project project);
    Task DeleteProjectAsync(int id);
    Task<bool> ProjectExistsAsync(int id);
    Task<List<Project>> SearchProjectsAsync(string searchTerm);
}