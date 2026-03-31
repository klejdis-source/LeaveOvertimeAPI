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
[Authorize]
public class EmployeeDocumentsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IFileService _fileService;

    public EmployeeDocumentsController(AppDbContext db, IFileService fileService)
    {
        _db = db;
        _fileService = fileService;
    }

    // POST /api/employees/{id}/documents/upload
    [HttpPost("api/employees/{id}/documents/upload")]
    public async Task<IActionResult> UploadFormFile(Guid id, IFormFile file)
    {
        var currentId = Guid.Parse(User.FindFirstValue("employeeId")!);
        var currentRole = User.FindFirstValue(ClaimTypes.Role);

        if (currentRole == "Employee" && currentId != id)
            return Forbid();

        var employee = await _db.Employees.FindAsync(id);
        if (employee == null)
            return NotFound(new { message = "Punonjesi nuk u gjet." });

        if (file == null || file.Length == 0)
            return BadRequest(new { message = "Nuk u zgjodh asnje file." });

        if (!_fileService.IsValidContentType(file.ContentType))
            return BadRequest(new { message = "Tipi i lejuar: image/png, image/jpeg, application/pdf." });

        if (!_fileService.IsValidSize(file.Length))
            return BadRequest(new { message = "Madhesia maksimale e lejuar eshte 5MB." });

        var docCount = await _db.EmployeeDocuments.CountAsync(d => d.EmployeeId == id);
        if (docCount >= 5)
            return BadRequest(new { message = "Punonjesi ka arritur limitin maksimal prej 5 dokumentesh." });

        var document = await _fileService.ProcessFormFileAsync(id, file);
        _db.EmployeeDocuments.Add(document);

        _db.Auditlogs.Add(new Auditlog
        {
            UserId = currentId,
            Action = "Document uploaded",
            EntityName = "EmployeeDocument",
            EntityId = document.Id.ToString(),
            Date = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();

        return Ok(MapToDto(document));
    }

    // POST /api/employees/{id}/documents/base64
    [HttpPost("api/employees/{id}/documents/base64")]
    public async Task<IActionResult> UploadBase64(Guid id, [FromBody] UploadBase64DocumentDto dto)
    {
        var currentId = Guid.Parse(User.FindFirstValue("employeeId")!);
        var currentRole = User.FindFirstValue(ClaimTypes.Role);

        if (currentRole == "Employee" && currentId != id)
            return Forbid();

        var employee = await _db.Employees.FindAsync(id);
        if (employee == null)
            return NotFound(new { message = "Punonjesi nuk u gjet." });

        if (string.IsNullOrEmpty(dto.Base64Content))
            return BadRequest(new { message = "Base64 content nuk mund te jete bosh." });

        if (!_fileService.IsValidContentType(dto.ContentType))
            return BadRequest(new { message = "Tipi i lejuar: image/png, image/jpeg, application/pdf." });

        var document = await _fileService.ProcessBase64Async(id, dto.Base64Content, dto.FileName, dto.ContentType);

        if (!_fileService.IsValidSize(document.Size))
            return BadRequest(new { message = "Madhesia maksimale e lejuar eshte 5MB." });

        var docCount = await _db.EmployeeDocuments.CountAsync(d => d.EmployeeId == id);
        if (docCount >= 5)
            return BadRequest(new { message = "Punonjesi ka arritur limitin maksimal prej 5 dokumentesh." });

        _db.EmployeeDocuments.Add(document);

        _db.Auditlogs.Add(new Auditlog
        {
            UserId = currentId,
            Action = "Document uploaded via base64",
            EntityName = "EmployeeDocument",
            EntityId = document.Id.ToString(),
            Date = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();

        return Ok(MapToDto(document));
    }

    // GET /api/employees/{id}/documents
    [HttpGet("api/employees/{id}/documents")]
    public async Task<IActionResult> GetDocuments(Guid id)
    {
        var currentId = Guid.Parse(User.FindFirstValue("employeeId")!);
        var currentRole = User.FindFirstValue(ClaimTypes.Role);

        if (currentRole == "Employee" && currentId != id)
            return Forbid();

        var documents = await _db.EmployeeDocuments
            .Where(d => d.EmployeeId == id && !d.IsDeleted)
            .ToListAsync();

        return Ok(documents.Select(d => MapToDto(d)));
    }

    // GET /api/documents/{id}/download
    [HttpGet("api/documents/{id}/download")]
    public async Task<IActionResult> Download(Guid id)
    {
        var currentId = Guid.Parse(User.FindFirstValue("employeeId")!);
        var currentRole = User.FindFirstValue(ClaimTypes.Role);

        var doc = await _db.EmployeeDocuments
            .FirstOrDefaultAsync(d => d.Id == id && !d.IsDeleted);

        if (doc == null)
            return NotFound(new { message = "Dokumenti nuk u gjet." });

        if (currentRole == "Employee" && currentId != doc.EmployeeId)
            return Forbid();

        if (string.IsNullOrEmpty(doc.Base64Content))
            return BadRequest(new { message = "Dokumenti nuk ka permbajtje." });


        var bytes = Convert.FromBase64String(doc.Base64Content);
        return File(bytes, doc.ContentType, doc.FileName);
    }

    private DocumentResponseDto MapToDto(EmployeeDocument d) => new DocumentResponseDto
    {
        Id = d.Id,
        FileName = d.FileName,
        ContentType = d.ContentType,
        Size = d.Size,
        UploadedAt = d.UploadedAt,
        EmployeeId = d.EmployeeId
    };
}