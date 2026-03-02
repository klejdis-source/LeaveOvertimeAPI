namespace LeaveOvertimeAPI.DTOs
{
    public class EmployeeCreateDto
    {
            public string? FirstName { get; set; }
            
            public string? LastName { get; set; }
            
            public string? Email { get; set; }
            public string? Password { get; set; } 
           
            public string? Position { get; set; }
           
            public decimal Salary { get; set; }
            
            public DateTime HireDate { get; set; }
            
            public Roles? Role { get; set; }
            public int? ManagerId { get; set; }
    }
}
