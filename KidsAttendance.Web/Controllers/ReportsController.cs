using ClosedXML.Excel;
using KidsAttendance.Infrastructure.Persistence;
using KidsAttendance.Infrastructure.Security;
using KidsAttendance.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KidsAttendance.Web.Controllers;

[Authorize(Roles = ApplicationRoles.Coordinador)]
public class ReportsController : Controller
{
    private readonly KidsAttendanceDbContext _dbContext;

    public ReportsController(KidsAttendanceDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet]
    public async Task<IActionResult> AttendanceByDay(DateTime? date)
    {
        var targetDate = date?.Date ?? DateTime.Today;
        var result = await BuildSummaryAsync(targetDate);
        ViewBag.Result = result;
        return View(new AttendanceReportFilterViewModel { Date = targetDate });
    }

    [HttpGet]
    public async Task<IActionResult> DownloadAttendanceExcel(DateTime date)
    {
        var targetDate = date.Date;
        var summary = await BuildSummaryAsync(targetDate);
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Asistencia");
        ws.Cell("A1").Value = $"Asistencia por grupo - {targetDate:dd/MM/yyyy}";
        ws.Range("A1:B1").Merge().Style.Font.Bold = true;
        ws.Cell("A3").Value = "Grupo";
        ws.Cell("B3").Value = "Cantidad de niños";
        ws.Range("A3:B3").Style.Font.Bold = true;

        var row = 4;
        foreach (var item in summary)
        {
            ws.Cell(row, 1).Value = item.GroupName;
            ws.Cell(row, 2).Value = item.Count;
            row++;
        }

        ws.Cell(row, 1).Value = "Total general";
        ws.Cell(row, 2).Value = summary.Sum(x => x.Count);
        ws.Range(row, 1, row, 2).Style.Font.Bold = true;
        ws.Range(3, 1, row, 2).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        ws.Range(3, 1, row, 2).Style.Border.InsideBorder = XLBorderStyleValues.Thin;
        ws.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        stream.Position = 0;
        return File(stream.ToArray(),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"Asistencia_{targetDate:yyyyMMdd}.xlsx");
    }

    private async Task<List<AttendanceReportResultViewModel>> BuildSummaryAsync(DateTime date)
    {
        var session = await _dbContext.AttendanceSessions.AsNoTracking().FirstOrDefaultAsync(x => x.SessionDate == date);
        if (session is null) return new List<AttendanceReportResultViewModel>();

        var groups = await _dbContext.ClassGroups.AsNoTracking().ToDictionaryAsync(x => x.Id, x => x.Name);
        var data = await _dbContext.AttendanceRecords.AsNoTracking()
            .Where(x => x.AttendanceSessionId == session.Id)
            .GroupBy(x => x.ClassGroupId)
            .Select(g => new { ClassGroupId = g.Key, Count = g.Count() })
            .ToListAsync();

        return data.Select(x => new AttendanceReportResultViewModel
        {
            GroupName = groups.GetValueOrDefault(x.ClassGroupId, $"Grupo #{x.ClassGroupId}"),
            Count = x.Count
        }).OrderBy(x => x.GroupName).ToList();
    }
}
