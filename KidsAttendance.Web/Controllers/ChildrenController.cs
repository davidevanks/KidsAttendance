using KidsAttendance.Infrastructure.Persistence;
using KidsAttendance.Infrastructure.Persistence.Entities;
using KidsAttendance.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace KidsAttendance.Web.Controllers;

[Authorize(Roles = "Admin,Teacher")]
public class ChildrenController : Controller
{
    private readonly KidsAttendanceDbContext _dbContext;
    private readonly UserManager<AppUser> _userManager;

    public ChildrenController(KidsAttendanceDbContext dbContext, UserManager<AppUser> userManager)
    {
        _dbContext = dbContext;
        _userManager = userManager;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var query = _dbContext.Children.AsNoTracking();
        if (User.IsInRole("Teacher"))
        {
            var allowedGroups = await GetAssignedGroupIdsAsync();
            query = query.Where(x => allowedGroups.Contains(x.CurrentClassGroupId));
        }

        var children = await query.OrderBy(x => x.FullName).ToListAsync();
        var groups = await _dbContext.ClassGroups.AsNoTracking().ToDictionaryAsync(x => x.Id, x => x.Name);
        ViewBag.Groups = groups;
        return View(children);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        await LoadLookupsAsync();
        return View(new ChildCreateViewModel { IsActive = true });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ChildCreateViewModel model)
    {
        await LoadLookupsAsync();
        if (!ModelState.IsValid) return View(model);

        if (User.IsInRole("Teacher"))
        {
            var allowedGroups = await GetAssignedGroupIdsAsync();
            if (!allowedGroups.Contains(model.CurrentClassGroupId))
            {
                return Forbid();
            }
        }

        var child = new Child
        {
            FullName = model.FullName.Trim(),
            BirthDate = model.BirthDate,
            Age = model.Age,
            CurrentClassGroupId = model.CurrentClassGroupId,
            IsActive = model.IsActive,
            CreatedAt = DateTime.UtcNow
        };
        _dbContext.Children.Add(child);
        await _dbContext.SaveChangesAsync();

        await SyncGuardiansAsync(child.Id, model.SelectedGuardianIds);
        TempData["SuccessMessage"] = "Niño registrado.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var child = await _dbContext.Children.FindAsync(id);
        if (child is null) return NotFound();
        if (User.IsInRole("Teacher"))
        {
            var allowedGroups = await GetAssignedGroupIdsAsync();
            if (!allowedGroups.Contains(child.CurrentClassGroupId))
            {
                return Forbid();
            }
        }
        var selectedGuardians = await _dbContext.ChildGuardians
            .Where(x => x.ChildId == id)
            .Select(x => x.GuardianId)
            .ToListAsync();
        await LoadLookupsAsync(selectedGuardians);

        return View(new ChildCreateViewModel
        {
            Id = child.Id,
            FullName = child.FullName,
            BirthDate = child.BirthDate,
            Age = child.Age,
            CurrentClassGroupId = child.CurrentClassGroupId,
            IsActive = child.IsActive,
            SelectedGuardianIds = selectedGuardians
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(ChildCreateViewModel model)
    {
        await LoadLookupsAsync();
        if (!ModelState.IsValid) return View(model);

        var child = await _dbContext.Children.FirstOrDefaultAsync(x => x.Id == model.Id);
        if (child is null) return NotFound();
        if (User.IsInRole("Teacher"))
        {
            var allowedGroups = await GetAssignedGroupIdsAsync();
            if (!allowedGroups.Contains(child.CurrentClassGroupId) || !allowedGroups.Contains(model.CurrentClassGroupId))
            {
                return Forbid();
            }
        }

        var previousGroupId = child.CurrentClassGroupId;
        child.FullName = model.FullName.Trim();
        child.BirthDate = model.BirthDate;
        child.Age = model.Age;
        child.IsActive = model.IsActive;
        child.UpdatedAt = DateTime.UtcNow;

        if (previousGroupId != model.CurrentClassGroupId)
        {
            child.CurrentClassGroupId = model.CurrentClassGroupId;
            var userId = _userManager.GetUserId(User) ?? string.Empty;
            _dbContext.ChildGroupHistories.Add(new ChildGroupHistory
            {
                ChildId = child.Id,
                FromClassGroupId = previousGroupId,
                ToClassGroupId = model.CurrentClassGroupId,
                ChangedByUserId = userId,
                ChangedAt = DateTime.UtcNow,
                Reason = "Cambio manual"
            });
        }

        await _dbContext.SaveChangesAsync();
        await SyncGuardiansAsync(child.Id, model.SelectedGuardianIds);

        TempData["SuccessMessage"] = "Niño actualizado.";
        return RedirectToAction(nameof(Index));
    }

    private async Task LoadLookupsAsync(IEnumerable<int>? selectedGuardianIds = null)
    {
        var groupsQuery = _dbContext.ClassGroups.AsNoTracking().Where(x => x.IsActive);
        if (User.IsInRole("Teacher"))
        {
            var allowedGroups = await GetAssignedGroupIdsAsync();
            groupsQuery = groupsQuery.Where(x => allowedGroups.Contains(x.Id));
        }

        var groups = await groupsQuery.OrderBy(x => x.MinAge).ToListAsync();
        var selectedIds = selectedGuardianIds?.Distinct().ToList() ?? new List<int>();
        var guardiansQuery = _dbContext.Guardians.AsNoTracking().Where(x => x.IsActive || selectedIds.Contains(x.Id));
        var guardians = await guardiansQuery.OrderBy(x => x.FullName).ToListAsync();
        ViewBag.ClassGroups = new SelectList(groups, "Id", "Name");
        ViewBag.Guardians = guardians;
    }

    private async Task SyncGuardiansAsync(int childId, IEnumerable<int> guardianIds)
    {
        var selected = guardianIds.Distinct().ToHashSet();
        var existing = await _dbContext.ChildGuardians.Where(x => x.ChildId == childId).ToListAsync();

        var toRemove = existing.Where(x => !selected.Contains(x.GuardianId)).ToList();
        if (toRemove.Count > 0)
        {
            _dbContext.ChildGuardians.RemoveRange(toRemove);
        }

        var existingIds = existing.Select(x => x.GuardianId).ToHashSet();
        foreach (var guardianId in selected.Where(x => !existingIds.Contains(x)))
        {
            _dbContext.ChildGuardians.Add(new ChildGuardian
            {
                ChildId = childId,
                GuardianId = guardianId,
                Relationship = "Tutor",
                IsPrimary = false,
                IsAuthorizedPickup = true,
                CreatedAt = DateTime.UtcNow
            });
        }

        await _dbContext.SaveChangesAsync();
    }

    private async Task<HashSet<int>> GetAssignedGroupIdsAsync()
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return new HashSet<int>();
        }

        return await _dbContext.TeacherClassGroups
            .AsNoTracking()
            .Where(x => x.TeacherUserId == userId && x.IsActive)
            .Select(x => x.ClassGroupId)
            .ToHashSetAsync();
    }
}
