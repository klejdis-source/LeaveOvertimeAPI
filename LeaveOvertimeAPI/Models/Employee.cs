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
        public string Position { get; set; }
        public decimal Salary { get; set; }
        public DateTime HireDate { get; set; }
        public string Status { get; set; }
        public Roles? Roles { get; set; }

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





