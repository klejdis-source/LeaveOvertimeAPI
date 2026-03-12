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
    public class OvertimeController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly IEmailService _email;

        public OvertimeController(AppDbContext db, IEmailService email)
        {
            _db = db;
            _email = email;
        }

        // GET: api/overtime
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

            var query = _db.Overtimes
                .Include(o => o.Employee)
                .AsQueryable();

            // Filtrim sipas rolit
            if (currentRole == "Employee")
                query = query.Where(o => o.EmployeeId == currentId);
            else if (currentRole == "Manager")
            {
                var subordinateIds = await _db.Employees
                    .Where(e => e.ManagerId == currentId)
                    .Select(e => e.Id)
                    .ToListAsync();
                subordinateIds.Add(currentId);
                query = query.Where(o => subordinateIds.Contains(o.EmployeeId));
            }

            // Filtra shtesë
            if (!string.IsNullOrEmpty(status))
                query = query.Where(o => o.Status == status);
            if (from.HasValue)
                query = query.Where(o => o.Date >= from.Value);
            if (to.HasValue)
                query = query.Where(o => o.Date <= to.Value);
            if (employeeId.HasValue && currentRole != "Employee")
                query = query.Where(o => o.EmployeeId == employeeId.Value);

            var total = await query.CountAsync();

            var items = await query
                .OrderByDescending(o => o.Date)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(o => new
                {
                    o.Id,
                    o.EmployeeId,
                    EmployeeName = $"{o.Employee.FirstName} {o.Employee.LastName}",
                    o.Date,
                    o.HoursWorked,
                    o.Description,
                    o.Status,
                    o.ApprovedBy,
                    o.CreatedAt
                })
                .ToListAsync();

            return Ok(new { TotalCount = total, Page = page, PageSize = pageSize, Items = items });
        }

        // GET: api/overtime/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var currentId = Guid.Parse(User.FindFirstValue("employeeId")!);
            var currentRole = User.FindFirstValue(ClaimTypes.Role);

            var overtime = await _db.Overtimes
                .Include(o => o.Employee)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (overtime == null)
                return NotFound(new { message = "Regjistrimi nuk u gjet." });

            if (currentRole == "Employee" && overtime.EmployeeId != currentId)
                return Forbid();

            return Ok(overtime);
        }

        // POST: api/overtime
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] OvertimeCreateDto dto)
        {
            var currentId = Guid.Parse(User.FindFirstValue("employeeId")!);

            // Rregull biznesi: maksimumi 12 orë shtesë në ditë
            if (dto.HoursWorked > 12)
                return BadRequest(new { message = "Maksimumi i orëve shtesë në ditë është 12." });

            // Kontrollo orët ekzistuese për të njëjtën ditë
            var existingHours = await _db.Overtimes
                .Where(o => o.EmployeeId == currentId &&
                            o.Date.Date == dto.Date!.Value.Date &&
                            o.Status != "Rejected")
                .SumAsync(o => o.HoursWorked);

            if (existingHours + dto.HoursWorked > 12)
                return BadRequest(new { message = $"Tejkaloni limitin ditor. Orët ekzistuese: {existingHours}h. Maksimumi: 12h." });

            var overtime = new Overtime
            {
                Id = Guid.NewGuid(),
                EmployeeId = currentId,
                Date = dto.Date!.Value,
                HoursWorked = dto.HoursWorked!.Value,
                Description = dto.Description!,
                Status = "Pending",
                CreatedAt = DateTime.UtcNow
            };

            _db.Overtimes.Add(overtime);
            await _db.SaveChangesAsync();

            // Email Notification
            var employee = await _db.Employees.FindAsync(currentId);
            var manager = employee?.ManagerId.HasValue == true
                ? await _db.Employees.FindAsync(employee.ManagerId.Value)
                : null;
            var managerEmail = manager?.Email ?? "manager@gmail.com";
            await _email.SendAsync(
                managerEmail,
                "Ore shtese te regjistruara",
                $"{employee?.FirstName} {employee?.LastName} ka regjistruar ore shtese. Ore: {overtime.HoursWorked}h me date {overtime.Date:dd/MM/yyyy}. Eshte ne pritje te aprovimit.");
            return CreatedAtAction(nameof(GetById), new { id = overtime.Id }, overtime);
        }

        // PATCH: api/overtime/{id}/approve
        [HttpPatch("{id}/approve")]
        [Authorize(Roles = "Manager,Admin")]
        public async Task<IActionResult> ApproveOrReject(Guid id, [FromBody] ApproveRejectDto dto)
        {
            var currentId = Guid.Parse(User.FindFirstValue("employeeId")!);

            var overtime = await _db.Overtimes.FindAsync(id);

            if (overtime == null)
                return NotFound(new { message = "Regjistrimi nuk u gjet." });
            if (overtime.Status == "Approved")
                return BadRequest(new { message = "Orët e aprovuara nuk mund të modifikohen." });

            overtime.Status = dto.Approve ? "Approved" : "Rejected";
            overtime.ApprovedBy = Guid.Parse(currentId.ToString());

            await _db.SaveChangesAsync();
            return Ok(overtime);
        }

        // DELETE: api/overtime/{id}
        // GET: api/overtime/monthly-total
        [HttpGet("monthly-total")]
        public async Task<IActionResult> MonthlyTotal()
        {
            var currentId = Guid.Parse(User.FindFirstValue("employeeId")!);

            var now = DateTime.UtcNow;
            var currentMonth = now.Month;
            var currentYear = now.Year;

            var totalHours = await _db.Overtimes
                .Where(o =>
                    o.EmployeeId == currentId &&
                    o.Status == "Approved" &&
                    o.Date.Month == currentMonth &&
                    o.Date.Year == currentYear)
                .SumAsync(o => o.HoursWorked);

            return Ok(new
            {
                month = currentMonth,
                year = currentYear,
                totalApprovedHours = totalHours
            });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var currentId = Guid.Parse(User.FindFirstValue("employeeId")!);

            var overtime = await _db.Overtimes.FindAsync(id);

            if (overtime == null)
                return NotFound(new { message = "Regjistrimi nuk u gjet." });
            if (overtime.EmployeeId != currentId)
                return Forbid();
            if (overtime.Status != "Pending")
                return BadRequest(new { message = "Vetëm regjistrimet Pending mund të fshihen." });

            _db.Overtimes.Remove(overtime);
            await _db.SaveChangesAsync();
            return NoContent();
        }
    }
}