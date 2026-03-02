using System.ComponentModel.DataAnnotations;

namespace LeaveOvertimeAPI.DTOs
{
    public class ApproveRejectDto
    {
        public bool? Approve { get; set; }
        public string? Comment { get; set; }
    }
}
