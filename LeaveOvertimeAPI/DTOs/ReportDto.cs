namespace LeaveOvertimeAPI.DTOs
{
    public class ReportDto
    {
        public string? EmployeeName { get; set; }
        public int? TotalLeaveDays { get; set; } // Llogaritja automatike 
        public decimal? TotalOvertimeHours { get; set; }
        
    }
}
