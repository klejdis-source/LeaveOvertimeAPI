using LeaveOvertimeAPI.Data;
using LeaveOvertimeAPI.DTOs;
using LeaveOvertimeAPI.Models;
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

        public LeaveRequestsController(AppDbContext db)
        {
            _db = db;
        }

        // GET: api/leaverequests liston te gjitha kerkesat e lejes
        [HttpGet]
        public async Task<IActionResult> GetAll(
            [FromQuery] string? status = null,
            [FromQuery] DateTime? from = null,
            [FromQuery] DateTime? to = null,
            [FromQuery] Guid? employeeId = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            var currentId = Guid.Parse(User.FindFirstValue("employeeId")!);
            var currentRole = User.FindFirstValue(ClaimTypes.Role);

            var query = _db.LeaveRequests
                .Include(l => l.Employee)
                .AsQueryable();

            // Filtrim sipas rolit
            if (currentRole == "Employee")
                query = query.Where(l => l.EmployeeId == currentId);
            else if (currentRole == "Manager")
            {
                var subordinateIds = await _db.Employees
                    .Where(e => e.ManagerId == currentId)
                    .Select(e => e.Id)
                    .ToListAsync();
                subordinateIds.Add(currentId);
                query = query.Where(l => subordinateIds.Contains(l.EmployeeId));
            }

            // Filtra shtesë
            if (!string.IsNullOrEmpty(status))
                query = query.Where(l => l.Status == status);
            if (from.HasValue)
                query = query.Where(l => l.StartDate >= from.Value);
            if (to.HasValue)
                query = query.Where(l => l.EndDate <= to.Value);
            if (employeeId.HasValue && currentRole != "Employee")
                query = query.Where(l => l.EmployeeId == employeeId.Value);

            var total = await query.CountAsync();

            var items = await query
                .OrderByDescending(l => l.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(l => new
                {
                    l.Id,
                    l.EmployeeId,
                    EmployeeName = $"{l.Employee.FirstName} {l.Employee.LastName}",
                    l.Type,
                    l.StartDate,
                    l.EndDate,
                    l.TotalDays,
                    l.Reason,
                    l.Status,
                    l.ApprovedBy,
                    l.CreatedAt
                })
                .ToListAsync();

            return Ok(new { TotalCount = total, Page = page, PageSize = pageSize, Items = items });
        }

        // GET: api/leaverequests/{id} merr nje kerkese specifike
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var currentId = Guid.Parse(User.FindFirstValue("employeeId")!);
            var currentRole = User.FindFirstValue(ClaimTypes.Role);

            var leave = await _db.LeaveRequests
                .Include(l => l.Employee)
                .FirstOrDefaultAsync(l => l.Id == id);

            if (leave == null)
                return NotFound(new { message = "Kërkesa nuk u gjet." });

            if (currentRole == "Employee" && leave.EmployeeId != currentId)
                return Forbid();

            return Ok(leave);
        }

        // POST: api/leaverequests krijon nje kerkese te re leje
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateLeaveRequestDto dto)
        {
            var currentId = Guid.Parse(User.FindFirstValue("employeeId")!);

            // Rregull biznesi: EndDate nuk mund të jetë para StartDate
            if (dto.EndDate < dto.StartDate)
                return BadRequest(new { message = "EndDate nuk mund të jetë para StartDate." });

            // Rregull biznesi: nuk lejohet mbivendosja me leje të aprovuar
            var hasOverlap = await _db.LeaveRequests.AnyAsync(l =>
                l.EmployeeId == currentId &&
                l.Status == "Approved" &&
                l.StartDate <= dto.EndDate &&
                l.EndDate >= dto.StartDate);

            if (hasOverlap)
                return Conflict(new { message = "Ekziston një leje e aprovuar që mbivendoset me këtë periudhë." });

            var leave = new LeaveRequest
            {
                Id = Guid.NewGuid(),
                EmployeeId = currentId,
                Type = dto.Type,
                StartDate = dto.StartDate,
                EndDate = dto.EndDate,
                Reason = dto.Reason,
                TotalDays = (dto.EndDate - dto.StartDate).Days + 1,
                Status = "Pending",
                CreatedAt = DateTime.UtcNow
            };

            _db.LeaveRequests.Add(leave);
            await _db.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = leave.Id }, leave);
        }

        // PUT: api/leaverequests/{id} modifikon nje kerkese ekzistuese
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] CreateLeaveRequestDto dto)
        {
            var currentId = Guid.Parse(User.FindFirstValue("employeeId")!);

            var leave = await _db.LeaveRequests.FindAsync(id);

            if (leave == null)
                return NotFound(new { message = "Kërkesa nuk u gjet." });
            //if (leave.EmployeeId != currentId)
            //    return Forbid();
            if (leave.Status == "Approved")
                return BadRequest(new { message = "Një kërkesë e aprovuar nuk mund të modifikohet." });
            if (dto.EndDate < dto.StartDate)
                return BadRequest(new { message = "EndDate nuk mund të jetë para StartDate." });

            leave.Type = dto.Type;
            leave.StartDate = dto.StartDate;
            leave.EndDate = dto.EndDate;
            leave.Reason = dto.Reason;
            leave.TotalDays = (dto.EndDate - dto.StartDate).Days + 1;

            await _db.SaveChangesAsync();
            return Ok(leave);
        }

        // Put: api/leaverequests/{id}/approve aprovon/refuzon nje kerkese 
        [HttpPut("{id}/approve")]
        [Authorize(Roles = "Manager,Admin")]
        public async Task<IActionResult> ApproveOrReject(Guid id, [FromBody] ApproveRejectDto dto)
        {
            var currentId = Guid.Parse(User.FindFirstValue("employeeId")!);

            var leave = await _db.LeaveRequests.FindAsync(id);

            if (leave == null)
                return NotFound(new { message = "Kërkesa nuk u gjet." });
            if (leave.Status == "Approved")
                return BadRequest(new { message = "Një kërkesë e aprovuar nuk mund të modifikohet." });

            leave.Status = dto.Approve ? "Approved" : "Rejected";
            leave.ApprovedBy = currentId;

            await _db.SaveChangesAsync();
            return Ok(leave);
        }

        // GET: api/leaverequests/check-overlap startdate&enddate
        [HttpGet("check-overlap")]
        public async Task<IActionResult> CheckOverlap(
            [FromQuery] DateTime startDate,
            [FromQuery] DateTime endDate)
        {
            var currentId = Guid.Parse(User.FindFirstValue("employeeId")!);

            if (endDate < startDate)
                return BadRequest(new { message = "EndDate nuk mund të jetë para StartDate." });

            var overlapping = await _db.LeaveRequests
                .Where(l =>
                    l.EmployeeId == currentId &&
                    l.Status == "Approved" &&
                    l.StartDate <= endDate &&
                    l.EndDate >= startDate)
                .Select(l => new
                {
                    l.Id,
                    l.Type,
                    l.StartDate,
                    l.EndDate,
                    l.TotalDays,
                    l.Status
                })
                .ToListAsync();

            if (overlapping.Any())
                return Conflict(new
                {
                    message = "Ekziston një leje e aprovuar që mbivendoset me këtë periudhë.",
                    hasOverlap = true,
                    overlappingLeaves = overlapping
                });

            return Ok(new
            {
                message = "Nuk ka mbivendosje. Mund të aplikoni për leje.",
                hasOverlap = false
            });
        }

        // DELETE: api/leaverequests/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var currentId = Guid.Parse(User.FindFirstValue("employeeId")!);

            var leave = await _db.LeaveRequests.FindAsync(id);

            if (leave == null)
                return NotFound(new { message = "Kërkesa nuk u gjet." });
            if (leave.EmployeeId != currentId)
                return Forbid();
            if (leave.Status != "Pending")
                return BadRequest(new { message = "Vetëm kërkesat Pending mund të fshihen." });

            _db.LeaveRequests.Remove(leave);
            await _db.SaveChangesAsync();
            return NoContent();
        }
    }
}