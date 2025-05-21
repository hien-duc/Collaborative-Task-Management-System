using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Collaborative_Task_Management_System.Models;

namespace Collaborative_Task_Management_System.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Project> Projects { get; set; }
        public DbSet<TaskItem> Tasks { get; set; }
        public DbSet<Comment> Comments { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }
        public DbSet<FileAttachment> FileAttachments { get; set; }
        public DbSet<Notification> Notifications { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Configure Project relationships
            builder.Entity<Project>()
                .HasOne(p => p.CreatedBy)
                .WithMany(u => u.CreatedProjects)
                .HasForeignKey(p => p.CreatedById)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure TaskItem relationships
            builder.Entity<TaskItem>()
                .HasOne(t => t.Project)
                .WithMany(p => p.Tasks)
                .HasForeignKey(t => t.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<TaskItem>()
                .HasOne(t => t.AssignedUser)
                .WithMany(u => u.AssignedTasks)
                .HasForeignKey(t => t.AssignedUserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<TaskItem>()
                .HasOne(t => t.CreatedBy)
                .WithMany(u => u.CreatedTasks)
                .HasForeignKey(t => t.CreatedById)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure Comment relationships
            builder.Entity<Comment>()
                .HasOne(c => c.Task)
                .WithMany(t => t.Comments)
                .HasForeignKey(c => c.TaskId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Comment>()
                .HasOne(c => c.User)
                .WithMany(u => u.Comments)
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure FileAttachment relationships
            builder.Entity<FileAttachment>()
                .HasOne(f => f.Task)
                .WithMany(t => t.FileAttachments)
                .HasForeignKey(f => f.TaskId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<FileAttachment>()
                .HasOne(f => f.UploadedBy)
                .WithMany(u => u.FileAttachments)
                .HasForeignKey(f => f.UploadedById)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure AuditLog relationships
            builder.Entity<AuditLog>()
                .HasOne(a => a.User)
                .WithMany(u => u.AuditLogs)
                .HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.Restrict);


            builder.Entity<Notification>()
                .HasOne(n => n.User)
                .WithMany()
                .HasForeignKey(n => n.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Add indexes for performance optimization
            builder.Entity<TaskItem>()
                .HasIndex(t => t.ProjectId)
                .HasDatabaseName("IX_Tasks_ProjectId");

            builder.Entity<TaskItem>()
                .HasIndex(t => t.AssignedUserId)
                .HasDatabaseName("IX_Tasks_AssignedToId");

            builder.Entity<TaskItem>()
                .HasIndex(t => t.Status)
                .HasDatabaseName("IX_Tasks_Status");

            builder.Entity<Comment>()
                .HasIndex(c => c.TaskId)
                .HasDatabaseName("IX_Comments_TaskId");

            builder.Entity<FileAttachment>()
                .HasIndex(f => f.TaskId)
                .HasDatabaseName("IX_FileAttachments_TaskId");

            builder.Entity<Notification>()
                .HasIndex(n => n.UserId)
                .HasDatabaseName("IX_Notifications_UserId");

            builder.Entity<AuditLog>()
                .HasIndex(a => a.Timestamp)
                .HasDatabaseName("IX_AuditLogs_Timestamp");
        }
    }
}