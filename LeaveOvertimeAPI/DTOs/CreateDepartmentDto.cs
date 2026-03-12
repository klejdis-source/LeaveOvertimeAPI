using Microsoft.Identity.Client;
using System.Globalization;

namespace LeaveOvertimeAPI.DTOs
{
    public class CreateDepartmentDto
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public Guid? ManagerId { get; set; }
    }
}
