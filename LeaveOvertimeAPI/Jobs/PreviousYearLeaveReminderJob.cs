using LeaveOvertimeAPI.Data;
using LeaveOvertimeAPI.Services;
using Microsoft.EntityFrameworkCore;
using Quartz;
using System.Runtime.CompilerServices;

namespace LeaveOvertimeAPI.Jobs;

public class PreviousYearLeaveReminderJob : IJob
{
    private readonly AppDbContext _context;
    private readonly IEmailService _emailService;
    private readonly ILogger<PreviousYearLeaveReminderJob> _logger;
    public PreviousYearLeaveReminderJob(AppDbContext context, IEmailService emailService, ILogger<PreviousYearLeaveReminderJob> logger)

    {
        _context = context;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        _logger.LogInformation("PreviousYearLeaveReminderJob: Duke kontrolluar lejet e mbartura...");

        // Punonjesit me dite pushimi te mbartura nga viti i kaluar
        var employees = await _context.Employees
            .Where(e => e.Status == EmployeeStatus.Active && !e.IsDeleted && e.UsedVacationDays < e.VacationDaysPerYear)
            .ToListAsync();

        int notified = 0;

        foreach (var employee in employees)
        {
            int remainingDays = employee.VacationDaysPerYear - employee.UsedVacationDays;


            await _emailService.SendAsync(employee.Email,
                "Kujtese: Lejet e vitit te kaluar skadojne me 31 Mars",
                $"Pershendetje {employee.FirstName}, keni {remainingDays} dite leje te mbartura nga viti i kaluar. " +
                $"Keto dite mund te perdoren vetem deri me 31 Mars. Pas kesaj date nuk jane me te vlefshme.");

            notified++;

        }

        _logger.LogInformation("PreviousYearLeaveReminderJob: U njoftuan {Count} punonjes.", notified);

    }
}

