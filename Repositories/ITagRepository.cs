using Collaborative_Task_Management_System.Models;

namespace Collaborative_Task_Management_System.Repositories
{
    public interface ITagRepository : IRepository<Tag>
    {
        Task<IEnumerable<Tag>> GetTagsByTaskIdAsync(int taskId);
        Task<IEnumerable<Tag>> GetTagsByNameAsync(string name);
        Task<Tag> GetTagByNameAsync(string name);
    }
}