using Collaborative_Task_Management_System.Models;

namespace Collaborative_Task_Management_System.Repositories
{
    public interface IFileAttachmentRepository : IRepository<FileAttachment>
    {
        Task<IEnumerable<FileAttachment>> GetAttachmentsByTaskIdAsync(int taskId);
        Task<IEnumerable<FileAttachment>> GetAttachmentsByUserIdAsync(string userId);
        Task<FileAttachment> GetAttachmentByFileNameAsync(string fileName);
        Task<long> GetTotalFileSizeByUserAsync(string userId);
        Task<IEnumerable<FileAttachment>> GetRecentAttachmentsAsync(int count = 10);
    }
}