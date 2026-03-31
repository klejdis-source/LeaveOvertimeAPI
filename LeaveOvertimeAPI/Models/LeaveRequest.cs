using System.Globalization;

namespace LeaveOvertimeAPI.Models
{
    public class LeaveRequest
    {
        //te detyrueshme id ,reason, type, status 
        public Guid Id { get; set; }
        public Guid EmployeeId { get; set; }
        public LeaveType? Type { get; set; } // Vacation , sick , unpaid //update ne db
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string? Reason { get; set; }
        public string? Status { get; set; } // pending , approved , rejected 
        public Guid? ApprovedBy { get; set; }
        public int TotalDays { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public Employee Employee { get; set; }

        // Soft Delete
        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }   
    }
}

public enum LeaveType
{
   Vacation = 0,
    Sick = 1,
    Unpaid = 2
}