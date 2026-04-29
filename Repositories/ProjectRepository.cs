using Microsoft.EntityFrameworkCore;
using Collaborative_Task_Management_System.Data;
using Collaborative_Task_Management_System.Models;

namespace Collaborative_Task_Management_System.Repositories
{
    public class ProjectRepository : Repository<Project>, IProjectRepository
    {
        public ProjectRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<Project>> GetProjectsByOwnerAsync(string ownerId)
        {
            var context = _context as ApplicationDbContext;
            return await _context.Projects
                .Where(p => p.CreatedById == ownerId && !p.IsDeleted)
                .Include(p => p.CreatedBy)
                .Include(p => p.ProjectMembers)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Project>> GetProjectsByStatusAsync(ProjectStatus status)
        {
            return await _dbSet
                .Where(p => p.Status == status && !p.IsDeleted)
                .Include(p => p.CreatedBy)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Project>> GetProjectsWithTasksAsync()
        {
            return await _dbSet
                .Where(p => !p.IsDeleted)
                .Include(p => p.CreatedBy)
                .Include(p => p.Tasks.Where(t => !t.IsDeleted))
                    .ThenInclude(t => t.AssignedUser)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }

        public async Task<Project> GetProjectWithTasksAsync(int projectId)
        {
            return await _dbSet
                .Where(p => p.Id == projectId && !p.IsDeleted)
                .Include(p => p.CreatedBy)
                .Include(p => p.Tasks.Where(t => !t.IsDeleted))
                    .ThenInclude(t => t.AssignedUser)
                .Include(p => p.Tasks.Where(t => !t.IsDeleted))
                    .ThenInclude(t => t.Comments)
                        .ThenInclude(c => c.User)
                .Include(p => p.Tasks.Where(t => !t.IsDeleted))
                    .ThenInclude(t => t.FileAttachments)
                .FirstOrDefaultAsync();
        }

        public async Task<IEnumerable<Project>> GetProjectsByDeadlineRangeAsync(DateTime startDate, DateTime endDate)
        {
            return await _dbSet
                .Where(p => p.Deadline >= startDate && p.Deadline <= endDate && !p.IsDeleted)
                .Include(p => p.CreatedBy)
                .OrderBy(p => p.Deadline)
                .ToListAsync();
        }

        public async Task<IEnumerable<Project>> SearchProjectsAsync(string searchTerm)
        {
            return await _dbSet
                .Where(p => (p.Title.Contains(searchTerm) || p.Description.Contains(searchTerm)) && !p.IsDeleted)
                .Include(p => p.CreatedBy)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }

        public async Task<ProjectMember> AddProjectMemberAsync(int projectId, string userId)
        {
            var context = _context as ApplicationDbContext;
            
            // Check if the member is already added to the project
            var existingMember = await context.ProjectMembers
                .FirstOrDefaultAsync(pm => pm.ProjectId == projectId && pm.UserId == userId);
                
            if (existingMember != null)
            {
                existingMember.IsActive = true;
                context.ProjectMembers.Update(existingMember);
                await context.SaveChangesAsync();
                return existingMember; // Member already exists
            }
            
            var projectMember = new ProjectMember
            {
                ProjectId = projectId,
                UserId = userId,
                JoinedAt = DateTime.UtcNow
            };
            
            await context.ProjectMembers.AddAsync(projectMember);
            await context.SaveChangesAsync();
            
            return projectMember;
        }

        public async Task RemoveProjectMemberAsync(int projectId, string userId)
        {
            var context = _context as ApplicationDbContext;
            
            var projectMember = await context.ProjectMembers
                .FirstOrDefaultAsync(pm => pm.ProjectId == projectId && pm.UserId == userId);
                
            if (projectMember != null)
            {
                projectMember.IsActive = false;
                context.ProjectMembers.Update(projectMember);
                await context.SaveChangesAsync();
            }
        }

        // Project member management methods
        public async Task<IEnumerable<ProjectMember>> GetProjectMembersAsync(int projectId)
        {
            var context = _context as ApplicationDbContext;
            
            return await context.ProjectMembers
                .Where(pm => pm.ProjectId == projectId && pm.IsActive  && !pm.Project.IsDeleted)
                .Include(pm => pm.User)
                .OrderBy(pm => pm.User.FullName)
                .ToListAsync();
        }

        public async Task<IEnumerable<Project>> GetProjectsByMemberAsync(string userId)
        {
            var context = _context as ApplicationDbContext;
    
            return await context.ProjectMembers
                .Where(pm => pm.UserId == userId && pm.IsActive && !pm.Project.IsDeleted)
                .Include(pm => pm.Project)
                .ThenInclude(p => p.CreatedBy)
                .Include(pm => pm.Project)
                .ThenInclude(p => p.ProjectMembers)
                .ThenInclude(pm => pm.User)  // Include the User for each ProjectMember
                .Select(pm => pm.Project)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }

        public async Task<bool> IsUserProjectMemberAsync(int projectId, string userId)
        {
            var context = _context as ApplicationDbContext;
            
            return await context.ProjectMembers
                .AnyAsync(pm => pm.ProjectId == projectId && pm.UserId == userId && pm.IsActive);
        }

        public async Task RemoveProjectAsync(Project project)
        {
            var context = _context as ApplicationDbContext;
            project.IsDeleted = true;
            context.Projects.Update(project);
            await context.SaveChangesAsync();
        }
    }
}