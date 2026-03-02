using Microsoft.AspNetCore.Authorization.Infrastructure;
using System.Globalization;

namespace LeaveOvertimeAPI.Models
{
    //te detyrueshem ID , email, role, 
    public class Employee
    {
        public int? Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string? Email { get; set; }
        public string Password { get; set; } 
        public string Position { get; set; }
        public decimal Salary { get; set; }
        public DateTime HireDate { get; set; }
        public EmployeeStatus Status { get; set; } //Active or Inactive 
        public Roles? Roles { get; set; }
        public int? ManagerId { get; set; } 
        public Employee Manager { get; set; }

        public ICollection<LeaveRequest> LeaveRequest { get; set; }
        public ICollection<Overtime> Overtime { get; set; }


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



