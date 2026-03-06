namespace LeaveOvertimeAPI.DTOs
{
    public class LeaveStatusUpdateDto
    {
        public string Status { get; set; } // Approved / Rejected 
        public Guid ManagerId { get; set; } // ApprovedBy 
    }
}
