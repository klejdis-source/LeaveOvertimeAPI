namespace LeaveOvertimeAPI.DTOs
{
    public class MonthlyOvertimeReportRowDTO
    {
        public Guid EmployeeId { get; set; }
        public string? FullName { get; set; } 
        public string? Month { get; set; }
        public decimal ApprovedHours { get; set; }
        public int RequestCount { get; set; }
        public string? Department { get; set; }
        
    }
}
