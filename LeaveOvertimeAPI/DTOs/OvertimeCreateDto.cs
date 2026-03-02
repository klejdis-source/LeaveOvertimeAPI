namespace LeaveOvertimeAPI.DTOs
{
    public class OvertimeCreateDto
    {
        public DateTime? Date { get; set; }
        public decimal? HoursWorked { get; set; } // Max 12 orë 
        public string? Description { get; set; }
        
    }
}
