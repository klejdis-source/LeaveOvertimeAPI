using LeaveOvertimeAPI.Data;
using LeaveOvertimeAPI.DTOs;
using LeaveOvertimeAPI.Models;
using LeaveOvertimeAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace LeaveOvertimeAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class EmployeesController : ControllerBase
{
    private readonly AppDbContext _db;

    public EmployeesController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<ActionResult<PagedResult<EmployeeResponseDto>>> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] EmployeeStatus? status = null,
        [FromQuery] Roles? role = null)
    {
        var query = _db.Employees.AsQueryable();

        if (status.HasValue) query = query.Where(e => e.Status == status.Value);
        if (role.HasValue) query = query.Where(e => e.Roles == role.Value);

        var total = await query.CountAsync();

        var items = await query
            .OrderBy(e => e.LastName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(e => ToDto(e))
            .ToListAsync();

        return Ok(new PagedResult<EmployeeResponseDto>(items, total, page, pageSize));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<EmployeeResponseDto>> GetById(Guid id)
    {
        var currentId = GetCurrentEmployeeId();
        var currentRole = GetCurrentRole();

        if (currentRole == "Employee" && currentId != id)
            return Forbid();

        var employee = await _db.Employees.FindAsync(id);
        if (employee is null) return NotFound(new { message = "Punonjesi nuk u gjet." });

        return Ok(ToDto(employee));
    }

    // GET: api/employees/{id}/leave-balance
    [HttpGet("{id}/leave-balance")]
    public async Task<IActionResult> GetLeaveBalance(Guid id)
    {
        var currentId = GetCurrentEmployeeId();
        var currentRole = GetCurrentRole();

        if (currentRole == "Employee" && currentId != id)
            return Forbid();

        var employee = await _db.Employees.FindAsync(id);
        if (employee is null) return NotFound(new { message = "Punonjesi nuk u gjet." });

        return Ok(new LeaveBalanceDto
        {
            EmployeeId = employee.Id,
            EmployeeName = $"{employee.FirstName} {employee.LastName}",
            TotalDaysPerYear = employee.VacationDaysPerYear,
            UsedDays = employee.UsedVacationDays,
            RemainingDays = employee.VacationDaysPerYear - employee.UsedVacationDays
        });
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<EmployeeResponseDto>> Create([FromBody] EmployeeCreateDto dto)
    {
        if (await _db.Employees.AnyAsync(e => e.Email == dto.Email))
            return Conflict(new { message = "Email ekziston tashmë." });

        var employee = new Employee
        {
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            Email = dto.Email,
            Password = dto.Password,
            Position = dto.Position,
            Salary = dto.Salary,
            HireDate = dto.HireDate,
            Roles = dto.Role,
            ManagerId = dto.ManagerId,
            DepartmentId = dto.DepartmentId,
            Status = EmployeeStatus.Active
        };

        _db.Employees.Add(employee);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = employee.Id }, ToDto(employee));
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<EmployeeResponseDto>> Update(Guid id, [FromBody] UpdateEmployeeDto dto)
    {
        var employee = await _db.Employees.FindAsync(id);
        if (employee is null) return NotFound(new { message = "Punonjesi nuk u gjet." });

        if (dto.FirstName is not null) employee.FirstName = dto.FirstName;
        if (dto.LastName is not null) employee.LastName = dto.LastName;
        if (dto.Position is not null) employee.Position = dto.Position;
        if (dto.Salary.HasValue) employee.Salary = dto.Salary.Value;
        if (dto.Status.HasValue) employee.Status = dto.Status.Value;
        if (dto.Roles.HasValue) employee.Roles = dto.Roles.Value;
        if (dto.ManagerId.HasValue) employee.ManagerId = dto.ManagerId.Value;
        if (dto.DepartmentId.HasValue) employee.DepartmentId = dto.DepartmentId.Value;

        await _db.SaveChangesAsync();
        return Ok(ToDto(employee));
    }

    // DELETE - Soft Delete
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var employee = await _db.Employees.FindAsync(id);
        if (employee is null) return NotFound(new { message = "Punonjesi nuk u gjet." });

        employee.IsDeleted = true;
        employee.DeletedAt = DateTime.UtcNow;
        employee.Status = EmployeeStatus.Inactive;


        //  departmenti  mbetet pa manager deri ne caktimin e nje te riu
        var dept = await _db.Departments.FirstOrDefaultAsync(d => d.ManagerId == id);
        if (dept != null)
            dept.ManagerId = null;

        await _db.SaveChangesAsync();
        return NoContent();
    }

    private Guid GetCurrentEmployeeId() =>
        Guid.Parse(User.FindFirstValue("employeeId")!);

    private string GetCurrentRole() =>
        User.FindFirstValue(ClaimTypes.Role)!;

    private static EmployeeResponseDto ToDto(Employee e) => new EmployeeResponseDto
    {
        Id = e.Id,
        FirstName = e.FirstName,
        LastName = e.LastName,
        Email = e.Email,
        Position = e.Position,
        Salary = e.Salary,
        HireDate = e.HireDate,
        Status = e.Status,
        Role = e.Roles,
        ManagerId = e.ManagerId,
        DepartmentId = e.DepartmentId
    };
}