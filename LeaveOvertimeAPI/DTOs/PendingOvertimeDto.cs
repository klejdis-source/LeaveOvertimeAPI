namespace LeaveOvertimeAPI.DTOs
{
    public class PendingOvertimeDto
    {
        public Guid Id { get; set; }
        public string EmployeeName { get; set; }
        public DateTime Date { get; set; } 
        public double HoursWorked { get; set; }
        public DateTime CreatedAt { get; set; }

        public PendingOvertimeDto(Guid id, string employeeName, DateTime date, double hours, DateTime CreatedAt)
        {
            Id = id;
            EmployeeName = employeeName;
            Date = date;
            HoursWorked = HoursWorked;
            CreatedAt = CreatedAt;
        }
    }
}
