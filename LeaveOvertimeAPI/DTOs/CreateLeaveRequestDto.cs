namespace LeaveOvertimeAPI.DTOs
{
    public class CreateLeaveRequestDto
    {
        public string Type { get; set; }        // Vacation, Sick, Unpaid
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string Reason { get; set; }
    }
}