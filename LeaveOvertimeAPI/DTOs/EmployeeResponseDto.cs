using System;
using LeaveOvertimeAPI.Models;

namespace LeaveOvertimeAPI.DTOs
{
    public class EmployeeResponseDto
    {
        public Guid? Id { get; set; }  

        public string? FirstName { get; set; }

        public string? LastName { get; set; }

        public string? Email { get; set; }

        public string? Position { get; set; }

        public decimal Salary { get; set; }

        public DateTime HireDate { get; set; }

        public EmployeeStatus Status { get; set; }  //Active/Inactive

        public Roles? Role { get; set; } 

        public Guid? ManagerId { get; set; } 
        public Guid? DepartmentId { get; set; }
    }
}