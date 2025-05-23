using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Collaborative_Task_Management_System.Data;
using Collaborative_Task_Management_System.Repositories;

namespace Collaborative_Task_Management_System.UnitOfWork
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly ApplicationDbContext _context;
        private IDbContextTransaction _transaction;
        private readonly Dictionary<Type, object> _repositories;
        private bool _disposed = false;

        // Repository instances
        private IProjectRepository _projects;
        private ITaskRepository _tasks;
        private ICommentRepository _comments;
        private INotificationRepository _notifications;
        private IFileAttachmentRepository _fileAttachments;
        private IAuditLogRepository _auditLogs;

        public UnitOfWork(ApplicationDbContext context)
        {
            _context = context;
            _repositories = new Dictionary<Type, object>();
        }

        // Repository properties with lazy initialization
        public IProjectRepository Projects
        {
            get
            {
                if (_projects == null)
                    _projects = new ProjectRepository(_context);
                return _projects;
            }
        }

        public ITaskRepository Tasks
        {
            get
            {
                if (_tasks == null)
                    _tasks = new TaskRepository(_context);
                return _tasks;
            }
        }

        public ICommentRepository Comments
        {
            get
            {
                if (_comments == null)
                    _comments = new CommentRepository(_context);
                return _comments;
            }
        }

        public INotificationRepository Notifications
        {
            get
            {
                if (_notifications == null)
                    _notifications = new NotificationRepository(_context);
                return _notifications;
            }
        }

        public IFileAttachmentRepository FileAttachments
        {
            get
            {
                if (_fileAttachments == null)
                    _fileAttachments = new FileAttachmentRepository(_context);
                return _fileAttachments;
            }
        }

        public IAuditLogRepository AuditLogs
        {
            get
            {
                if (_auditLogs == null)
                    _auditLogs = new AuditLogRepository(_context);
                return _auditLogs;
            }
        }

        // Generic repository access
        public IRepository<T> Repository<T>() where T : class
        {
            var type = typeof(T);
            if (!_repositories.ContainsKey(type))
            {
                _repositories[type] = new Repository<T>(_context);
            }
            return (IRepository<T>)_repositories[type];
        }

        // Save changes
        public async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync();
        }

        public async Task<int> SaveChangesAsync(CancellationToken cancellationToken)
        {
            return await _context.SaveChangesAsync(cancellationToken);
        }

        // Transaction support
        public async Task BeginTransactionAsync()
        {
            _transaction = await _context.Database.BeginTransactionAsync();
        }

        public async Task CommitTransactionAsync()
        {
            try
            {
                await _context.SaveChangesAsync();
                if (_transaction != null)
                {
                    await _transaction.CommitAsync();
                }
            }
            catch
            {
                await RollbackTransactionAsync();
                throw;
            }
            finally
            {
                if (_transaction != null)
                {
                    await _transaction.DisposeAsync();
                    _transaction = null;
                }
            }
        }

        public async Task RollbackTransactionAsync()
        {
            if (_transaction != null)
            {
                await _transaction.RollbackAsync();
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }

        // Dispose pattern
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _transaction?.Dispose();
                    _context?.Dispose();
                }
                _disposed = true;
            }
        }
    }
}