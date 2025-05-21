using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Collaborative_Task_Management_System.Data;
using Collaborative_Task_Management_System.Models;

namespace Collaborative_Task_Management_System.Services
{
    public interface ITaskService
    {
        Task<List<TaskItem>> GetTasksByProjectIdAsync(int projectId);
        Task<List<TaskItem>> GetTasksByUserIdAsync(string userId);
        Task<TaskItem> GetTaskByIdAsync(int id);
        Task<TaskItem> CreateTaskAsync(TaskItem task);
        Task<TaskItem> UpdateTaskAsync(TaskItem task);
        Task<TaskItem> UpdateTaskStatusAsync(int taskId, TaskStatus status);
        Task DeleteTaskAsync(int id);
        Task<bool> TaskExistsAsync(int id);
        Task<FileAttachment> SaveFileAttachmentAsync(int taskId, IFormFile file, string uploadedById);
        Task<FileAttachment> GetFileAttachmentAsync(int fileId);
    }

    public class TaskService : ITaskService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<TaskService> _logger;
        private readonly string _uploadsPath;

        public TaskService(
            ApplicationDbContext context,
            ILogger<TaskService> logger,
            IWebHostEnvironment environment)
        {
            _context = context;
            _logger = logger;
            _uploadsPath = Path.Combine(environment.ContentRootPath, "Uploads");
            
            // Ensure uploads directory exists
            if (!Directory.Exists(_uploadsPath))
            {
                Directory.CreateDirectory(_uploadsPath);
            }
        }

        public async Task<List<TaskItem>> GetTasksByProjectIdAsync(int projectId)
        {
            return await _context.Tasks
                .Include(t => t.AssignedTo)
                .Include(t => t.CreatedBy)
                .Include(t => t.FileAttachments)
                .Where(t => t.ProjectId == projectId)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<TaskItem>> GetTasksByUserIdAsync(string userId)
        {
            return await _context.Tasks
                .Include(t => t.Project)
                .Include(t => t.AssignedTo)
                .Include(t => t.CreatedBy)
                .Where(t => t.AssignedToId == userId || t.CreatedById == userId)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();
        }

        public async Task<TaskItem> GetTaskByIdAsync(int id)
        {
            return await _context.Tasks
                .Include(t => t.Project)
                .Include(t => t.AssignedTo)
                .Include(t => t.CreatedBy)
                .Include(t => t.FileAttachments)
                .FirstOrDefaultAsync(t => t.Id == id);
        }

        public async Task<TaskItem> CreateTaskAsync(TaskItem task)
        {
            task.CreatedAt = DateTime.UtcNow;
            _context.Tasks.Add(task);
            await _context.SaveChangesAsync();
            return task;
        }

        public async Task<TaskItem> UpdateTaskAsync(TaskItem task)
        {
            var existingTask = await _context.Tasks.FindAsync(task.Id);
            if (existingTask == null)
            {
                throw new KeyNotFoundException($"Task with ID {task.Id} not found");
            }

            existingTask.Title = task.Title;
            existingTask.Description = task.Description;
            existingTask.DueDate = task.DueDate;
            existingTask.Priority = task.Priority;
            existingTask.Status = task.Status;
            existingTask.AssignedToId = task.AssignedToId;
            existingTask.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return existingTask;
        }

        public async Task<TaskItem> UpdateTaskStatusAsync(int taskId, TaskStatus status)
        {
            var task = await _context.Tasks.FindAsync(taskId);
            if (task == null)
            {
                throw new KeyNotFoundException($"Task with ID {taskId} not found");
            }

            task.Status = status;
            task.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return task;
        }

        public async Task DeleteTaskAsync(int id)
        {
            var task = await _context.Tasks
                .Include(t => t.FileAttachments)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (task == null)
            {
                throw new KeyNotFoundException($"Task with ID {id} not found");
            }

            // Delete associated files from disk
            foreach (var file in task.FileAttachments)
            {
                var filePath = Path.Combine(_uploadsPath, file.FileName);
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
            }

            _context.Tasks.Remove(task);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> TaskExistsAsync(int id)
        {
            return await _context.Tasks.AnyAsync(t => t.Id == id);
        }

        public async Task<FileAttachment> SaveFileAttachmentAsync(int taskId, IFormFile file, string uploadedById)
        {
            if (file.Length > 5 * 1024 * 1024) // 5MB limit
            {
                throw new InvalidOperationException("File size exceeds 5MB limit");
            }

            var allowedTypes = new[] { ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".jpg", ".jpeg", ".png" };
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!allowedTypes.Contains(extension))
            {
                throw new InvalidOperationException("File type not allowed");
            }

            var uniqueFileName = $"{Guid.NewGuid()}{extension}";
            var filePath = Path.Combine(_uploadsPath, uniqueFileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var attachment = new FileAttachment
            {
                TaskId = taskId,
                FileName = uniqueFileName,
                FilePath = filePath,
                ContentType = file.ContentType,
                FileSize = file.Length,
                UploadedById = uploadedById,
                UploadedAt = DateTime.UtcNow
            };

            _context.FileAttachments.Add(attachment);
            await _context.SaveChangesAsync();

            return attachment;
        }

        public async Task<FileAttachment> GetFileAttachmentAsync(int fileId)
        {
            return await _context.FileAttachments.FindAsync(fileId);
        }
    }
}