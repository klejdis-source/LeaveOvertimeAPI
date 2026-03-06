namespace LeaveOvertimeAPI.DTOs
{
    public class LeaveResponseDto
    {
    public Guid Id { get; set; }
    public Guid EmployeeId { get; set; }
    public string? EmployeeName { get; set; }
    public string? Type { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string? Reason { get; set; }
    public string Status { get; set; } 
    public int TotalDays { get; set; }
    public string? ApprovedByName { get; set; }
    public DateTime CreatedAt { get; set; }
    public int DaysOfLeave { get; set; }

    }
}
