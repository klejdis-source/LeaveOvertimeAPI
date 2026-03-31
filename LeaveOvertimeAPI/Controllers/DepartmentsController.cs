using LeaveOvertimeAPI.Data;
using LeaveOvertimeAPI.DTOs;
using LeaveOvertimeAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace LeaveOvertimeAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DepartmentsController : ControllerBase
{
    private readonly AppDbContext _db;

    public DepartmentsController(AppDbContext db)
    {
        _db = db;
    }

    // POST: api/departments
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create([FromBody] CreateDepartmentDto dto)
    {
        if (dto.ManagerId.HasValue)
        {
            var alreadyManages = await _db.Departments
                .AnyAsync(d => d.ManagerId == dto.ManagerId && !d.IsDeleted);
            if (alreadyManages)
                return Conflict(new { message = "Ky manager menaxhon tashme nje department tjeter." });
        }

        var dept = new Department
        {
            Id = Guid.NewGuid(),
            Name = dto.Name,
            Description = dto.Description,
            ManagerId = dto.ManagerId
        };

        _db.Departments.Add(dept);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = dept.Id }, new DepartmentResponseDto
        {
            Id = dept.Id,
            Name = dept.Name,
            Description = dept.Description,
            ManagerId = dept.ManagerId
        });
    }

    // GET: api/departments
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var departments = await _db.Departments
            .Where(d => !d.IsDeleted)
            .Select(d => new DepartmentResponseDto
            {
                Id = d.Id,
                Name = d.Name,
                Description = d.Description,
                ManagerId = d.ManagerId,
                ManagerName = _db.Employees
                    .Where(e => e.Id == d.ManagerId)
                    .Select(e => e.FirstName + " " + e.LastName)
                    .FirstOrDefault()
            })
            .ToListAsync();

        return Ok(departments);
    }

    // GET: api/departments/{id}
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var dept = await _db.Departments
            .Where(d => d.Id == id && !d.IsDeleted)
            .Select(d => new DepartmentResponseDto
            {
                Id = d.Id,
                Name = d.Name,
                Description = d.Description,
                ManagerId = d.ManagerId,
                ManagerName = _db.Employees
                    .Where(e => e.Id == d.ManagerId)
                    .Select(e => e.FirstName + " " + e.LastName)
                    .FirstOrDefault()
            })
            .FirstOrDefaultAsync();

        if (dept == null)
            return NotFound(new { message = "Departmenti nuk u gjet." });

        return Ok(dept);
    }

    // PUT: api/departments/{id}
    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(Guid id, [FromBody] CreateDepartmentDto dto)
    {
        var dept = await _db.Departments.FirstOrDefaultAsync(d => d.Id == id && !d.IsDeleted);
        if (dept == null)
            return NotFound(new { message = "Departmenti nuk u gjet." });

        if (dto.ManagerId.HasValue)
        {
            var alreadyManages = await _db.Departments
                .AnyAsync(d => d.ManagerId == dto.ManagerId && d.Id != id && !d.IsDeleted);
            if (alreadyManages)
                return Conflict(new { message = "Ky manager menaxhon tashme nje department tjeter." });
        }

        dept.Name = dto.Name;
        dept.Description = dto.Description;
        dept.ManagerId = dto.ManagerId;

        await _db.SaveChangesAsync();

        return Ok(new DepartmentResponseDto
        {
            Id = dept.Id,
            Name = dept.Name,
            Description = dept.Description,
            ManagerId = dept.ManagerId
        });
    }

    // DELETE: api/departments/{id} - Soft Delete
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var dept = await _db.Departments.FirstOrDefaultAsync(d => d.Id == id && !d.IsDeleted);
        if (dept == null)
            return NotFound(new { message = "Departmenti nuk u gjet." });

        dept.IsDeleted = true;
        dept.DeletedAt = DateTime.UtcNow;
        dept.ManagerId = null;

        await _db.SaveChangesAsync();
        return NoContent();
    }

    // BONUS - GET: api/departments/{id}/employees
    [HttpGet("{id}/employees")]
    public async Task<IActionResult> GetEmployees(Guid id)
    {
        var deptExists = await _db.Departments.AnyAsync(d => d.Id == id && !d.IsDeleted);
        if (!deptExists)
            return NotFound(new { message = "Departmenti nuk u gjet." });

        var employees = await _db.Employees
            .Where(e => e.DepartmentId == id && !e.IsDeleted)
            .Select(e => new
            {
                e.Id,
                e.FirstName,
                e.LastName,
                e.Email,
                e.Position,
                e.Roles
            })
            .ToListAsync();

        return Ok(employees);
    }
}