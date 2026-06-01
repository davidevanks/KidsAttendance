using KidsAttendance.Infrastructure.Persistence;
using KidsAttendance.Infrastructure.Persistence.Entities;
using KidsAttendance.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KidsAttendance.Web.Controllers;

[Authorize(Roles = "Admin")]
public class TeacherAssignmentsController : Controller
{
    private readonly KidsAttendanceDbContext _dbContext;
    private readonly UserManager<AppUser> _userManager;

    public TeacherAssignmentsController(KidsAttendanceDbContext dbContext, UserManager<AppUser> userManager)
    {
        _dbContext = dbContext;
        _userManager = userManager;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        return await BuildIndexViewAsync(new TeacherAssignmentViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Assign(TeacherAssignmentViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return await BuildIndexViewAsync(model);
        }

        var alreadyActive = await _dbContext.TeacherClassGroups.AnyAsync(x =>
            x.TeacherUserId == model.TeacherUserId &&
            x.ClassGroupId == model.ClassGroupId &&
            x.IsActive);

        if (alreadyActive)
        {
            TempData["ErrorMessage"] = "Ese maestro ya está asignado al grupo.";
            return RedirectToAction(nameof(Index));
        }

        var currentActiveAssignments = await _dbContext.TeacherClassGroups
            .Where(x => x.TeacherUserId == model.TeacherUserId && x.IsActive)
            .ToListAsync();
        foreach (var assignment in currentActiveAssignments)
        {
            assignment.IsActive = false;
        }

        var userId = _userManager.GetUserId(User);
        _dbContext.TeacherClassGroups.Add(new TeacherClassGroup
        {
            TeacherUserId = model.TeacherUserId,
            ClassGroupId = model.ClassGroupId,
            AssignedByUserId = userId,
            AssignedAt = DateTime.UtcNow,
            IsActive = true
        });

        await _dbContext.SaveChangesAsync();
        TempData["SuccessMessage"] = "Asignación registrada.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Deactivate(int id)
    {
        var assignment = await _dbContext.TeacherClassGroups.FirstOrDefaultAsync(x => x.Id == id);
        if (assignment is null)
        {
            TempData["ErrorMessage"] = "Asignación no encontrada.";
            return RedirectToAction(nameof(Index));
        }

        assignment.IsActive = false;
        await _dbContext.SaveChangesAsync();
        TempData["SuccessMessage"] = "Asignación desactivada.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<IActionResult> BuildIndexViewAsync(TeacherAssignmentViewModel model)
    {
        var teachers = await _userManager.GetUsersInRoleAsync("Teacher");
        var groups = await _dbContext.ClassGroups.AsNoTracking().OrderBy(x => x.MinAge).ToListAsync();
        var assignments = await _dbContext.TeacherClassGroups.AsNoTracking().Where(x => x.IsActive).ToListAsync();

        var assignedTeacherIds = assignments.Select(a => a.TeacherUserId).ToHashSet();
        var availableTeachers = teachers.Where(t => !assignedTeacherIds.Contains(t.Id)).ToList();

        ViewBag.AllTeachers = teachers;
        ViewBag.AvailableTeachers = availableTeachers;
        ViewBag.Groups = groups;
        ViewBag.Assignments = assignments;
        return View("Index", model);
    }
}
