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
    }
}