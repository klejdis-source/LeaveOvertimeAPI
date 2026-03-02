namespace LeaveOvertimeAPI.DTOs
{
    public class UpdateEmployeeDto
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Position { get; set; }
        public decimal? Salary { get; set; }
        public EmployeeStatus? Status { get; set; }
        public Roles? Roles { get; set; }
        public int? ManagerId { get; set; }


    }
    
}
