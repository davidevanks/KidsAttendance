using KidsAttendance.Infrastructure.Persistence;
using KidsAttendance.Infrastructure.Persistence.Entities;
using KidsAttendance.Infrastructure.Security;
using KidsAttendance.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KidsAttendance.Web.Controllers;

[Authorize(Roles = ApplicationRoles.CoordinadorOrTeacher)]
public class GuardiansController : Controller
{
    private readonly KidsAttendanceDbContext _dbContext;
    private readonly UserManager<AppUser> _userManager;

    public GuardiansController(KidsAttendanceDbContext dbContext, UserManager<AppUser> userManager)
    {
        _dbContext = dbContext;
        _userManager = userManager;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var query = _dbContext.Guardians.AsNoTracking().AsQueryable();

        if (User.IsInRole(ApplicationRoles.Teacher))
        {
            var allowedGroupIds = await GetAssignedGroupIdsAsync();
            if (allowedGroupIds.Count == 0)
            {
                ViewBag.TeacherNoGroup = true;
                return View(new List<GuardianIndexViewModel>());
            }

            var allowedGuardianIdsQuery = _dbContext.ChildGuardians.AsNoTracking()
                .Join(_dbContext.Children.AsNoTracking().Where(c => c.IsActive && allowedGroupIds.Contains(c.CurrentClassGroupId)),
                    cg => cg.ChildId, c => c.Id, (cg, c) => cg.GuardianId)
                .Distinct();

            query = query.Where(g => allowedGuardianIdsQuery.Contains(g.Id));
        }

        var guardians = await query
            .OrderBy(x => x.FullName)
            .Select(x => new GuardianIndexViewModel
            {
                Id = x.Id,
                FullName = x.FullName,
                PhoneNumber = x.PhoneNumber,
                SecondaryPhoneNumber = x.SecondaryPhoneNumber,
                IsActive = x.IsActive
            })
            .ToListAsync();

        var guardianIds = guardians.Select(x => x.Id).ToList();
        var childrenByGuardian = await _dbContext.ChildGuardians.AsNoTracking()
            .Where(x => guardianIds.Contains(x.GuardianId))
            .Join(
                _dbContext.Children.AsNoTracking(),
                childGuardian => childGuardian.ChildId,
                child => child.Id,
                (childGuardian, child) => new
                {
                    childGuardian.GuardianId,
                    ChildName = child.FullName
                })
            .ToListAsync();

        var childrenTextByGuardian = childrenByGuardian
            .GroupBy(x => x.GuardianId)
            .ToDictionary(
                group => group.Key,
                group => string.Join(", ", group
                    .Select(x => x.ChildName)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(name => name)));

        foreach (var guardian in guardians)
        {
            guardian.ChildrenText = childrenTextByGuardian.GetValueOrDefault(guardian.Id) ?? "-";
        }

        return View(guardians);
    }

    [HttpGet]
    public async Task<IActionResult> SearchByPhone(string term)
    {
        if (string.IsNullOrWhiteSpace(term))
        {
            return Json(Array.Empty<object>());
        }

        var query = _dbContext.Guardians.AsNoTracking().Where(x => x.IsActive);
        if (User.IsInRole(ApplicationRoles.Teacher))
        {
            var allowedGroupIds = await GetAssignedGroupIdsAsync();
            if (allowedGroupIds.Count == 0)
            {
                return Json(Array.Empty<object>());
            }

            var allowedGuardianIdsQuery = _dbContext.ChildGuardians.AsNoTracking()
                .Join(_dbContext.Children.AsNoTracking().Where(c => c.IsActive && allowedGroupIds.Contains(c.CurrentClassGroupId)),
                    cg => cg.ChildId, c => c.Id, (cg, c) => cg.GuardianId)
                .Distinct();

            query = query.Where(g => allowedGuardianIdsQuery.Contains(g.Id));
        }

        var normalized = term.Trim();
        var data = await query
            .Where(x => x.FullName.Contains(normalized) || x.PhoneNumber.Contains(normalized))
            .OrderBy(x => x.FullName)
            .Take(20)
            .Select(x => new { x.Id, x.FullName, x.PhoneNumber })
            .ToListAsync();

        return Json(data);
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View(new GuardianCreateViewModel { IsActive = true });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(GuardianCreateViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var exists = await _dbContext.Guardians.AnyAsync(x => x.PhoneNumber == model.PhoneNumber.Trim());
        if (exists)
        {
            ModelState.AddModelError(nameof(model.PhoneNumber), "Ya existe un padre con ese celular.");
            return View(model);
        }

        _dbContext.Guardians.Add(new Guardian
        {
            FullName = model.FullName.Trim(),
            PhoneNumber = model.PhoneNumber.Trim(),
            SecondaryPhoneNumber = model.SecondaryPhoneNumber?.Trim(),
            IsActive = model.IsActive,
            CreatedAt = DateTime.UtcNow
        });

        await _dbContext.SaveChangesAsync();
        TempData["SuccessMessage"] = "Padre creado correctamente.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var guardian = await _dbContext.Guardians.FindAsync(id);
        if (guardian is null) return NotFound();

        return View(new GuardianCreateViewModel
        {
            Id = guardian.Id,
            FullName = guardian.FullName,
            PhoneNumber = guardian.PhoneNumber,
            SecondaryPhoneNumber = guardian.SecondaryPhoneNumber,
            IsActive = guardian.IsActive
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(GuardianCreateViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var guardian = await _dbContext.Guardians.FindAsync(model.Id);
        if (guardian is null) return NotFound();

        var duplicated = await _dbContext.Guardians.AnyAsync(x => x.Id != model.Id && x.PhoneNumber == model.PhoneNumber.Trim());
        if (duplicated)
        {
            ModelState.AddModelError(nameof(model.PhoneNumber), "Ya existe un padre con ese celular.");
            return View(model);
        }

        guardian.FullName = model.FullName.Trim();
        guardian.PhoneNumber = model.PhoneNumber.Trim();
        guardian.SecondaryPhoneNumber = model.SecondaryPhoneNumber?.Trim();
        guardian.IsActive = model.IsActive;
        guardian.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();
        TempData["SuccessMessage"] = "Padre actualizado.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<HashSet<int>> GetAssignedGroupIdsAsync()
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return new HashSet<int>();
        }

        return await _dbContext.TeacherClassGroups.AsNoTracking()
            .Where(x => x.TeacherUserId == userId && x.IsActive)
            .Select(x => x.ClassGroupId)
            .ToHashSetAsync();
    }
}
