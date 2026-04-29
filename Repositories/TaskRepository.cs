using Microsoft.EntityFrameworkCore;
using Collaborative_Task_Management_System.Data;
using Collaborative_Task_Management_System.Models;
using TaskStatus = Collaborative_Task_Management_System.Models.TaskStatus;

namespace Collaborative_Task_Management_System.Repositories
{
    public class TaskRepository : Repository<TaskItem>, ITaskRepository
    {
        public TaskRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<TaskItem>> GetTasksByProjectIdAsync(int projectId)
        {
            return await _dbSet
                .Where(t => t.ProjectId == projectId && !t.IsDeleted)
                .Include(t => t.AssignedUser)
                .Include(t => t.CreatedBy)
                .Include(t => t.Project)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<TaskItem>> GetTasksByAssignedUserAsync(string userId)
        {
            return await _dbSet
                .Where(t => t.AssignedUserId == userId && !t.IsDeleted && !t.Project.IsDeleted)
                .Include(t => t.Project)
                .Include(t => t.CreatedBy)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<TaskItem>> GetTasksByCreatorAsync(string userId)
        {
            return await _dbSet
                .Where(t => t.CreatedById == userId && !t.IsDeleted && !t.Project.IsDeleted)
                .Include(t => t.AssignedUser)
                .Include(t => t.Project)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<TaskItem>> GetTasksByStatusAsync(TaskStatus status)
        {
            return await _dbSet
                .Where(t => t.Status == status && !t.IsDeleted)
                .Include(t => t.AssignedUser)
                .Include(t => t.CreatedBy)
                .Include(t => t.Project)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<TaskItem>> GetTasksByPriorityAsync(string priority)
        {
            return await _dbSet
                .Where(t => t.Priority == priority && !t.IsDeleted)
                .Include(t => t.AssignedUser)
                .Include(t => t.CreatedBy)
                .Include(t => t.Project)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<TaskItem>> GetOverdueTasksAsync()
        {
            var today = DateTime.UtcNow.Date;
            return await _dbSet
                .Where(t => t.DueDate < today && t.Status != TaskStatus.Completed && !t.IsDeleted)
                .Include(t => t.AssignedUser)
                .Include(t => t.CreatedBy)
                .Include(t => t.Project)
                .OrderBy(t => t.DueDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<TaskItem>> GetTasksDueSoonAsync(int days = 7)
        {
            var today = DateTime.UtcNow.Date;
            var dueDate = today.AddDays(days);
            return await _dbSet
                .Where(t => t.DueDate >= today && t.DueDate <= dueDate && t.Status != TaskStatus.Completed && !t.IsDeleted)
                .Include(t => t.AssignedUser)
                .Include(t => t.CreatedBy)
                .Include(t => t.Project)
                .OrderBy(t => t.DueDate)
                .ToListAsync();
        }

        public async Task<TaskItem> GetTaskWithCommentsAsync(int taskId)
        {
            return await _dbSet
                .Where(t => t.Id == taskId && !t.IsDeleted)
                .Include(t => t.Comments)
                    .ThenInclude(c => c.User)
                .Include(t => t.AssignedUser)
                .Include(t => t.CreatedBy)
                .Include(t => t.Project)
                .FirstOrDefaultAsync();
        }

        public async Task<TaskItem> GetTaskWithAttachmentsAsync(int taskId)
        {
            return await _dbSet
                .Where(t => t.Id == taskId && !t.IsDeleted)
                .Include(t => t.FileAttachments)
                    .ThenInclude(f => f.UploadedBy)
                .Include(t => t.AssignedUser)
                .Include(t => t.CreatedBy)
                .Include(t => t.Project)
                .FirstOrDefaultAsync();
        }

        public async Task<TaskItem> GetTaskWithAllDetailsAsync(int taskId)
        {
            return await _dbSet
                .Where(t => t.Id == taskId && !t.IsDeleted)
                .Include(t => t.Comments)
                    .ThenInclude(c => c.User)
                .Include(t => t.FileAttachments)
                    .ThenInclude(f => f.UploadedBy)
                .Include(t => t.AssignedUser)
                .Include(t => t.CreatedBy)
                .Include(t => t.Project)
                .FirstOrDefaultAsync();
        }

        public async Task<IEnumerable<TaskItem>> SearchTasksAsync(string searchTerm)
        {
            return await _dbSet
                .Where(t => (t.Title.Contains(searchTerm) || t.Description.Contains(searchTerm)) && !t.IsDeleted)
                .Include(t => t.AssignedUser)
                .Include(t => t.CreatedBy)
                .Include(t => t.Project)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<TaskItem>> GetTasksByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            return await _dbSet
                .Where(t => t.DueDate >= startDate && t.DueDate <= endDate && !t.IsDeleted)
                .Include(t => t.AssignedUser)
                .Include(t => t.CreatedBy)
                .Include(t => t.Project)
                .OrderBy(t => t.DueDate)
                .ToListAsync();
        }
    }
}