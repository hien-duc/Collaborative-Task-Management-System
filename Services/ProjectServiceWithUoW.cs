using Collaborative_Task_Management_System.Models;
using Collaborative_Task_Management_System.UnitOfWork;

namespace Collaborative_Task_Management_System.Services
{
    public interface IProjectServiceWithUoW
    {
        Task<List<Project>> GetAllProjectsAsync();
        Task<Project> GetProjectByIdAsync(int id);
        Task<Project> GetProjectWithTasksAsync(int id);
        Task<List<Project>> GetProjectsByOwnerAsync(string ownerId);
        Task<List<Project>> GetProjectsByStatusAsync(ProjectStatus status);
        Task<Project> CreateProjectAsync(Project project);
        Task<Project> UpdateProjectAsync(Project project);
        Task DeleteProjectAsync(int id);
        Task<bool> ProjectExistsAsync(int id);
        Task<List<Project>> SearchProjectsAsync(string searchTerm);
    }

    public class ProjectServiceWithUoW : IProjectServiceWithUoW
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<ProjectServiceWithUoW> _logger;

        public ProjectServiceWithUoW(IUnitOfWork unitOfWork, ILogger<ProjectServiceWithUoW> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<List<Project>> GetAllProjectsAsync()
        {
            try
            {
                var projects = await _unitOfWork.Projects.GetAllWithIncludesAsync(p => p.CreatedBy);
                return projects.OrderByDescending(p => p.CreatedAt).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all projects");
                throw;
            }
        }

        public async Task<Project> GetProjectByIdAsync(int id)
        {
            try
            {
                return await _unitOfWork.Projects.GetByIdWithIncludesAsync(id, 
                    p => p.CreatedBy, 
                    p => p.Tasks);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving project with ID: {ProjectId}", id);
                throw;
            }
        }

        public async Task<Project> GetProjectWithTasksAsync(int id)
        {
            try
            {
                return await _unitOfWork.Projects.GetProjectWithTasksAsync(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving project with tasks for ID: {ProjectId}", id);
                throw;
            }
        }

        public async Task<List<Project>> GetProjectsByOwnerAsync(string ownerId)
        {
            try
            {
                var projects = await _unitOfWork.Projects.GetProjectsByOwnerAsync(ownerId);
                return projects.ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving projects for owner: {OwnerId}", ownerId);
                throw;
            }
        }

        public async Task<List<Project>> GetProjectsByStatusAsync(ProjectStatus status)
        {
            try
            {
                var projects = await _unitOfWork.Projects.GetProjectsByStatusAsync(status);
                return projects.ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving projects with status: {Status}", status);
                throw;
            }
        }

        public async Task<Project> CreateProjectAsync(Project project)
        {
            try
            {
                await _unitOfWork.BeginTransactionAsync();
                
                project.CreatedAt = DateTime.UtcNow;
                await _unitOfWork.Projects.AddAsync(project);
                await _unitOfWork.SaveChangesAsync();
                
                // Log the creation
                var auditLog = new AuditLog
                {
                    UserId = project.CreatedById,
                    Action = "Project Created",
                    Details = $"Created project: {project.Title}",
                    Timestamp = DateTime.UtcNow
                };
                await _unitOfWork.AuditLogs.AddAsync(auditLog);
                
                await _unitOfWork.CommitTransactionAsync();
                
                _logger.LogInformation("Project created: {ProjectId} by user {UserId}", 
                    project.Id, project.CreatedById);
                
                return project;
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError(ex, "Error creating project");
                throw;
            }
        }

        public async Task<Project> UpdateProjectAsync(Project project)
        {
            try
            {
                await _unitOfWork.BeginTransactionAsync();
                
                var existingProject = await _unitOfWork.Projects.GetByIdAsync(project.Id);
                if (existingProject == null)
                {
                    throw new KeyNotFoundException($"Project with ID {project.Id} not found");
                }

                existingProject.Title = project.Title;
                existingProject.Description = project.Description;
                existingProject.Deadline = project.Deadline;
                existingProject.Status = project.Status;
                existingProject.UpdatedAt = DateTime.UtcNow;

                _unitOfWork.Projects.Update(existingProject);
                await _unitOfWork.SaveChangesAsync();
                
                // Log the update
                var auditLog = new AuditLog
                {
                    UserId = project.CreatedById, // You might want to pass the current user ID
                    Action = "Project Updated",
                    Details = $"Updated project: {project.Title}",
                    Timestamp = DateTime.UtcNow
                };
                await _unitOfWork.AuditLogs.AddAsync(auditLog);
                
                await _unitOfWork.CommitTransactionAsync();
                
                _logger.LogInformation("Project updated: {ProjectId}", project.Id);
                
                return existingProject;
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError(ex, "Error updating project {ProjectId}", project.Id);
                throw;
            }
        }

        public async Task DeleteProjectAsync(int id)
        {
            try
            {
                await _unitOfWork.BeginTransactionAsync();
                
                var project = await _unitOfWork.Projects.GetByIdAsync(id);
                if (project == null)
                {
                    throw new KeyNotFoundException($"Project with ID {id} not found");
                }

                _unitOfWork.Projects.Delete(project);
                await _unitOfWork.SaveChangesAsync();
                
                // Log the deletion
                var auditLog = new AuditLog
                {
                    UserId = project.CreatedById,
                    Action = "Project Deleted",
                    Details = $"Deleted project: {project.Title}",
                    Timestamp = DateTime.UtcNow
                };
                await _unitOfWork.AuditLogs.AddAsync(auditLog);
                
                await _unitOfWork.CommitTransactionAsync();
                
                _logger.LogInformation("Project deleted: {ProjectId}", id);
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError(ex, "Error deleting project {ProjectId}", id);
                throw;
            }
        }

        public async Task<bool> ProjectExistsAsync(int id)
        {
            try
            {
                return await _unitOfWork.Projects.AnyAsync(p => p.Id == id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if project exists: {ProjectId}", id);
                throw;
            }
        }

        public async Task<List<Project>> SearchProjectsAsync(string searchTerm)
        {
            try
            {
                var projects = await _unitOfWork.Projects.SearchProjectsAsync(searchTerm);
                return projects.ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching projects with term: {SearchTerm}", searchTerm);
                throw;
            }
        }
    }
}