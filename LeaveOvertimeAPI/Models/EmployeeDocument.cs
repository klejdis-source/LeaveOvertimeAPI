using System.Globalization;

namespace LeaveOvertimeAPI.Models
{
    public class EmployeeDocument
    {
        public Guid Id { get; set; }
        public Guid EmployeeId { get; set; }
        public string? FileName { get; set; }
        public string ContentType { get; set; }
        public string? Base64Content { get; set; }
        public long Size { get; set; }
        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
        
        public Employee? Employee { get; set; }

        //  Soft Delete
        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }

    }
}
