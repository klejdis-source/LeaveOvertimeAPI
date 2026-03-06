namespace LeaveOvertimeAPI.DTOs
{
    public class OvertimeResponseDto
    {
        public Guid Id { get; set; }
        public  Guid EmployeeId { get; set; }
        public string EmployeeName { get; set; }
        public DateTime Date { get; set; }
        public double HoursWorked { get; set; }
        public string Description { get; set; }
        public string Status { get; set; }
        public string? ApprovedByName { get; set; }
        public  DateTime CreatedAt { get; set; }
    }
}
