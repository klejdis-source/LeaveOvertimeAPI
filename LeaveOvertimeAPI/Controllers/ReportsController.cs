using LeaveOvertimeAPI.DTOs;
using LeaveOvertimeAPI.Data;
using LeaveOvertimeAPI.Models;
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

    public ReportsController(AppDbContext db)
    {
        _db = db;
    }

    // Total ditë leje për çdo punonjës, me filtrim sipas periudhës.
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

        var result = await query
            .GroupBy(l => new { l.EmployeeId, l.Employee.FirstName, l.Employee.LastName })
            .Select(g => new LeaveSummaryDto(
                g.Key.EmployeeId,
                $"{g.Key.FirstName} {g.Key.LastName}",
                g.Where(l => l.Type == "Vacation").Sum(l => l.TotalDays),
                g.Where(l => l.Type == "Sick").Sum(l => l.TotalDays),
                g.Where(l => l.Type == "Unpaid").Sum(l => l.TotalDays)
            ))
            .ToListAsync();

        return Ok(result);
    }

    // Total orë shtesë të aprovuara, me filtrim sipas muajit dhe vitit.
    [HttpGet("overtime-summary")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<IEnumerable<OvertimeSummaryDto>>> GetOvertimeSummary(
        [FromQuery] int? month = null,
        [FromQuery] int? year = null)
    {
        var query = _db.Overtimes
            .Include(o => o.Employee)
            .Where(o => o.Status == "Approved")
            .AsQueryable();

        if (month.HasValue) query = query.Where(o => o.Date.Month == month.Value);
        if (year.HasValue) query = query.Where(o => o.Date.Year == year.Value);

        var result = await query
            .GroupBy(o => new
            {
                o.EmployeeId,
                o.Employee.FirstName,
                o.Employee.LastName,
                o.Date.Month,
                o.Date.Year
            })
            .Select(g => new OvertimeSummaryDto(
                g.Key.EmployeeId,
                $"{g.Key.FirstName} {g.Key.LastName}",
                g.Key.Month,
                g.Key.Year,
                (double)g.Sum(o => o.HoursWorked)
            ))
            .ToListAsync();

        return Ok(result);
    }

    // Lista e të gjitha kërkesave Pending (leje + orë shtesë).
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

    // Lejet dhe orët shtesë të stafit që menaxhon, me filtrim sipas muajit/vitit.
    [HttpGet("my-team")]
    [Authorize(Roles = "Manager")]
    public async Task<ActionResult> GetMyTeamReport(
        [FromQuery] int? month = null,
        [FromQuery] int? year = null)
    {
        var currentId = int.Parse(User.FindFirstValue("employeeId")!);

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
            .Select(l => new
            {
                l.Id,
                EmployeeName = $"{l.Employee.FirstName} {l.Employee.LastName}",
                l.Type,
                l.StartDate,
                l.EndDate,
                l.TotalDays,
                l.Status
            })
            .ToListAsync();

        var overtime = await overtimeQuery
            .Select(o => new
            {
                o.Id,
                EmployeeName = $"{o.Employee.FirstName} {o.Employee.LastName}",
                o.Date,
                o.HoursWorked,
                o.Status
            })
            .ToListAsync();

        return Ok(new { Leaves = leaves, Overtime = overtime });
    }
}