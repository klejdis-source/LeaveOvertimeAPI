namespace LeaveOvertimeAPI.DTOs
{
    public class LeaveSummaryDto
    {
     public Guid EmployeeId { get; set; }
     public string EmployeeName { get; set; }
     public int TotalVacationDays { get; set; }
     public int TotalSickDays { get; set; }
     public int TotalUnpaidDays { get; set; }
      
    }
}
