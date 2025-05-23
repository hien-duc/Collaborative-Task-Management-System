using Microsoft.EntityFrameworkCore;
using Collaborative_Task_Management_System.Data;
using Collaborative_Task_Management_System.Models;

namespace Collaborative_Task_Management_System.Repositories
{
    public class FileAttachmentRepository : Repository<FileAttachment>, IFileAttachmentRepository
    {
        public FileAttachmentRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<FileAttachment>> GetAttachmentsByTaskIdAsync(int taskId)
        {
            return await _dbSet
                .Where(f => f.TaskId == taskId)
                .Include(f => f.UploadedBy)
                .Include(f => f.Task)
                .OrderByDescending(f => f.UploadedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<FileAttachment>> GetAttachmentsByUserIdAsync(string userId)
        {
            return await _dbSet
                .Where(f => f.UploadedById == userId)
                .Include(f => f.Task)
                    .ThenInclude(t => t.Project)
                .OrderByDescending(f => f.UploadedAt)
                .ToListAsync();
        }

        public async Task<FileAttachment> GetAttachmentByFileNameAsync(string fileName)
        {
            return await _dbSet
                .Include(f => f.UploadedBy)
                .Include(f => f.Task)
                    .ThenInclude(t => t.Project)
                .FirstOrDefaultAsync(f => f.FileName == fileName);
        }

        public async Task<long> GetTotalFileSizeByUserAsync(string userId)
        {
            // Note: This assumes you have a FileSize property in FileAttachment model
            // If not, you might need to calculate file sizes differently
            var attachments = await _dbSet
                .Where(f => f.UploadedById == userId)
                .ToListAsync();

            // For now, returning count as a placeholder
            // You should implement actual file size calculation
            return attachments.Count;
        }

        public async Task<IEnumerable<FileAttachment>> GetRecentAttachmentsAsync(int count = 10)
        {
            return await _dbSet
                .Include(f => f.UploadedBy)
                .Include(f => f.Task)
                    .ThenInclude(t => t.Project)
                .OrderByDescending(f => f.UploadedAt)
                .Take(count)
                .ToListAsync();
        }
    }
}