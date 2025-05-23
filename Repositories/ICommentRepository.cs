using Collaborative_Task_Management_System.Models;

namespace Collaborative_Task_Management_System.Repositories
{
    public interface ICommentRepository : IRepository<Comment>
    {
        Task<IEnumerable<Comment>> GetCommentsByTaskIdAsync(int taskId);
        Task<IEnumerable<Comment>> GetCommentsByUserIdAsync(string userId);
        Task<IEnumerable<Comment>> GetRecentCommentsAsync(int count = 10);
        Task<IEnumerable<Comment>> GetCommentsByDateRangeAsync(DateTime startDate, DateTime endDate);
    }
}