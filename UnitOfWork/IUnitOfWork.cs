using Collaborative_Task_Management_System.Repositories;

namespace Collaborative_Task_Management_System.UnitOfWork
{
    public interface IUnitOfWork : IDisposable
    {
        // Existing repositories
        IProjectRepository Projects { get; }
        ITaskRepository Tasks { get; }
        ICommentRepository Comments { get; }
        INotificationRepository Notifications { get; }
        IFileAttachmentRepository FileAttachments { get; }
        IAuditLogRepository AuditLogs { get; }
        
        // Missing repositories to add
        ITagRepository Tags { get; }
        ITaskTagRepository TaskTags { get; }
        ITaskChecklistItemRepository TaskChecklistItems { get; }
        ITaskTimeEntryRepository TaskTimeEntries { get; }
        ITaskActivityLogRepository TaskActivityLogs { get; }
        ITaskDependencyRepository TaskDependencies { get; }
        
        // Existing methods
        Task<int> SaveChangesAsync();
        Task<int> SaveChangesAsync(CancellationToken cancellationToken);
        Task BeginTransactionAsync();
        Task CommitTransactionAsync();
        Task RollbackTransactionAsync();
        IRepository<T> Repository<T>() where T : class;
    }
}