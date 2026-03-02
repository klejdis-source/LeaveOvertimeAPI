namespace LeaveOvertimeAPI.DTOs
{
    public class SignupDto
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Position { get; set; } = string.Empty;
        
        public Roles Roles { get; set; }
    }
}
