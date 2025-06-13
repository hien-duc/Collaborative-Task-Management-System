using System.Linq.Expressions;
using System.Runtime.InteropServices;
using Collaborative_Task_Management_System.Models;
using Collaborative_Task_Management_System.UnitOfWork;

namespace Collaborative_Task_Management_System.Services
{

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
                var projects = await _unitOfWork.Projects.GetAllWithIncludesAsync(p => p.CreatedBy,
                    p => p.ProjectMembers);
                return projects.OrderByDescending(p => p.CreatedAt).Where(p => p.IsDeleted == false).ToList();
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
                    p => p.TeamMembers,
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

        public async Task<Project> CreateProjectAsync(Project project, string? ipAddress)
        {
            try
            {
                await _unitOfWork.BeginTransactionAsync();
        
                // Check for duplicate project title
                var existingProject = await _unitOfWork.Projects
                    .FindAsync(p => p.Title.ToLower() == project.Title.ToLower());
            
                if (existingProject.Count() != 0)
                {
                    throw new InvalidOperationException($"A project with the title '{project.Title}' already exists.");
                }

                // Add the project
                await _unitOfWork.Projects.AddAsync(project);
                await _unitOfWork.SaveChangesAsync();
        
                // Log the creation
                if (ipAddress == null)
                {
                    ipAddress = "Unknown";
                }
                var auditLog = new AuditLog
                {
                    UserId = project.CreatedById,
                    Action = "Project Created",
                    Details = $"Created project: {project.Title}",
                    IpAddress = ipAddress,
                    Timestamp = DateTime.UtcNow,
                    EntityType = nameof(Project),
                    EntityId = project.Id.ToString()
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
                _logger.LogError(ex, "Error creating project: {Message}", ex.Message);
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

                await _unitOfWork.Projects.RemoveProjectAsync(project);
                
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

        // Project member management methods
        public async Task<ProjectMember> AddProjectMemberAsync(int projectId, string userId, string? ipAddress)
        {
            try
            {
                await _unitOfWork.BeginTransactionAsync();

                // Check if project exists
                var project = await _unitOfWork.Projects.GetByIdWithIncludesAsync(projectId,
                    p => p.ProjectMembers);
                if (project == null)
                {
                    throw new ArgumentException($"Project with ID {projectId} not found");
                }

                // Add the member to the project
                var projectMember = await _unitOfWork.Projects.AddProjectMemberAsync(projectId, userId);

                // Log the action
                await _unitOfWork.AuditLogs.AddAsync(new AuditLog
                {
                    UserId = userId,
                    Action = "Added to project",
                    EntityId = projectId.ToString(),
                    Details = "Add Project Member",
                    EntityType = "Project",
                    Timestamp = DateTime.UtcNow,
                    IpAddress = ipAddress
                });

                await _unitOfWork.CommitTransactionAsync();
                return projectMember;
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError(ex, "Error adding user {UserId} to project {ProjectId}", userId, projectId);
                throw;
            }
        }

        public async Task RemoveProjectMemberAsync(int projectId, string userId, string? ipAddress)
        {
            try
            {
                await _unitOfWork.BeginTransactionAsync();

                // Check if project exists
                var project = await _unitOfWork.Projects.GetByIdAsync(projectId);
                if (project == null)
                {
                    throw new ArgumentException($"Project with ID {projectId} not found");
                }

                // Remove the member from the project
                await _unitOfWork.Projects.RemoveProjectMemberAsync(projectId, userId);

                // Log the action
                await _unitOfWork.AuditLogs.AddAsync(new AuditLog
                {
                    UserId = userId,
                    Action = "Removed from project",
                    EntityId = projectId.ToString(),
                    EntityType = "Project",
                    Timestamp = DateTime.UtcNow,
                    IpAddress = ipAddress,
                    Details = $"User {userId} was removed from project {projectId}"
                });

                await _unitOfWork.CommitTransactionAsync();
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError(ex, "Error removing user {UserId} from project {ProjectId}", userId, projectId);
                throw;
            }
        }

        public async Task<List<ProjectMember>> GetProjectMembersAsync(int projectId)
        {
            try
            {
                var members = await _unitOfWork.Projects.GetProjectMembersAsync(projectId);
                return members.ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving members for project {ProjectId}", projectId);
                throw;
            }
        }

        public async Task<List<Project>> GetProjectsByMemberAsync(string userId)
        {
            try
            {
                var projects = await _unitOfWork.Projects.GetProjectsByMemberAsync(userId);
                return projects.ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving projects for member {UserId}", userId);
                throw;
            }
        }
        
        public async Task<List<Project>> GetProjectsForUserAsync(string userId)
        {
            try
            {
                // Get projects where user is a member
                var memberProjects = await _unitOfWork.Projects.GetProjectsByMemberAsync(userId);
                
                // Get projects created by the user
                var ownedProjects = await _unitOfWork.Projects.GetProjectsByOwnerAsync(userId);
                
                // Combine both lists and remove duplicates
                var allProjects = memberProjects.Concat(ownedProjects)
                    .GroupBy(p => p.Id)
                    .Select(g => g.First())
                    .OrderByDescending(p => p.CreatedAt)
                    .ToList();
                    
                return allProjects;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving projects for user {UserId}", userId);
                throw;
            }
        }

        public async Task<bool> IsUserProjectMemberAsync(int projectId, string userId)
        {
            try
            {
                return await _unitOfWork.Projects.IsUserProjectMemberAsync(projectId, userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if user {UserId} is a member of project {ProjectId}", userId, projectId);
                throw;
            }
        }
    }
}