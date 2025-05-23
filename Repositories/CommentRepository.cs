using Microsoft.EntityFrameworkCore;
using Collaborative_Task_Management_System.Data;
using Collaborative_Task_Management_System.Models;

namespace Collaborative_Task_Management_System.Repositories
{
    public class CommentRepository : Repository<Comment>, ICommentRepository
    {
        public CommentRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<Comment>> GetCommentsByTaskIdAsync(int taskId)
        {
            return await _dbSet
                .Where(c => c.TaskId == taskId)
                .Include(c => c.User)
                .Include(c => c.Task)
                .OrderBy(c => c.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Comment>> GetCommentsByUserIdAsync(string userId)
        {
            return await _dbSet
                .Where(c => c.UserId == userId)
                .Include(c => c.Task)
                    .ThenInclude(t => t.Project)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Comment>> GetRecentCommentsAsync(int count = 10)
        {
            return await _dbSet
                .Include(c => c.User)
                .Include(c => c.Task)
                    .ThenInclude(t => t.Project)
                .OrderByDescending(c => c.CreatedAt)
                .Take(count)
                .ToListAsync();
        }

        public async Task<IEnumerable<Comment>> GetCommentsByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            return await _dbSet
                .Where(c => c.CreatedAt >= startDate && c.CreatedAt <= endDate)
                .Include(c => c.User)
                .Include(c => c.Task)
                    .ThenInclude(t => t.Project)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();
        }
    }
}