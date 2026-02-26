namespace LeaveOvertimeAPI.DTOs
{
    public class EmployeeCreateDto
    {
            public string FirstName { get; set; }
            
            public string LastName { get; set; }
            
            public string Email { get; set; }
           
            public string Position { get; set; }
           
            public decimal Salary { get; set; }
            
            public DateTime EmploymentDate { get; set; }
            
            public Roles Roles { get; set; } // Admin, Manager, Employee 
        
    }
}
