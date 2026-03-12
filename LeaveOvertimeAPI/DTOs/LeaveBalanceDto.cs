using System.Globalization;

namespace LeaveOvertimeAPI.DTOs
{
    public class LeaveBalanceDto
    {
        public Guid? EmployeeId { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public int TotalDaysPerYear { get; set; }
        public int UsedDays { get; set; }
        public int RemainingDays { get; set; } 
    }
}