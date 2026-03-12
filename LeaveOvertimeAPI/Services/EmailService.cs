namespace LeaveOvertimeAPI.Services;

public class EmailService : IEmailService
{
    private readonly ILogger<EmailService> _logger;

    public EmailService(ILogger<EmailService> logger)
    {
        _logger = logger;
    }

    public Task SendAsync(string to, string subject, string message)
    {
        _logger.LogInformation(
           "[Email] To: {To} | Subject: {Subject} | Message: {Message}",
            to, subject, message);

        Console.WriteLine($"[EMAIL] To: {to} | Subject: {subject} | Message: {message}");

        return Task.CompletedTask;
    }
}