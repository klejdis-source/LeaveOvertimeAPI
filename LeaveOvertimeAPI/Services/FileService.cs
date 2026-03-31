using DocumentFormat.OpenXml.Presentation;
using LeaveOvertimeAPI.Models;
using System.Buffers.Text;

namespace LeaveOvertimeAPI.Services;

public class FileService : IFileService
{
    private static readonly string[] AllowedTypes = { "image/png", "image/jpeg", "application/pdf" };
    private const long MaxSizeBytes = 5 * 1024 * 1024;  // 5MB
      
    public async Task<EmployeeDocument> ProcessFormFileAsync(Guid employeeId, IFormFile file)
    {
        using var memoryStream = new MemoryStream();
        await file.CopyToAsync(memoryStream);
        var base64 = Convert.ToBase64String(memoryStream.ToArray());

        return new EmployeeDocument
        {
            Id = Guid.NewGuid(),
            EmployeeId = employeeId,
            FileName = file.FileName,
            ContentType = file.ContentType,
            Base64Content = base64,
            Size = file.Length,
            UploadedAt = DateTime.UtcNow
        };

    }
   
    public Task<EmployeeDocument> ProcessBase64Async(Guid employeeId, string base64Content, string fileName, string contentType )
    {
        // Hiq prefix nese ekziston 
        var cleanBase64 = base64Content.Contains(",")
            ? base64Content.Split(",")[1]
            : base64Content;

        var bytes = Convert.FromBase64String(cleanBase64);

        var doc = new EmployeeDocument
        {
            Id = Guid.NewGuid(),
            EmployeeId = employeeId,
            FileName = fileName,
            ContentType = contentType,
            Base64Content = cleanBase64,
            Size = bytes.Length,
            UploadedAt = DateTime.UtcNow
        };
        return Task.FromResult(doc);
    }
    public bool IsValidContentType(string contentType) => AllowedTypes.Contains(contentType.ToLower());

    public bool IsValidSize(long sizeInBytes) => sizeInBytes <= MaxSizeBytes;

}
