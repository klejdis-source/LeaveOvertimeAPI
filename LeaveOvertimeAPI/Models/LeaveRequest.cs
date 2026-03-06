using System.Globalization;

namespace LeaveOvertimeAPI.Models
{
    public class LeaveRequest
    {
        //te detyrueshme id ,reason, type, status 
        public Guid? Id { get; set; }
        public Guid EmployeeId { get; set; }
        public string? Type { get; set; } // Vacation , sick , unpaid
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string? Reason { get; set; }
        public string? Status { get; set; } // pending , approved , rejected 
        public Guid? ApprovedBy { get; set; }
        public int TotalDays { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public virtual Employee Employee { get; set; }
    }
}

public enum LeaveType
{
    Vacation,
    Sick,
    Unpaid,
}