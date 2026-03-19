using ClosedXML.Excel;
using LeaveOvertimeAPI.Data;
using LeaveOvertimeAPI.DTOs;
using Microsoft.EntityFrameworkCore;

namespace LeaveOvertimeAPI.Services;

public class ReportExportService
{
    private readonly AppDbContext _db;

    public ReportExportService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<ExportResult> ExportMonthlyOvertimeAsync(
        DateTime from, DateTime to, Guid? currentUserId = null, string? currentRole = null)
    {
        var query = _db.Overtimes
            .Include(o => o.Employee)
                .ThenInclude(e => e.Department)
            .Where(o => o.Status == "Approved" && o.Date >= from && o.Date <= to)
            .AsQueryable();

        // Nese Manager, shiko vetem stafin e tij
        if (currentRole == "Manager" && currentUserId.HasValue)
        {
            var subordinateIds = await _db.Employees
                .Where(e => e.ManagerId == currentUserId.Value)
                .Select(e => e.Id)
                .ToListAsync();
            query = query.Where(o => subordinateIds.Contains(o.EmployeeId));
        }

        var data = await query.ToListAsync();

        // Grupim sipas punonjësit dhe muajit
        var rows = data
            .GroupBy(o => new { o.EmployeeId, Year = o.Date.Year, Month = o.Date.Month })
            .Select(g => new MonthlyOvertimeReportRowDTO
            {
                EmployeeId = g.Key.EmployeeId,
                FullName = $"{g.First().Employee.FirstName} {g.First().Employee.LastName}",
                Month = $"{g.Key.Year}-{g.Key.Month:00}",
                ApprovedHours = g.Sum(o => o.HoursWorked),
                RequestCount = g.Count(),
                Department = g.First().Employee.Department?.Name
            })
            .OrderBy(r => r.Month)
            .ThenBy(r => r.FullName)
            .ToList();

        // Nderto Excel
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Overtime Report");

        // Rreshti 1 - Titulli
        ws.Cell(1, 1).Value = $"Raport Mujor i Orëve Shtesë - Periudha: {from:dd/MM/yyyy} - {to:dd/MM/yyyy}";
        ws.Cell(1, 1).Style.Font.Bold = true;
        ws.Cell(1, 1).Style.Font.FontSize = 14;
        ws.Range(1, 1, 1, 6).Merge();

        // Rreshti 3 - Header
        ws.Cell(3, 1).Value = "Employee ID";
        ws.Cell(3, 2).Value = "Emri i Plotë";
        ws.Cell(3, 3).Value = "Muaji";
        ws.Cell(3, 4).Value = "Orë të Aprovuara";
        ws.Cell(3, 5).Value = "Nr. Kërkesash";
        ws.Cell(3, 6).Value = "Departamenti";

        var headerRange = ws.Range(3, 1, 3, 6);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.BackgroundColor = XLColor.LightBlue;
        headerRange.SetAutoFilter();

        // Të dhënat
        int row = 4;
        foreach (var r in rows)
        {
            ws.Cell(row, 1).Value = r.EmployeeId.ToString();
            ws.Cell(row, 2).Value = r.FullName;
            ws.Cell(row, 3).Value = r.Month;
            ws.Cell(row, 4).Value = (double)r.ApprovedHours;
            ws.Cell(row, 5).Value = r.RequestCount;
            ws.Cell(row, 6).Value = r.Department ?? "-";
            row++;
        }

        // Rreshti final - Totalet
        ws.Cell(row, 3).Value = "TOTAL";
        ws.Cell(row, 3).Style.Font.Bold = true;
        ws.Cell(row, 4).Value = (double)rows.Sum(r => r.ApprovedHours);
        ws.Cell(row, 4).Style.Font.Bold = true;
        ws.Cell(row, 5).Value = rows.Sum(r => r.RequestCount);
        ws.Cell(row, 5).Style.Font.Bold = true;

        // Formatim
        ws.Columns().AdjustToContents();
        ws.SheetView.FreezeRows(3);

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);

        var fileName = $"OvertimeReport_{from:yyyyMMdd}_{to:yyyyMMdd}.xlsx";
        return new ExportResult(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }
}

public record ExportResult(byte[] Content, string ContentType, string FileName);