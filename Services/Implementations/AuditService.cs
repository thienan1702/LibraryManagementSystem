using LibraryManagement.Data;
using LibraryManagement.Models;

namespace LibraryManagement.Services
{
    public class AuditService : IAuditService
    {
        private readonly ApplicationDbContext _context;

        public AuditService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task SaveAsync(
            string user,
            string action,
            string entity,
            int entityId,
            string description)
        {
            var auditLog = new AuditLog
            {
                UserName = string.IsNullOrWhiteSpace(user)
                    ? "System"
                    : user,

                Action = action,

                Entity = entity,

                EntityId = entityId,

                Description = description,

                Time = DateTime.Now
            };

            _context.AuditLogs.Add(auditLog);

            await _context.SaveChangesAsync();
        }
    }
}