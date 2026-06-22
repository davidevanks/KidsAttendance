using KidsAttendance.Infrastructure.Persistence;
using KidsAttendance.Infrastructure.Persistence.Entities;
using KidsAttendance.Infrastructure.Security;
using KidsAttendance.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace KidsAttendance.Web.Controllers;

[Authorize(Roles = ApplicationRoles.CoordinadorOrTeacher)]
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
    public async Task<IActionResult> Index(bool myGroupOnly = false)
    {
        ViewBag.MyGroupOnly = myGroupOnly;
        var query = _dbContext.Children.AsNoTracking();
        if (User.IsInRole(ApplicationRoles.Teacher) && (!await IsGlobalTeacherAsync() || myGroupOnly))
        {
            var allowedGroups = await GetAssignedGroupIdsAsync();
            query = query.Where(x => allowedGroups.Contains(x.CurrentClassGroupId));
        }

        var children = await query
            .OrderBy(x => x.FullName)
            .Select(x => new ChildIndexViewModel
            {
                Id = x.Id,
                FullName = x.FullName,
                Age = x.Age,
                CurrentClassGroupId = x.CurrentClassGroupId,
                IsActive = x.IsActive
            })
            .ToListAsync();

        var childIds = children.Select(x => x.Id).ToList();
        var guardiansByChild = await _dbContext.ChildGuardians.AsNoTracking()
            .Where(x => childIds.Contains(x.ChildId))
            .Join(
                _dbContext.Guardians.AsNoTracking(),
                childGuardian => childGuardian.GuardianId,
                guardian => guardian.Id,
                (childGuardian, guardian) => new
                {
                    childGuardian.ChildId,
                    GuardianName = guardian.FullName,
                    GuardianPhoneNumber = guardian.PhoneNumber
                })
            .ToListAsync();

        var guardianNamesByChild = guardiansByChild
            .GroupBy(x => x.ChildId)
            .ToDictionary(
                group => group.Key,
                group => string.Join(", ", group
                    .Select(x => x.GuardianName)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(name => name)));

        var guardianPhonesByChild = guardiansByChild
            .GroupBy(x => x.ChildId)
            .ToDictionary(
                group => group.Key,
                group => string.Join(", ", group
                    .Select(x => x.GuardianPhoneNumber)
                    .Where(phone => !string.IsNullOrWhiteSpace(phone))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(phone => phone)));

        var groups = await _dbContext.ClassGroups.AsNoTracking().ToDictionaryAsync(x => x.Id, x => x.Name);
        ViewBag.Groups = groups;

        foreach (var child in children)
        {
            child.GuardiansText = guardianNamesByChild.GetValueOrDefault(child.Id) ?? "-";
            child.GuardianPhoneNumbersText = guardianPhonesByChild.GetValueOrDefault(child.Id) ?? "-";
        }

        return View(children);
    }

    [HttpGet]
    public async Task<IActionResult> Create(bool myGroupOnly = false)
    {
        await LoadLookupsAsync(myGroupOnly);
        return View(new ChildCreateViewModel { IsActive = true, MyGroupOnly = myGroupOnly });
    }

    [HttpGet]
    public async Task<IActionResult> SearchGuardians(string term)
    {
        if (string.IsNullOrWhiteSpace(term) || term.Length < 2)
            return Json(Array.Empty<object>());

        var query = _dbContext.Guardians.AsNoTracking().Where(x => x.IsActive);
        if (User.IsInRole(ApplicationRoles.Teacher) && !await IsGlobalTeacherAsync())
        {
            var allowedGroups = await GetAssignedGroupIdsAsync();
            query = query.Where(g => _dbContext.ChildGuardians
                .Any(cg => cg.GuardianId == g.Id &&
                           _dbContext.Children.Any(c => c.Id == cg.ChildId && allowedGroups.Contains(c.CurrentClassGroupId))));
        }

        var results = await query
            .Where(x => x.FullName.Contains(term) || x.PhoneNumber.Contains(term))
            .OrderBy(x => x.FullName)
            .Take(20)
            .Select(x => new { x.Id, x.FullName, x.PhoneNumber })
            .ToListAsync();

        return Json(results);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ChildCreateViewModel model)
    {
        // Remove server-side validation for NewGuardians list items — handled by JS before submit
        foreach (var key in ModelState.Keys.Where(k => k.StartsWith("NewGuardians")).ToList())
            ModelState.Remove(key);

        await LoadLookupsAsync(model.MyGroupOnly);
        if (!ModelState.IsValid) return View(model);

        if (User.IsInRole(ApplicationRoles.Teacher) && (!await IsGlobalTeacherAsync() || model.MyGroupOnly))
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

        var newGuardianIds = await CreateNewGuardiansAsync(model.NewGuardians);
        var allGuardianIds = model.SelectedGuardianIds.Concat(newGuardianIds.Keys).Distinct().ToList();
        var relationshipMap = BuildRelationshipMap(model.SelectedGuardianIds, model.SelectedGuardianRelationships, newGuardianIds);
        await SyncGuardiansAsync(child.Id, allGuardianIds, relationshipMap);

        TempData["SuccessMessage"] = "Niño registrado.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id, bool myGroupOnly = false)
    {
        var child = await _dbContext.Children.FindAsync(id);
        if (child is null) return NotFound();
        if (User.IsInRole(ApplicationRoles.Teacher) && (!await IsGlobalTeacherAsync() || myGroupOnly))
        {
            var allowedGroups = await GetAssignedGroupIdsAsync();
            if (!allowedGroups.Contains(child.CurrentClassGroupId))
            {
                return Forbid();
            }
        }

        var linkedGuardians = await _dbContext.ChildGuardians
            .Where(x => x.ChildId == id)
            .Join(_dbContext.Guardians, cg => cg.GuardianId, g => g.Id,
                (cg, g) => new { g.Id, g.FullName, g.PhoneNumber, cg.Relationship })
            .ToListAsync();

        ViewBag.LinkedGuardians = linkedGuardians
            .Select(g => new { g.Id, g.FullName, g.PhoneNumber, g.Relationship })
            .ToList();

        await LoadLookupsAsync(myGroupOnly);

        return View(new ChildCreateViewModel
        {
            Id = child.Id,
            FullName = child.FullName,
            BirthDate = child.BirthDate,
            Age = child.Age,
            CurrentClassGroupId = child.CurrentClassGroupId,
            IsActive = child.IsActive,
            MyGroupOnly = myGroupOnly,
            SelectedGuardianIds = linkedGuardians.Select(x => x.Id).ToList(),
            SelectedGuardianRelationships = linkedGuardians.Select(x => x.Relationship).ToList()
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(ChildCreateViewModel model)
    {
        foreach (var key in ModelState.Keys.Where(k => k.StartsWith("NewGuardians")).ToList())
            ModelState.Remove(key);

        await LoadLookupsAsync(model.MyGroupOnly);
        if (!ModelState.IsValid) return View(model);

        var child = await _dbContext.Children.FirstOrDefaultAsync(x => x.Id == model.Id);
        if (child is null) return NotFound();
        if (User.IsInRole(ApplicationRoles.Teacher) && (!await IsGlobalTeacherAsync() || model.MyGroupOnly))
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

        var newGuardianIds = await CreateNewGuardiansAsync(model.NewGuardians);
        var allGuardianIds = model.SelectedGuardianIds.Concat(newGuardianIds.Keys).Distinct().ToList();
        var relationshipMap = BuildRelationshipMap(model.SelectedGuardianIds, model.SelectedGuardianRelationships, newGuardianIds);
        await SyncGuardiansAsync(child.Id, allGuardianIds, relationshipMap);

        TempData["SuccessMessage"] = "Niño actualizado.";
        return RedirectToAction(nameof(Index));
    }

    private async Task LoadLookupsAsync(bool myGroupOnly = false)
    {
        var groupsQuery = _dbContext.ClassGroups.AsNoTracking().Where(x => x.IsActive);
        if (User.IsInRole(ApplicationRoles.Teacher) && (!await IsGlobalTeacherAsync() || myGroupOnly))
        {
            var allowedGroups = await GetAssignedGroupIdsAsync();
            groupsQuery = groupsQuery.Where(x => allowedGroups.Contains(x.Id));
        }

        var groups = await groupsQuery.OrderBy(x => x.MinAge).ToListAsync();
        ViewBag.ClassGroups = new SelectList(groups, "Id", "Name");
    }

    // Creates new guardians from inline form entries, reusing existing ones if the phone already exists.
    // Returns a map of (newGuardianId → relationship).
    private async Task<Dictionary<int, string>> CreateNewGuardiansAsync(IEnumerable<NewGuardianEntry> entries)
    {
        var result = new Dictionary<int, string>();
        foreach (var entry in entries)
        {
            if (string.IsNullOrWhiteSpace(entry.FullName) || string.IsNullOrWhiteSpace(entry.PhoneNumber))
                continue;

            var phone = entry.PhoneNumber.Trim();
            var existing = await _dbContext.Guardians.FirstOrDefaultAsync(g => g.PhoneNumber == phone);
            if (existing is not null)
            {
                result[existing.Id] = entry.Relationship;
            }
            else
            {
                var guardian = new Guardian
                {
                    FullName = entry.FullName.Trim(),
                    PhoneNumber = phone,
                    SecondaryPhoneNumber = string.IsNullOrWhiteSpace(entry.SecondaryPhoneNumber) ? null : entry.SecondaryPhoneNumber.Trim(),
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };
                _dbContext.Guardians.Add(guardian);
                await _dbContext.SaveChangesAsync();
                result[guardian.Id] = entry.Relationship;
            }
        }
        return result;
    }

    // Builds a dictionary mapping guardianId → relationship from existing selections plus newly created ones.
    private static Dictionary<int, string> BuildRelationshipMap(
        List<int> selectedIds,
        List<string> selectedRelationships,
        Dictionary<int, string> newGuardianIds)
    {
        var map = new Dictionary<int, string>();
        for (var i = 0; i < selectedIds.Count; i++)
        {
            var rel = i < selectedRelationships.Count ? selectedRelationships[i] : "Tutor";
            map[selectedIds[i]] = string.IsNullOrWhiteSpace(rel) ? "Tutor" : rel;
        }
        foreach (var (id, rel) in newGuardianIds)
            map[id] = rel;
        return map;
    }

    private async Task SyncGuardiansAsync(int childId, IEnumerable<int> guardianIds, Dictionary<int, string> relationshipMap)
    {
        var selected = guardianIds.Distinct().ToHashSet();
        var existing = await _dbContext.ChildGuardians.Where(x => x.ChildId == childId).ToListAsync();

        var toRemove = existing.Where(x => !selected.Contains(x.GuardianId)).ToList();
        if (toRemove.Count > 0)
            _dbContext.ChildGuardians.RemoveRange(toRemove);

        var existingByGuardian = existing.ToDictionary(x => x.GuardianId);
        foreach (var guardianId in selected)
        {
            var relationship = relationshipMap.GetValueOrDefault(guardianId, "Tutor");
            if (existingByGuardian.TryGetValue(guardianId, out var link))
            {
                // Update relationship if it changed
                if (link.Relationship != relationship)
                    link.Relationship = relationship;
            }
            else
            {
                _dbContext.ChildGuardians.Add(new ChildGuardian
                {
                    ChildId = childId,
                    GuardianId = guardianId,
                    Relationship = relationship,
                    IsPrimary = false,
                    IsAuthorizedPickup = true,
                    CreatedAt = DateTime.UtcNow
                });
            }
        }

        await _dbContext.SaveChangesAsync();
    }

    private async Task<bool> IsGlobalTeacherAsync()
    {
        if (!User.IsInRole(ApplicationRoles.Teacher)) return false;
        var user = await _userManager.GetUserAsync(User);
        return user?.AsistenciaGlobal == true;
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
