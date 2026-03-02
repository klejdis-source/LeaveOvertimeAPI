namespace LeaveOvertimeAPI.DTOs
{
    public class OvertimeSummaryDto
    {
     public int EmployeeId { get; set; }
     public string? EmployeeName { get; set; }
     public int Month { get; set; }
     public int Year { get; set; }
     public double TotalHours { get; set; }

        public OvertimeSummaryDto(int employeeId, string employeeName, int month, int year, double totalHours)
        {
            EmployeeId = employeeId;
            EmployeeName = employeeName;
            Month = month;
            Year = year;
            TotalHours = totalHours;
        }
    }
}
