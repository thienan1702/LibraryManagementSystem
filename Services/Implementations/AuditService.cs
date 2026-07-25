using LibraryManagement.Data;
using LibraryManagement.Models;

namespace LibraryManagement.Services.Implementations
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
            _context.AuditLogs.Add(new AuditLog
            {
                UserName = user,
                Action = action,
                Entity = entity,
                EntityId = entityId,
                Description = description,
                Time = DateTime.Now
            });

            await _context.SaveChangesAsync();
        }
    }
}