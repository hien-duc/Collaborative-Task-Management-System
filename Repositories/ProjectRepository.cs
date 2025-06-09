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
            return await _dbSet
                .Where(p => p.CreatedById == ownerId)
                .Include(p => p.CreatedBy)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Project>> GetProjectsByStatusAsync(ProjectStatus status)
        {
            return await _dbSet
                .Where(p => p.Status == status)
                .Include(p => p.CreatedBy)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Project>> GetProjectsWithTasksAsync()
        {
            return await _dbSet
                .Include(p => p.CreatedBy)
                .Include(p => p.Tasks)
                    .ThenInclude(t => t.AssignedUser)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }

        public async Task<Project> GetProjectWithTasksAsync(int projectId)
        {
            return await _dbSet
                .Include(p => p.CreatedBy)
                .Include(p => p.Tasks)
                    .ThenInclude(t => t.AssignedUser)
                .Include(p => p.Tasks)
                    .ThenInclude(t => t.Comments)
                        .ThenInclude(c => c.User)
                .Include(p => p.Tasks)
                    .ThenInclude(t => t.FileAttachments)
                .FirstOrDefaultAsync(p => p.Id == projectId);
        }

        public async Task<IEnumerable<Project>> GetProjectsByDeadlineRangeAsync(DateTime startDate, DateTime endDate)
        {
            return await _dbSet
                .Where(p => p.Deadline >= startDate && p.Deadline <= endDate)
                .Include(p => p.CreatedBy)
                .OrderBy(p => p.Deadline)
                .ToListAsync();
        }

        public async Task<IEnumerable<Project>> SearchProjectsAsync(string searchTerm)
        {
            return await _dbSet
                .Where(p => p.Title.Contains(searchTerm) || p.Description.Contains(searchTerm))
                .Include(p => p.CreatedBy)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }

        // Project member management methods
        public async Task<ProjectMember> AddProjectMemberAsync(int projectId, string userId)
        {
            var context = _context as ApplicationDbContext;
            
            // Check if the member is already added to the project
            var existingMember = await context.ProjectMembers
                .FirstOrDefaultAsync(pm => pm.ProjectId == projectId && pm.UserId == userId);
                
            if (existingMember != null)
            {
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
                context.ProjectMembers.Remove(projectMember);
                await context.SaveChangesAsync();
            }
        }
        
        public async Task<IEnumerable<ProjectMember>> GetProjectMembersAsync(int projectId)
        {
            var context = _context as ApplicationDbContext;
            
            return await context.ProjectMembers
                .Where(pm => pm.ProjectId == projectId)
                .Include(pm => pm.User)
                .OrderBy(pm => pm.User.FullName)
                .ToListAsync();
        }
        
        public async Task<IEnumerable<Project>> GetProjectsByMemberAsync(string userId)
        {
            var context = _context as ApplicationDbContext;
            
            return await context.ProjectMembers
                .Where(pm => pm.UserId == userId)
                .Include(pm => pm.Project)
                    .ThenInclude(p => p.CreatedBy)
                .Select(pm => pm.Project)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }
        
        public async Task<bool> IsUserProjectMemberAsync(int projectId, string userId)
        {
            var context = _context as ApplicationDbContext;
            
            return await context.ProjectMembers
                .AnyAsync(pm => pm.ProjectId == projectId && pm.UserId == userId);
        }
    }
}