using LeaveOvertimeAPI.Data;
using LeaveOvertimeAPI.Services;
using Microsoft.EntityFrameworkCore;
using Quartz;

namespace LeaveOvertimeAPI.Jobs;

public class NewYearGreetingJob : IJob
{
    private readonly AppDbContext _context;
    private readonly IEmailService _emailService;
    private readonly ILogger<NewYearGreetingJob> _logger;

    public NewYearGreetingJob(AppDbContext context, IEmailService emailService, ILogger<NewYearGreetingJob> logger)
    {
        _context = context;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        _logger.LogInformation("NewYearGreetingJob: Duke derguar urime per Vitin e Ri...");

        var employees = await _context.Employees
            .Where(e => e.Status == EmployeeStatus.Active && !e.IsDeleted)
            .ToListAsync();

        foreach (var employee in employees)
        {
            await _emailService.SendAsync(employee.Email,
               "Urime Vitin e Ri",
               "Ju urojmë shëndet, suksese dhe një vit të mbarë!");
        }

        _logger.LogInformation("NewYearGreetingJob: U derguan urime per {Count} punonjes.", employees.Count);




    }
}
