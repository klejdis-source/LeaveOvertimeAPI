using LeaveOvertimeAPI.Data;
using LeaveOvertimeAPI.DTOs;
using LeaveOvertimeAPI.Models;
using LeaveOvertimeAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace LeaveOvertimeAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class LeaveRequestsController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly IEmailService _email;

        public LeaveRequestsController(AppDbContext db, IEmailService email)
        {
            _db = db;
            _email = email;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll(
            [FromQuery] string? status = null,
            [FromQuery] DateTime? from = null,
            [FromQuery] DateTime? to = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            var currentId = Guid.Parse(User.FindFirstValue("employeeId")!);
            var currentRole = User.FindFirstValue(ClaimTypes.Role);

            var query = _db.LeaveRequests.Include(l => l.Employee).AsQueryable();

            if (currentRole == "Employee")
                query = query.Where(l => l.EmployeeId == currentId);
            else if (currentRole == "Manager")
            {
                var subordinateIds = await _db.Employees
                    .Where(e => e.ManagerId == currentId)
                    .Select(e => e.Id).ToListAsync();
                subordinateIds.Add(currentId);
                query = query.Where(l => subordinateIds.Contains(l.EmployeeId));
            }

            if (!string.IsNullOrEmpty(status)) query = query.Where(l => l.Status == status);
            if (from.HasValue) query = query.Where(l => l.StartDate >= from.Value);
            if (to.HasValue) query = query.Where(l => l.EndDate <= to.Value);

            var total = await query.CountAsync();
            var items = await query
                .OrderByDescending(l => l.CreatedAt)
                .Skip((page - 1) * pageSize).Take(pageSize)
                .Select(l => new {
                    l.Id,
                    l.EmployeeId,
                    EmployeeName = l.Employee.FirstName + " " + l.Employee.LastName,
                    l.Type,
                    l.StartDate,
                    l.EndDate,
                    l.TotalDays,
                    l.Reason,
                    l.Status,
                    l.ApprovedBy,
                    l.CreatedAt
                }).ToListAsync();

            return Ok(new { TotalCount = total, Page = page, PageSize = pageSize, Items = items });
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var currentId = Guid.Parse(User.FindFirstValue("employeeId")!);
            var currentRole = User.FindFirstValue(ClaimTypes.Role);

            var leave = await _db.LeaveRequests.Include(l => l.Employee).FirstOrDefaultAsync(l => l.Id == id);
            if (leave == null) return NotFound(new { message = "Kerkesa nuk u gjet." });
            if (currentRole == "Employee" && leave.EmployeeId != currentId) return Forbid();

            return Ok(leave);
        }

        [HttpGet("check-overlap")]
        public async Task<IActionResult> CheckOverlap([FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
        {
            var currentId = Guid.Parse(User.FindFirstValue("employeeId")!);
            if (endDate < startDate) return BadRequest(new { message = "EndDate nuk mund te jete para StartDate." });

            var overlapping = await _db.LeaveRequests
                .Where(l => l.EmployeeId == currentId && l.Status == "Approved" &&
                            l.StartDate <= endDate && l.EndDate >= startDate)
                .Select(l => new { l.Id, l.Type, l.StartDate, l.EndDate, l.TotalDays, l.Status })
                .ToListAsync();

            if (overlapping.Any())
                return Conflict(new { message = "Ekziston nje leje e aprovuar qe mbivendoset.", hasOverlap = true, overlappingLeaves = overlapping });

            return Ok(new { message = "Nuk ka mbivendosje. Mund te aplikoni per leje.", hasOverlap = false });
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateLeaveRequestDto dto)
        {
            var currentId = Guid.Parse(User.FindFirstValue("employeeId")!);

            if (dto.EndDate < dto.StartDate)
                return BadRequest(new { message = "EndDate nuk mund te jete para StartDate." });

            var hasOverlap = await _db.LeaveRequests.AnyAsync(l =>
                l.EmployeeId == currentId && l.Status == "Approved" &&
                l.StartDate <= dto.EndDate && l.EndDate >= dto.StartDate);

            if (hasOverlap)
                return Conflict(new { message = "Ekziston nje leje e aprovuar qe mbivendoset." });

            int totalDays = (dto.EndDate - dto.StartDate).Days + 1;

            if (dto.Type == LeaveType.Vacation)
            {
                var emp = await _db.Employees.FindAsync(currentId);
                if (emp != null)
                {
                    int remaining = emp.VacationDaysPerYear - emp.UsedVacationDays;
                    if (totalDays > remaining)
                        return BadRequest(new { message = $"Nuk keni dite te mjaftueshme. Keni {remaining} dite te mbetura." });
                }
            }

            var leave = new LeaveRequest
            {
                Id = Guid.NewGuid(),
                EmployeeId = currentId,
                Type = dto.Type,
                StartDate = dto.StartDate,
                EndDate = dto.EndDate,
                Reason = dto.Reason,
                TotalDays = totalDays,
                Status = "Pending",
                CreatedAt = DateTime.UtcNow
            };

            _db.LeaveRequests.Add(leave);
            _db.Auditlogs.Add(new Auditlog
            {
                UserId = currentId,
                Action = "Leave request created",
                EntityName = "LeaveRequest",
                EntityId = leave.Id.ToString(),
                Date = DateTime.UtcNow
            });

            await _db.SaveChangesAsync();

            var employee = await _db.Employees.FindAsync(currentId);
            var manager = employee?.ManagerId.HasValue == true
                ? await _db.Employees.FindAsync(employee.ManagerId.Value)
                : null;
            var managerEmail = manager?.Email ?? "manager@company.com";
            await _email.SendAsync(managerEmail, "Kerkese e re per leje",
                $"{employee?.FirstName} {employee?.LastName} ka bere kerkese per leje. Eshte ne pritje te aprovimit.");

            return CreatedAtAction(nameof(GetById), new { id = leave.Id }, leave);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] CreateLeaveRequestDto dto)
        {
            var currentId = Guid.Parse(User.FindFirstValue("employeeId")!);
            var leave = await _db.LeaveRequests.FindAsync(id);

            if (leave == null) return NotFound(new { message = "Kerkesa nuk u gjet." });
            if (leave.EmployeeId != currentId) return Forbid();
            if (leave.Status == "Approved") return BadRequest(new { message = "Nje kerkese e aprovuar nuk mund te modifikohet." });
            if (dto.EndDate < dto.StartDate) return BadRequest(new { message = "EndDate nuk mund te jete para StartDate." });

            leave.Type = dto.Type;
            leave.StartDate = dto.StartDate;
            leave.EndDate = dto.EndDate;
            leave.Reason = dto.Reason;
            leave.TotalDays = (dto.EndDate - dto.StartDate).Days + 1;

            await _db.SaveChangesAsync();
            return Ok(new { message = "Kerkesa u perditesua me sukses.", data = leave });
        }

        [HttpPut("{id}/approve")]
        [Authorize(Roles = "Manager,Admin")]
        public async Task<IActionResult> ApproveOrReject(Guid id, [FromBody] ApproveRejectDto dto)
        {
            var currentId = Guid.Parse(User.FindFirstValue("employeeId")!);
            var leave = await _db.LeaveRequests.Include(l => l.Employee).FirstOrDefaultAsync(l => l.Id == id);

            if (leave == null) return NotFound(new { message = "Kerkesa nuk u gjet." });
            if (leave.Status != "Pending") return BadRequest(new { message = "Vetem kerkesat Pending mund te aprovohen/refuzohen." });

            if (dto.Approve && leave.Type == LeaveType.Vacation)
            {
                var emp = await _db.Employees.FindAsync(leave.EmployeeId);
                if (emp != null)
                {
                    int remaining = emp.VacationDaysPerYear - emp.UsedVacationDays;
                    if (leave.TotalDays > remaining)
                        return BadRequest(new { message = $"Punonjesi nuk ka dite te mjaftueshme. Ka {remaining} dite te mbetura." });
                    emp.UsedVacationDays += leave.TotalDays;
                }
            }

            leave.Status = dto.Approve ? "Approved" : "Rejected";
            leave.ApprovedBy = currentId;

            _db.Auditlogs.Add(new Auditlog
            {
                UserId = currentId,
                Action = dto.Approve ? "Leave request approved" : "Leave request rejected",
                EntityName = "LeaveRequest",
                EntityId = leave.Id.ToString(),
                Date = DateTime.UtcNow
            });

            await _db.SaveChangesAsync();

            var subject = dto.Approve ? "Leja u aprovua" : "Leja u refuzua";
            var msg = dto.Approve
                ? $"Kerkesa juaj per leje nga {leave.StartDate:dd/MM/yyyy} deri {leave.EndDate:dd/MM/yyyy} u aprovua."
                : $"Kerkesa juaj per leje nga {leave.StartDate:dd/MM/yyyy} deri {leave.EndDate:dd/MM/yyyy} u refuzua.";
            await _email.SendAsync(leave.Employee?.Email ?? "", subject, msg);

            return Ok(leave);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var currentId = Guid.Parse(User.FindFirstValue("employeeId")!);
            var leave = await _db.LeaveRequests.FindAsync(id);

            if (leave == null) return NotFound(new { message = "Kerkesa nuk u gjet." });
            if (leave.EmployeeId != currentId) return Forbid();
            if (leave.Status != "Pending") return BadRequest(new { message = "Vetem kerkesat Pending mund te fshihen." });

            leave.IsDeleted = true;
            leave.DeletedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            return NoContent();
        }
    }
}