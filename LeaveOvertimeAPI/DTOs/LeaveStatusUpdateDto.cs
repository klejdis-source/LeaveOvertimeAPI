namespace LeaveOvertimeAPI.DTOs
{
    public class LeaveStatusUpdateDto
    {
        public string Status { get; set; } // Approved / Rejected 
        public int ManagerId { get; set; } // ApprovedBy 
    }
}
