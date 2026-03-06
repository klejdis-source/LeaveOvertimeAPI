using System.ComponentModel.DataAnnotations;

namespace LeaveOvertimeAPI.Models
{
    public class Overtime
    {
        // primary keys GUID 
        public Guid Id { get; set; }

        public Guid EmployeeId { get; set; }
        public Employee Employee { get; set; }

        public DateTime Date { get; set; }
        public decimal HoursWorked { get; set; }
        [Range(1, 12, ErrorMessage = "Maksimumi 12 orë shtesë në ditë")]
        public string Description { get; set; }

        public string Status { get; set; } // Pending, Approved, Rejected

        public Guid? ApprovedBy { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    }
}

public enum OvertimeStatus
{
    Pending,
    Approved,
    Rejected,
}