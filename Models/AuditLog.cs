using System.ComponentModel.DataAnnotations;

namespace LibraryManagement.Models
{
    public class AuditLog
    {
        public int Id { get; set; }

        [MaxLength(100)]
        public string UserName { get; set; }

        [MaxLength(100)]
        public string Action { get; set; }

        [MaxLength(100)]
        public string Entity { get; set; }

        public int EntityId { get; set; }

        public DateTime Time { get; set; }

        public string? Description { get; set; }
    }
}