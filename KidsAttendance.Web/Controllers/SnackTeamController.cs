using KidsAttendance.Infrastructure.Persistence;
using KidsAttendance.Infrastructure.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KidsAttendance.Web.Controllers;

[Authorize(Roles = ApplicationRoles.SnackTeamOrCoordinador)]
public class SnackTeamController : Controller
{
    private readonly KidsAttendanceDbContext _dbContext;

    public SnackTeamController(KidsAttendanceDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet]
    public async Task<IActionResult> Index(DateTime? date)
    {
        var targetDate = date?.Date ?? DateTime.Today;
        var session = await _dbContext.AttendanceSessions.AsNoTracking().FirstOrDefaultAsync(x => x.SessionDate == targetDate);
        if (session is null)
        {
            ViewBag.Items = new List<object>();
            ViewBag.Total = 0;
            return View();
        }

        var counts = await _dbContext.AttendanceRecords
            .AsNoTracking()
            .Where(x => x.AttendanceSessionId == session.Id && x.Status == "CheckedIn")
            .GroupBy(x => x.ClassGroupId)
            .Select(g => new { ClassGroupId = g.Key, Count = g.Count() })
            .ToListAsync();

        var groups = await _dbContext.ClassGroups.AsNoTracking().ToDictionaryAsync(x => x.Id, x => x.Name);
        ViewBag.Items = counts.Select(x => new { Group = groups.GetValueOrDefault(x.ClassGroupId, $"Grupo #{x.ClassGroupId}"), x.Count }).ToList();
        ViewBag.Total = counts.Sum(x => x.Count);
        ViewBag.Date = targetDate;
        return View();
    }

    [HttpGet]
    public async Task<IActionResult> CurrentCountByGroup(DateTime? date)
    {
        var targetDate = date?.Date ?? DateTime.Today;
        var session = await _dbContext.AttendanceSessions.AsNoTracking().FirstOrDefaultAsync(x => x.SessionDate == targetDate);
        if (session is null)
        {
            return Json(new { total = 0, groups = Array.Empty<object>() });
        }

        var counts = await _dbContext.AttendanceRecords
            .AsNoTracking()
            .Where(x => x.AttendanceSessionId == session.Id && x.Status == "CheckedIn")
            .GroupBy(x => x.ClassGroupId)
            .Select(g => new { ClassGroupId = g.Key, Count = g.Count() })
            .ToListAsync();

        var groups = await _dbContext.ClassGroups.AsNoTracking().ToDictionaryAsync(x => x.Id, x => x.Name);
        var groupData = counts.Select(x => new { groupId = x.ClassGroupId, groupName = groups.GetValueOrDefault(x.ClassGroupId, $"Grupo #{x.ClassGroupId}"), count = x.Count }).ToList();
        return Json(new { total = groupData.Sum(x => x.count), groups = groupData });
    }
}
