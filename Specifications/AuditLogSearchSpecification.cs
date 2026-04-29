using Collaborative_Task_Management_System.Models;

namespace Collaborative_Task_Management_System.Specifications
{
    public class AuditLogSearchSpecification : BaseSpecification<AuditLog>
    {
        public AuditLogSearchSpecification(string? userId, string? action, DateTime? fromDate, DateTime? toDate, int skip, int take)
            : base(a => 
                (string.IsNullOrEmpty(userId) || a.UserId == userId) &&
                (string.IsNullOrEmpty(action) || a.Action.Contains(action)) &&
                (!fromDate.HasValue || a.Timestamp >= fromDate.Value) &&
                (!toDate.HasValue || a.Timestamp <= toDate.Value))
        {
            AddInclude(a => a.User);
            ApplyOrderByDescending(a => a.Timestamp);
            ApplyPaging(skip, take);
        }
    }

    public class AuditLogSearchCountSpecification : BaseSpecification<AuditLog>
    {
        public AuditLogSearchCountSpecification(string? userId, string? action, DateTime? fromDate, DateTime? toDate)
            : base(a => 
                (string.IsNullOrEmpty(userId) || a.UserId == userId) &&
                (string.IsNullOrEmpty(action) || a.Action.Contains(action)) &&
                (!fromDate.HasValue || a.Timestamp >= fromDate.Value) &&
                (!toDate.HasValue || a.Timestamp <= toDate.Value))
        {
        }
    }

    public class AuditLogRecentSpecification : BaseSpecification<AuditLog>
    {
        public AuditLogRecentSpecification(int takeCount)
        {
            ApplyOrderByDescending(a => a.Timestamp);
            ApplyPaging(0, takeCount);
        }
    }
}
