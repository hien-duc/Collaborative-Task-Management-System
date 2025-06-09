using Collaborative_Task_Management_System.Models;

namespace Collaborative_Task_Management_System.Services
{
    public interface ITagServiceWithUoW
    {
        Task<List<Tag>> GetAllTagsAsync();
        Task<Tag> GetTagByIdAsync(int id);
        Task<Tag> GetTagByNameAsync(string name);
        Task<List<Tag>> GetTagsByTaskIdAsync(int taskId);
        Task<List<Tag>> SearchTagsAsync(string searchTerm);
        Task<Tag> CreateTagAsync(Tag tag);
        Task<Tag> UpdateTagAsync(Tag tag);
        Task DeleteTagAsync(int id);
        Task<bool> TagExistsAsync(int id);
        Task<bool> TagExistsByNameAsync(string name);
        Task AddTagToTaskAsync(int taskId, int tagId);
        Task RemoveTagFromTaskAsync(int taskId, int tagId);
    }
}