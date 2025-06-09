using Microsoft.EntityFrameworkCore;
using Collaborative_Task_Management_System.Data;
using Collaborative_Task_Management_System.Models;

namespace Collaborative_Task_Management_System.Repositories
{
    public class TagRepository : Repository<Tag>, ITagRepository
    {
        public TagRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<Tag>> GetTagsByTaskIdAsync(int taskId)
        {
            return await _context.TaskTags
                .Where(tt => tt.TaskId == taskId)
                .Select(tt => tt.Tag)
                .ToListAsync();
        }

        public async Task<IEnumerable<Tag>> GetTagsByNameAsync(string name)
        {
            return await _dbSet
                .Where(t => t.Name.Contains(name))
                .ToListAsync();
        }

        public async Task<Tag> GetTagByNameAsync(string name)
        {
            return await _dbSet
                .FirstOrDefaultAsync(t => t.Name == name);
        }
    }
}