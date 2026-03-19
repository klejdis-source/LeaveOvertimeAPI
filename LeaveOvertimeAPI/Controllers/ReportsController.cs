using LeaveOvertimeAPI.DTOs;
using LeaveOvertimeAPI.Data;
using LeaveOvertimeAPI.Models;
using LeaveOvertimeAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace LeaveOvertimeManagement.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin,Manager")]
public class ReportsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ReportExportService _reportExport;

    public ReportsController(AppDbContext db, ReportExportService reportExport)
    {
        _db = db;
        _reportExport = reportExport;
    }

    [HttpGet("leave-summary")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<IEnumerable<LeaveSummaryDto>>> GetLeaveSummary(
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null)
    {
        var query = _db.LeaveRequests
            .Include(l => l.Employee)
            .Where(l => l.Status == "Approved")
            .AsQueryable();

        if (from.HasValue) query = query.Where(l => l.StartDate >= from.Value);
        if (to.HasValue) query = query.Where(l => l.EndDate <= to.Value);

        var grouped = await query
            .GroupBy(l => new { l.EmployeeId, l.Employee.FirstName, l.Employee.LastName })
            .ToListAsync();

        var result = grouped.Select(g => new LeaveSummaryDto
        {
            EmployeeId = g.Key.EmployeeId,
            EmployeeName = $"{g.Key.FirstName} {g.Key.LastName}",
            TotalVacationDays = g.Where(l => l.Type == "Vacation").Sum(l => l.TotalDays),
            TotalSickDays = g.Where(l => l.Type == "Sick").Sum(l => l.TotalDays),
            TotalUnpaidDays = g.Where(l => l.Type == "Unpaid").Sum(l => l.TotalDays)
        }).ToList();

        return Ok(result);
    }

    [HttpGet("overtime-summary")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<IEnumerable<OvertimeSummaryDto>>> GetOvertimeSummary(
        [FromQuery] int? month = null,
        [FromQuery] int? year = null)
    {
        var targetMonth = month ?? DateTime.UtcNow.Month;
        var targetYear = year ?? DateTime.UtcNow.Year;

        var grouped = await _db.Overtimes
            .Include(o => o.Employee)
            .Where(o => o.Status == "Approved" &&
                        o.Date.Month == targetMonth &&
                        o.Date.Year == targetYear)
            .GroupBy(o => new { o.EmployeeId, o.Employee.FirstName, o.Employee.LastName })
            .ToListAsync();

        var result = grouped.Select(g => new OvertimeSummaryDto
        {
            EmployeeId = g.Key.EmployeeId,
            EmployeeName = $"{g.Key.FirstName} {g.Key.LastName}",
            Month = targetMonth,
            Year = targetYear,
            TotalHours = (double)g.Sum(o => o.HoursWorked)
        }).ToList();

        return Ok(new { Month = targetMonth, Year = targetYear, Summary = result });
    }

    [HttpGet("pending")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> GetPendingRequests()
    {
        var pendingLeaves = await _db.LeaveRequests
            .Include(l => l.Employee)
            .Where(l => l.Status == "Pending")
            .Select(l => new PendingLeaveDto(
                l.Id,
                $"{l.Employee.FirstName} {l.Employee.LastName}",
                l.Type,
                l.StartDate,
                l.EndDate,
                l.TotalDays,
                l.Status,
                l.CreatedAt
            ))
            .ToListAsync();

        var pendingOvertime = await _db.Overtimes
            .Include(o => o.Employee)
            .Where(o => o.Status == "Pending")
            .Select(o => new PendingOvertimeDto(
                o.Id,
                $"{o.Employee.FirstName} {o.Employee.LastName}",
                o.Date,
                (double)o.HoursWorked,
                o.CreatedAt
            ))
            .ToListAsync();

        return Ok(new
        {
            PendingLeaveRequests = pendingLeaves,
            PendingOvertimeRequests = pendingOvertime
        });
    }

    [HttpGet("my-team")]
    [Authorize(Roles = "Manager")]
    public async Task<ActionResult> GetMyTeamReport(
        [FromQuery] int? month = null,
        [FromQuery] int? year = null)
    {
        var currentId = Guid.Parse(User.FindFirstValue("employeeId")!);

        var subordinateIds = await _db.Employees
            .Where(e => e.ManagerId == currentId)
            .Select(e => e.Id)
            .ToListAsync();

        var leavesQuery = _db.LeaveRequests
            .Include(l => l.Employee)
            .Where(l => subordinateIds.Contains(l.EmployeeId))
            .AsQueryable();

        var overtimeQuery = _db.Overtimes
            .Include(o => o.Employee)
            .Where(o => subordinateIds.Contains(o.EmployeeId))
            .AsQueryable();

        if (month.HasValue)
        {
            leavesQuery = leavesQuery.Where(l => l.StartDate.Month == month.Value);
            overtimeQuery = overtimeQuery.Where(o => o.Date.Month == month.Value);
        }

        if (year.HasValue)
        {
            leavesQuery = leavesQuery.Where(l => l.StartDate.Year == year.Value);
            overtimeQuery = overtimeQuery.Where(o => o.Date.Year == year.Value);
        }

        var leaves = await leavesQuery
            .Select(l => new {
                l.Id,
                EmployeeName = $"{l.Employee.FirstName} {l.Employee.LastName}",
                l.Type,
                l.StartDate,
                l.EndDate,
                l.TotalDays,
                l.Status
            }).ToListAsync();

        var overtime = await overtimeQuery
            .Select(o => new {
                o.Id,
                EmployeeName = $"{o.Employee.FirstName} {o.Employee.LastName}",
                o.Date,
                o.HoursWorked,
                o.Status
            }).ToListAsync();

        return Ok(new { Leaves = leaves, Overtime = overtime });
    }

    // GET: api/reports/overtime/monthly/export
    [HttpGet("overtime/monthly/export")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> ExportMonthlyOvertime(
        [FromQuery] DateTime from,
        [FromQuery] DateTime to)
    {
        var currentUserId = Guid.Parse(User.FindFirstValue("employeeId")!);
        var currentRole = User.FindFirstValue(ClaimTypes.Role);

        var result = await _reportExport.ExportMonthlyOvertimeAsync(from, to, currentUserId, currentRole);

        return File(result.Content, result.ContentType, result.FileName);
    }
}