namespace LeaveOvertimeAPI.DTOs
{
    public class LeaveSummaryDto
    {
     public int EmployeeId { get; set; }
     public string EmployeeName { get; set; }
     public int TotalVacationDays { get; set; }
     public int TotalSickDays { get; set; }
     public int TotalUnpaidDays { get; set; }

        public LeaveSummaryDto(int employeeId, string employeeName, int totalVacationDays, int totalSickDays, int totalUnpaidDays)
        {
            EmployeeId = employeeId;
            EmployeeName = employeeName;
            TotalVacationDays = totalVacationDays;
            TotalSickDays = totalSickDays;
            TotalUnpaidDays = totalUnpaidDays;
        }
    }
}
