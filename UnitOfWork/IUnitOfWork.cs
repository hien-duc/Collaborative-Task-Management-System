using Collaborative_Task_Management_System.Repositories;

namespace Collaborative_Task_Management_System.UnitOfWork
{
    public interface IUnitOfWork : IDisposable
    {
        // Repository properties
        IProjectRepository Projects { get; }
        ITaskRepository Tasks { get; }
        ICommentRepository Comments { get; }
        INotificationRepository Notifications { get; }
        IFileAttachmentRepository FileAttachments { get; }
        IAuditLogRepository AuditLogs { get; }

        // Unit of Work methods
        Task<int> SaveChangesAsync();
        Task<int> SaveChangesAsync(CancellationToken cancellationToken);
        
        // Transaction support
        Task BeginTransactionAsync();
        Task CommitTransactionAsync();
        Task RollbackTransactionAsync();
        
        // Generic repository access
        IRepository<T> Repository<T>() where T : class;
    }
}