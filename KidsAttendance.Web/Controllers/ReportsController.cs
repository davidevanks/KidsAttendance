using ClosedXML.Excel;
using KidsAttendance.Infrastructure.Persistence;
using KidsAttendance.Infrastructure.Persistence.Entities;
using KidsAttendance.Infrastructure.Security;
using KidsAttendance.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KidsAttendance.Web.Controllers;

[Authorize]
public class ReportsController : Controller
{
    private readonly KidsAttendanceDbContext _dbContext;
    private readonly UserManager<AppUser> _userManager;

    public ReportsController(KidsAttendanceDbContext dbContext, UserManager<AppUser> userManager)
    {
        _dbContext = dbContext;
        _userManager = userManager;
    }

    [HttpGet]
    [Authorize(Roles = ApplicationRoles.Coordinador)]
    public async Task<IActionResult> AttendanceByDay(DateTime? date)
    {
        var targetDate = date?.Date ?? DateTime.Today;
        var result = await BuildSummaryAsync(targetDate);
        ViewBag.Result = result;
        return View(new AttendanceReportFilterViewModel { Date = targetDate });
    }

    [HttpGet]
    [Authorize(Roles = ApplicationRoles.Coordinador)]
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

    /// <summary>
    /// Shows the current teacher a month-by-month attendance matrix for their active group.
    /// </summary>
    [HttpGet]
    [Authorize(Roles = ApplicationRoles.Teacher)]
    public async Task<IActionResult> MonthlyGroupAttendance(int? year, int? month)
    {
        var today = DateTime.Today;
        var selectedYear = year is >= 1 and <= 9999 ? year.Value : today.Year;
        var selectedMonth = month is >= 1 and <= 12 ? month.Value : today.Month;
        var monthStart = new DateTime(selectedYear, selectedMonth, 1);
        var nextMonthStart = monthStart.AddMonths(1);

        var model = new MonthlyAttendanceReportViewModel
        {
            Year = selectedYear,
            Month = selectedMonth
        };

        var teacherGroup = await GetTeacherActiveGroupAsync();
        if (teacherGroup is null)
        {
            return View(model);
        }

        model.HasAssignedGroup = true;
        model.GroupName = teacherGroup.Name;

        var children = await _dbContext.Children.AsNoTracking()
            .Where(x => x.IsActive && x.CurrentClassGroupId == teacherGroup.Id)
            .OrderBy(x => x.FullName)
            .Select(x => new { x.Id, x.FullName })
            .ToListAsync();

        var sessions = await _dbContext.AttendanceSessions.AsNoTracking()
            .Where(x => x.SessionDate >= monthStart && x.SessionDate < nextMonthStart)
            .OrderBy(x => x.SessionDate)
            .Select(x => new { x.Id, SessionDate = x.SessionDate.Date })
            .ToListAsync();

        model.AttendanceDates = sessions.Select(x => x.SessionDate).ToList();

        if (children.Count == 0 || sessions.Count == 0)
        {
            model.Rows = children.Select(x => new MonthlyAttendanceChildRowViewModel
            {
                ChildName = x.FullName
            }).ToList();

            return View(model);
        }

        var sessionIds = sessions.Select(x => x.Id).ToList();
        var sessionDatesById = sessions.ToDictionary(x => x.Id, x => x.SessionDate);
        var attendanceRecords = await _dbContext.AttendanceRecords.AsNoTracking()
            .Where(x => x.ClassGroupId == teacherGroup.Id && sessionIds.Contains(x.AttendanceSessionId))
            .Select(x => new { x.ChildId, x.AttendanceSessionId })
            .Distinct()
            .ToListAsync();

        var attendanceKeys = attendanceRecords
            .Select(x => (x.ChildId, Date: sessionDatesById[x.AttendanceSessionId]))
            .ToHashSet();

        model.Rows = children
            .Select(child =>
            {
                var attendedDates = model.AttendanceDates
                    .Where(date => attendanceKeys.Contains((child.Id, date)))
                    .ToHashSet();

                return new MonthlyAttendanceChildRowViewModel
                {
                    ChildName = child.FullName,
                    AttendedDates = attendedDates,
                    TotalAttendance = attendedDates.Count
                };
            })
            .OrderByDescending(x => x.TotalAttendance)
            .ThenBy(x => x.ChildName)
            .ToList();

        return View(model);
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

    private async Task<ClassGroup?> GetTeacherActiveGroupAsync()
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return null;
        }

        return await _dbContext.TeacherClassGroups.AsNoTracking()
            .Where(x => x.TeacherUserId == userId && x.IsActive)
            .Join(_dbContext.ClassGroups.AsNoTracking().Where(x => x.IsActive),
                teacherClassGroup => teacherClassGroup.ClassGroupId,
                classGroup => classGroup.Id,
                (_, classGroup) => classGroup)
            .FirstOrDefaultAsync();
    }
}
