using LeaveOvertimeAPI.Data;
using LeaveOvertimeAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace LeaveOvertimeAPI.Services
{
    public class LeaveStatusService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<LeaveStatusService> _logger;

        public LeaveStatusService(IServiceProvider serviceProvider, ILogger<LeaveStatusService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Leave Status Background Service po nis punen...");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                    // 1. Llogaritja automatike e totalit te shtesave per muajin aktual
                    var currentMonth = DateTime.Now.Month;
                    var currentYear = DateTime.Now.Year;

                    var totalOvertime = await context.Overtimes
                        .Where(o => o.Status == "Approved" && o.Date.Month == currentMonth && o.Date.Year == currentYear)
                        .SumAsync(o => o.HoursWorked, stoppingToken);

                    _logger.LogInformation($"[Raporti] Total ore shtese per muajin {currentMonth}: {totalOvertime} ore.");

                    // 2. Verifikimi i lejeve qe fillojne sot
                    var today = DateTime.Today;
                    var startingToday = await context.LeaveRequests
                        .Where(l => l.StartDate == today && l.Status == "Approved")
                        .Include(l => l.Employee)
                        .ToListAsync(stoppingToken);

                    foreach (var leave in startingToday)
                        _logger.LogInformation($"Punonjesi {leave.Employee.FirstName} fillon lejen sot ({leave.Type}).");

                    // 3. Refuzo automatikisht kerkesat Pending me StartDate te kaluar
                    var expiredRequests = await context.LeaveRequests
                        .Where(l => l.Status == "Pending" && l.StartDate.Date < today && !l.IsDeleted)
                        .ToListAsync(stoppingToken);

                    if (expiredRequests.Count > 0)
                    {
                        foreach (var request in expiredRequests)
                            request.Status = "Rejected";

                        await context.SaveChangesAsync(stoppingToken);
                        _logger.LogInformation($"LeaveStatusService: {expiredRequests.Count} kerkesa u refuzuan automatikisht.");
                    }

                    // 4. BONUS: Reseto balancen e lejeve ne 1 Janar cdo vit
                    if (DateTime.UtcNow.Month == 1 && DateTime.UtcNow.Day == 1)
                    {
                        var employees = await context.Employees
                            .Where(e => !e.IsDeleted)
                            .ToListAsync(stoppingToken);

                        foreach (var emp in employees)
                            emp.UsedVacationDays = 0;

                        await context.SaveChangesAsync(stoppingToken);
                        _logger.LogInformation($"LeaveStatusService: Balanca e lejeve u resetua per {employees.Count} punonjes.");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Gabim gjate ekzekutimit te sherbimit: {ex.Message}");
                }

                // Sherbimi pret 24 ore per te bere kontrollin e radhes
                await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
            }
        }
    }
}