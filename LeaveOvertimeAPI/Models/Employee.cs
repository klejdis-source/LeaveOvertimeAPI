using Microsoft.AspNetCore.Authorization.Infrastructure;
using System.Globalization;

namespace LeaveOvertimeAPI.Models
{
    //te detyrueshem ID , email, role, 
    public class Employee
    {
        public Guid? Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string? Email { get; set; }
        public string Password { get; set; } 
        public string? Position { get; set; }
        public decimal Salary { get; set; }
        public DateTime HireDate { get; set; }
        public EmployeeStatus Status { get; set; } //Active or Inactive 
        public Roles? Roles { get; set; }
        public Guid? ManagerId { get; set; } 
        public Employee Manager { get; set; }

        public Guid? DepartmentId { get; set; }
        public Department? Department { get; set; }

        public int VacationDaysPerYear { get; set; } = 20;
        public int UsedVacationDays { get; set; } = 0;

        public bool IsDeleted { get; set; } = false;
        public DateTime DeletedAt { get; set; }


        public ICollection<Employee> Subordinates { get; set; } = new List<Employee>();
        public ICollection<LeaveRequest> LeaveRequest { get; set; } = new List<LeaveRequest>();
        public ICollection<Overtime> Overtime { get; set; } = new List<Overtime>();


    }

}
public enum Roles
{
    Admin,
    Manager,
    Employee,
}

public enum EmployeeStatus
{
    Active = 1,
    Inactive = 2
}


