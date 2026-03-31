using Quartz.Impl.Triggers;

namespace LeaveOvertimeAPI.DTOs
{
    public class UploadBase64DocumentDto
    {
        public string? FileName { get; set; }
        public string? ContentType { get; set; }
        public string? Base64Content { get; set; }
    }
}
