namespace LeaveOvertimeAPI.DTOs
{
    public class OvertimeSummaryDto
    {
     public Guid EmployeeId { get; set; }
     public string? EmployeeName { get; set; }
     public int Month { get; set; }
     public int Year { get; set; }
     public double TotalHours { get; set; }

    }
}
