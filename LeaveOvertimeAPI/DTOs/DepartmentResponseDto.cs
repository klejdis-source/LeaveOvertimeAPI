namespace LeaveOvertimeAPI.DTOs
{
    public class DepartmentResponseDto
    {
        public Guid? Id { get; set; }
        public string? Name { get; set; } 
        public string? Description { get; set; }
        public Guid? ManagerId { get; set; }
        public string? ManagerName { get; set; }
    }
}
