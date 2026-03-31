using LeaveOvertimeAPI.Models;
using Microsoft.AspNetCore.Http;

namespace LeaveOvertimeAPI.Services;

public interface IFileService
{
    Task<EmployeeDocument> ProcessFormFileAsync(Guid employeeId, IFormFile file);

    Task<EmployeeDocument> ProcessBase64Async(Guid employeeId, string base64Content, string filename, string contentType);

    bool IsValidContentType(string contentType);
    bool IsValidSize(long sizeInBytes);
}
