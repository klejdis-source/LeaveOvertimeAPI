namespace LeaveOvertimeAPI.Services;

public interface IEmailService
{
    Task SendAsync(string to, string subject, string message);
}