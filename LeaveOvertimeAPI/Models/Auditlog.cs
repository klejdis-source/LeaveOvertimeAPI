using System.Globalization;

namespace LeaveOvertimeAPI.Models
{
    public class Auditlog
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string? Action { get; set; }
        public string? EntityName { get; set; }
        public string? EntityId { get; set; } 
        public DateTime Date { get; set; } = DateTime.UtcNow;
        
    }
} 
