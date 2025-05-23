# Repository Pattern and Unit of Work Implementation

This document describes the implementation of the Repository Pattern and Unit of Work pattern in the Collaborative Task Management System.

## Overview

The Repository Pattern and Unit of Work pattern have been implemented to provide a clean separation between the data access layer and business logic, improve testability, and ensure consistent data operations across the application.

## Architecture

### Repository Pattern

The Repository Pattern encapsulates the logic needed to access data sources. It centralizes common data access functionality, providing better maintainability and decoupling the infrastructure or technology used to access databases from the domain model layer.

#### Generic Repository

**Interface**: `IRepository<T>`
**Implementation**: `Repository<T>`

Provides basic CRUD operations for all entities:
- `GetAllAsync()`
- `GetByIdAsync(id)`
- `AddAsync(entity)`
- `Update(entity)`
- `Delete(entity)`
- `AnyAsync(predicate)`
- `GetAllWithIncludesAsync(includes)`
- `GetByIdWithIncludesAsync(id, includes)`

#### Specific Repositories

Each entity has its own repository interface and implementation with specialized methods:

1. **Project Repository**
   - `IProjectRepository` / `ProjectRepository`
   - Methods: GetByOwnerAsync, GetByStatusAsync, GetWithTasksAsync, etc.

2. **Task Repository**
   - `ITaskRepository` / `TaskRepository`
   - Methods: GetByProjectIdAsync, GetByAssignedUserAsync, GetOverdueTasksAsync, etc.

3. **Comment Repository**
   - `ICommentRepository` / `CommentRepository`
   - Methods: GetByTaskIdAsync, GetByUserIdAsync, GetRecentCommentsAsync, etc.

4. **Notification Repository**
   - `INotificationRepository` / `NotificationRepository`
   - Methods: GetByUserIdAsync, GetUnreadByUserIdAsync, MarkAsReadAsync, etc.

5. **File Attachment Repository**
   - `IFileAttachmentRepository` / `FileAttachmentRepository`
   - Methods: GetByTaskIdAsync, GetByUserIdAsync, GetTotalFileSizeByUserAsync, etc.

6. **Audit Log Repository**
   - `IAuditLogRepository` / `AuditLogRepository`
   - Methods: GetByUserIdAsync, GetByActionAsync, GetByDateRangeAsync, etc.

### Unit of Work Pattern

The Unit of Work pattern maintains a list of objects affected by a business transaction and coordinates writing out changes and resolving concurrency problems.

**Interface**: `IUnitOfWork`
**Implementation**: `UnitOfWork`

#### Features

- **Repository Management**: Provides access to all repositories through properties
- **Transaction Management**: BeginTransactionAsync, CommitTransactionAsync, RollbackTransactionAsync
- **Change Tracking**: SaveChangesAsync for persisting changes
- **Generic Repository Access**: GetRepository<T>() for dynamic repository access
- **Proper Disposal**: Implements IDisposable for resource cleanup

## Service Layer Integration

Services have been created to use the Unit of Work pattern:

### Original Services (Direct DbContext)
- `ProjectService`
- `TaskService`
- `NotificationService`

### New Services (Unit of Work)
- `ProjectServiceWithUoW`
- `TaskServiceWithUoW`
- `NotificationServiceWithUoW`

## Benefits

1. **Separation of Concerns**: Clear separation between data access and business logic
2. **Testability**: Easy to mock repositories for unit testing
3. **Consistency**: Unit of Work ensures all changes are committed together
4. **Transaction Management**: Proper handling of database transactions
5. **Maintainability**: Centralized data access logic
6. **Flexibility**: Easy to switch between different data access implementations

## Usage Examples

### Using Repository Pattern

```csharp
public class ProjectController : Controller
{
    private readonly IProjectServiceWithUoW _projectService;
    
    public ProjectController(IProjectServiceWithUoW projectService)
    {
        _projectService = projectService;
    }
    
    public async Task<IActionResult> Index()
    {
        var projects = await _projectService.GetAllProjectsAsync();
        return View(projects);
    }
}
```

### Using Unit of Work Directly

```csharp
public class CustomService
{
    private readonly IUnitOfWork _unitOfWork;
    
    public CustomService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }
    
    public async Task ComplexOperationAsync()
    {
        await _unitOfWork.BeginTransactionAsync();
        
        try
        {
            // Multiple repository operations
            var project = await _unitOfWork.Projects.GetByIdAsync(1);
            var tasks = await _unitOfWork.Tasks.GetByProjectIdAsync(1);
            
            // Modify entities
            project.Status = ProjectStatus.Completed;
            
            // Save all changes together
            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransactionAsync();
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync();
            throw;
        }
    }
}
```

## Dependency Injection Setup

The repositories and Unit of Work are registered in `Program.cs`:

```csharp
// Register repositories
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
builder.Services.AddScoped<IProjectRepository, ProjectRepository>();
builder.Services.AddScoped<ITaskRepository, TaskRepository>();
builder.Services.AddScoped<ICommentRepository, CommentRepository>();
builder.Services.AddScoped<INotificationRepository, NotificationRepository>();
builder.Services.AddScoped<IFileAttachmentRepository, FileAttachmentRepository>();
builder.Services.AddScoped<IAuditLogRepository, AuditLogRepository>();

// Register Unit of Work
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// Register services with Unit of Work pattern
builder.Services.AddScoped<IProjectServiceWithUoW, ProjectServiceWithUoW>();
builder.Services.AddScoped<ITaskServiceWithUoW, TaskServiceWithUoW>();
builder.Services.AddScoped<INotificationServiceWithUoW, NotificationServiceWithUoW>();
```

## File Structure

```
Repositories/
├── IRepository.cs              # Generic repository interface
├── Repository.cs               # Generic repository implementation
├── IProjectRepository.cs       # Project-specific repository interface
├── ProjectRepository.cs        # Project repository implementation
├── ITaskRepository.cs          # Task-specific repository interface
├── TaskRepository.cs           # Task repository implementation
├── ICommentRepository.cs       # Comment-specific repository interface
├── CommentRepository.cs        # Comment repository implementation
├── INotificationRepository.cs  # Notification-specific repository interface
├── NotificationRepository.cs   # Notification repository implementation
├── IFileAttachmentRepository.cs # File attachment repository interface
├── FileAttachmentRepository.cs # File attachment repository implementation
├── IAuditLogRepository.cs      # Audit log repository interface
└── AuditLogRepository.cs       # Audit log repository implementation

UnitOfWork/
├── IUnitOfWork.cs              # Unit of Work interface
└── UnitOfWork.cs               # Unit of Work implementation

Services/
├── ProjectServiceWithUoW.cs    # Project service using Unit of Work
├── TaskServiceWithUoW.cs       # Task service using Unit of Work
└── NotificationServiceWithUoW.cs # Notification service using Unit of Work
```

## Migration Guide

To migrate from direct DbContext usage to the Repository Pattern:

1. **Replace Service Dependencies**: Change from `ApplicationDbContext` to `IUnitOfWork`
2. **Update Method Calls**: Use repository methods instead of DbSet operations
3. **Add Transaction Management**: Wrap operations in transactions where needed
4. **Update Dependency Injection**: Register new services in Program.cs
5. **Update Controllers**: Inject new service interfaces

## Best Practices

1. **Use Transactions**: Always use transactions for operations that modify multiple entities
2. **Handle Exceptions**: Properly handle and rollback transactions on errors
3. **Logging**: Add comprehensive logging for debugging and monitoring
4. **Async Operations**: Use async/await for all database operations
5. **Resource Disposal**: Ensure proper disposal of Unit of Work instances
6. **Testing**: Create mock implementations for unit testing

## Conclusion

The Repository Pattern and Unit of Work implementation provides a robust, maintainable, and testable data access layer for the Collaborative Task Management System. It ensures data consistency, improves code organization, and facilitates easier testing and maintenance.