namespace LeaveOvertimeAPI.DTOs
{
    public class PendingLeaveDto
    {
     public Guid Id { get; set; }
     public string EmployeeName { get; set; }
     public LeaveType LeaveType { get; set; }
     public DateTime StartDate { get; set; }
     public DateTime EndDate { get; set; }
     public int TotalDays { get; set; }
     public string Status { get; set; }
     public DateTime CreatedAt { get; set; }


        public PendingLeaveDto(Guid id, string employeeName, LeaveType leaveType, DateTime startDate, DateTime endDate, int totalDays, string status, DateTime createdAt)
        {
            Id = id;
            EmployeeName = employeeName;
            LeaveType = leaveType;
            StartDate = startDate;
            EndDate = endDate;
            TotalDays = totalDays;
            Status = "Pending";
            CreatedAt = createdAt;
        }
    }
}
