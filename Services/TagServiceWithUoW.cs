using Collaborative_Task_Management_System.Models;
using Collaborative_Task_Management_System.UnitOfWork;

namespace Collaborative_Task_Management_System.Services
{
    public class TagServiceWithUoW : ITagServiceWithUoW
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<TagServiceWithUoW> _logger;

        public TagServiceWithUoW(IUnitOfWork unitOfWork, ILogger<TagServiceWithUoW> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<List<Tag>> GetAllTagsAsync()
        {
            try
            {
                var tags = await _unitOfWork.Tags.GetAllAsync();
                return tags.ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all tags");
                throw;
            }
        }

        public async Task<Tag> GetTagByIdAsync(int id)
        {
            try
            {
                return await _unitOfWork.Tags.GetByIdAsync(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving tag with ID: {TagId}", id);
                throw;
            }
        }

        public async Task<Tag> GetTagByNameAsync(string name)
        {
            try
            {
                return await _unitOfWork.Tags.GetTagByNameAsync(name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving tag with name: {TagName}", name);
                throw;
            }
        }

        public async Task<List<Tag>> GetTagsByTaskIdAsync(int taskId)
        {
            try
            {
                var tags = await _unitOfWork.Tags.GetTagsByTaskIdAsync(taskId);
                return tags.ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving tags for task: {TaskId}", taskId);
                throw;
            }
        }

        public async Task<List<Tag>> SearchTagsAsync(string searchTerm)
        {
            try
            {
                var tags = await _unitOfWork.Tags.GetTagsByNameAsync(searchTerm);
                return tags.ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching tags with term: {SearchTerm}", searchTerm);
                throw;
            }
        }

        public async Task<Tag> CreateTagAsync(Tag tag)
        {
            try
            {
                var existingTag = await _unitOfWork.Tags.GetTagByNameAsync(tag.Name);
                if (existingTag != null)
                {
                    return existingTag; // Return existing tag if name already exists
                }

                var newTag = await _unitOfWork.Tags.AddAsync(tag);
                await _unitOfWork.SaveChangesAsync();
                return newTag;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating tag: {TagName}", tag.Name);
                throw;
            }
        }

        public async Task<Tag> UpdateTagAsync(Tag tag)
        {
            try
            {
                _unitOfWork.Tags.Update(tag);
                await _unitOfWork.SaveChangesAsync();
                return tag;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating tag with ID: {TagId}", tag.Id);
                throw;
            }
        }

        public async Task DeleteTagAsync(int id)
        {
            try
            {
                await _unitOfWork.Tags.DeleteByIdAsync(id);
                await _unitOfWork.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting tag with ID: {TagId}", id);
                throw;
            }
        }

        public async Task<bool> TagExistsAsync(int id)
        {
            try
            {
                return await _unitOfWork.Tags.AnyAsync(t => t.Id == id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if tag exists with ID: {TagId}", id);
                throw;
            }
        }

        public async Task<bool> TagExistsByNameAsync(string name)
        {
            try
            {
                return await _unitOfWork.Tags.AnyAsync(t => t.Name == name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if tag exists with name: {TagName}", name);
                throw;
            }
        }

        public async Task AddTagToTaskAsync(int taskId, int tagId)
        {
            try
            {
                // First check if the task and tag exist
                var task = await _unitOfWork.Tasks.GetByIdAsync(taskId);
                var tag = await _unitOfWork.Tags.GetByIdAsync(tagId);

                if (task == null || tag == null)
                {
                    throw new ArgumentException("Task or Tag not found");
                }

                // Check if the relationship already exists
                var exists = await _unitOfWork.Repository<TaskTag>().AnyAsync(
                    tt => tt.TaskId == taskId && tt.TagId == tagId);

                if (!exists)
                {
                    // Create the relationship
                    var taskTag = new TaskTag
                    {
                        TaskId = taskId,
                        TagId = tagId
                    };

                    await _unitOfWork.Repository<TaskTag>().AddAsync(taskTag);
                    await _unitOfWork.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding tag {TagId} to task {TaskId}", tagId, taskId);
                throw;
            }
        }

        public async Task RemoveTagFromTaskAsync(int taskId, int tagId)
        {
            try
            {
                // Find the relationship
                var taskTag = await _unitOfWork.Repository<TaskTag>().FirstOrDefaultAsync(
                    tt => tt.TaskId == taskId && tt.TagId == tagId);

                if (taskTag != null)
                {
                    _unitOfWork.Repository<TaskTag>().Delete(taskTag);
                    await _unitOfWork.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing tag {TagId} from task {TaskId}", tagId, taskId);
                throw;
            }
        }
    }
}