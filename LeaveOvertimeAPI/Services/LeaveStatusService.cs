using LeaveOvertimeAPI.Data;
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
            _logger.LogInformation("Leave Status Background Service po nis punën...");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                        //Llogaritja automatike e totalit të orëve shtesë për muajin aktual
                        var currentMonth = DateTime.Now.Month;
                        var currentYear = DateTime.Now.Year;

                        var totalOvertime = await context.Overtimes
                            .Where(o => o.Status == "Approved" && o.Date.Month == currentMonth && o.Date.Year == currentYear)
                            .SumAsync(o => o.HoursWorked, stoppingToken);

                        _logger.LogInformation($"[Raporti] Total orë shtesë për muajin {currentMonth}: {totalOvertime} orë.");

                        // Verifikimi i lejeve që fillojnë sot 
                        var today = DateTime.Today;
                        var startingToday = await context.LeaveRequests
                            .Where(l => l.StartDate == today && l.Status == "Approved")
                            .Include(l => l.Employee)
                            .ToListAsync(stoppingToken);

                        foreach (var leave in startingToday)
                        {
                            _logger.LogInformation($"Punonjësi {leave.Employee.FirstName} fillon lejen sot ({leave.Type}).");
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Gabim gjatë ekzekutimit të shërbimit: {ex.Message}");
                }

                // Shërbimi pret 24 orë për të bërë kontrollin e radhës (ose sa ta caktosh ti)
                await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
            }
        }
    }
}