namespace LeaveOvertimeAPI.DTOs
{
    public class DocumentResponseDto
    {
        public Guid Id { get; set; }
        public string? FileName { get; set; }
        public string? ContentType { get; set; }
        public long Size { get; set; }
        public DateTime UploadedAt { get; set; }
        public Guid EmployeeId { get; set; }
        
    }
}
