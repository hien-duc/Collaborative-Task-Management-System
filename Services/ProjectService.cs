using Microsoft.EntityFrameworkCore;
using Collaborative_Task_Management_System.Data;
using Collaborative_Task_Management_System.Models;

namespace Collaborative_Task_Management_System.Services
{
    public interface IProjectService
    {
        Task<List<Project>> GetAllProjectsAsync();
        Task<Project> GetProjectByIdAsync(int id);
        Task<Project> CreateProjectAsync(Project project);
        Task<Project> UpdateProjectAsync(Project project);
        Task DeleteProjectAsync(int id);
        Task<bool> ProjectExistsAsync(int id);
    }

    public class ProjectService : IProjectService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ProjectService> _logger;

        public ProjectService(ApplicationDbContext context, ILogger<ProjectService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<List<Project>> GetAllProjectsAsync()
        {
            return await _context.Projects
                .Include(p => p.Owner)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }

        public async Task<Project> GetProjectByIdAsync(int id)
        {
            return await _context.Projects
                .Include(p => p.Owner)
                .Include(p => p.Tasks)
                    .ThenInclude(t => t.AssignedTo)
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task<Project> CreateProjectAsync(Project project)
        {
            _context.Add(project);
            await _context.SaveChangesAsync();
            return project;
        }

        public async Task<Project> UpdateProjectAsync(Project project)
        {
            var existingProject = await _context.Projects.FindAsync(project.Id);
            if (existingProject == null)
            {
                throw new KeyNotFoundException($"Project with ID {project.Id} not found");
            }

            existingProject.Title = project.Title;
            existingProject.Description = project.Description;
            existingProject.Deadline = project.Deadline;
            existingProject.Status = project.Status;
            existingProject.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return existingProject;
        }

        public async Task DeleteProjectAsync(int id)
        {
            var project = await _context.Projects.FindAsync(id);
            if (project == null)
            {
                throw new KeyNotFoundException($"Project with ID {id} not found");
            }

            _context.Projects.Remove(project);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> ProjectExistsAsync(int id)
        {
            return await _context.Projects.AnyAsync(p => p.Id == id);
        }
    }
}