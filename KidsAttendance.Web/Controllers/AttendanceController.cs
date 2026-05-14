using KidsAttendance.Application.Interfaces;
using KidsAttendance.Infrastructure.Persistence;
using KidsAttendance.Infrastructure.Persistence.Entities;
using KidsAttendance.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KidsAttendance.Web.Controllers;

[Authorize(Roles = "Admin,Teacher")]
public class AttendanceController : Controller
{
    private readonly KidsAttendanceDbContext _dbContext;
    private readonly UserManager<AppUser> _userManager;
    private readonly ISignatureService _signatureService;

    public AttendanceController(KidsAttendanceDbContext dbContext, UserManager<AppUser> userManager, ISignatureService signatureService)
    {
        _dbContext = dbContext;
        _userManager = userManager;
        _signatureService = signatureService;
    }

    [HttpGet]
    public async Task<IActionResult> CheckIn()
    {
        var model = new CheckInViewModel();
        if (User.IsInRole("Teacher"))
        {
            var teacherGroup = await GetTeacherActiveGroupAsync();
            if (teacherGroup is null)
            {
                ViewBag.CheckInBlocked = true;
                ViewBag.CheckInBlockedMessage = "No tenés un grupo activo asignado. Solicitá asignación al administrador.";
                await LoadCheckInLookupsAsync();
                return View(model);
            }

            model.ClassGroupId = teacherGroup.Id;
            ViewBag.TeacherFixedGroup = teacherGroup;
        }

        await LoadCheckInLookupsAsync();
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CheckIn(CheckInViewModel model)
    {
        ClassGroup? teacherGroup = null;
        if (User.IsInRole("Teacher"))
        {
            teacherGroup = await GetTeacherActiveGroupAsync();
            if (teacherGroup is null)
            {
                ViewBag.CheckInBlocked = true;
                ViewBag.CheckInBlockedMessage = "No tenés un grupo activo asignado. Solicitá asignación al administrador.";
                await LoadCheckInLookupsAsync();
                return View(model);
            }

            model.ClassGroupId = teacherGroup.Id;
            ViewBag.TeacherFixedGroup = teacherGroup;
        }

        await LoadCheckInLookupsAsync();
        if (!ModelState.IsValid) return View(model);
        if (model.ChildIds.Count == 0)
        {
            ModelState.AddModelError(nameof(model.ChildIds), "Seleccioná al menos un niño.");
            return View(model);
        }

        if (User.IsInRole("Teacher"))
        {
            var allowedGroups = await GetAssignedGroupIdsAsync();
            if (!allowedGroups.Contains(model.ClassGroupId))
            {
                ModelState.AddModelError(string.Empty, "No tenés permiso para registrar asistencia en ese grupo.");
                return View(model);
            }
        }

        var selectedChildren = await _dbContext.Children.AsNoTracking()
            .Where(x => model.ChildIds.Contains(x.Id))
            .Select(x => new { x.Id, x.CurrentClassGroupId, x.IsActive })
            .ToListAsync();
        if (selectedChildren.Count != model.ChildIds.Distinct().Count())
        {
            ModelState.AddModelError(nameof(model.ChildIds), "Uno o más niños no existen.");
            return View(model);
        }

        if (selectedChildren.Any(x => !x.IsActive || x.CurrentClassGroupId != model.ClassGroupId))
        {
            ModelState.AddModelError(nameof(model.ChildIds), "Todos los niños deben pertenecer al grupo seleccionado.");
            return View(model);
        }

        var session = await EnsureTodaySessionAsync();
        var selectedChildIds = model.ChildIds.Distinct().ToList();
        var normalizedToken = NormalizeToken(model.TokenNumber);

        var existingChildIds = await _dbContext.AttendanceRecords.AsNoTracking()
            .Where(x => x.AttendanceSessionId == session.Id && selectedChildIds.Contains(x.ChildId))
            .Select(x => x.ChildId)
            .ToListAsync();
        if (existingChildIds.Count > 0)
        {
            ModelState.AddModelError(nameof(model.ChildIds), "Uno o más niños ya tienen asistencia registrada hoy.");
            return View(model);
        }

        var userId = _userManager.GetUserId(User) ?? string.Empty;
        var checkInSignatureData = _signatureService.DecodeBase64Signature(model.CheckInSignatureBase64);
        var now = DateTime.UtcNow;

        foreach (var childId in selectedChildIds)
        {
            _dbContext.AttendanceRecords.Add(new AttendanceRecord
            {
                AttendanceSessionId = session.Id,
                ChildId = childId,
                ClassGroupId = model.ClassGroupId,
                TokenNumber = normalizedToken,
                CheckInGuardianId = model.GuardianId,
                CheckInTeacherId = userId,
                CheckInTime = now,
                CheckInSignatureData = checkInSignatureData,
                Status = "CheckedIn",
                CreatedAt = now
            });
        }

        await _dbContext.SaveChangesAsync();
        TempData["SuccessMessage"] = "Entrada registrada.";
        return RedirectToAction(nameof(Today));
    }

    [HttpGet]
    public async Task<IActionResult> CheckOut()
    {
        await LoadCheckOutDataAsync();
        return View(new CheckOutViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CheckOut(CheckOutViewModel model)
    {
        if (model.RecordIds.Count == 0)
        {
            TempData["ErrorMessage"] = "Seleccioná al menos un niño para registrar salida.";
            return RedirectToAction(nameof(CheckOut));
        }

        var records = await _dbContext.AttendanceRecords
            .Where(x => model.RecordIds.Contains(x.Id))
            .ToListAsync();
        if (records.Count != model.RecordIds.Distinct().Count() || records.Any(x => x.Status != "CheckedIn"))
        {
            TempData["ErrorMessage"] = "Hay registros no válidos para salida.";
            return RedirectToAction(nameof(CheckOut));
        }

        var firstClassGroupId = records[0].ClassGroupId;
        if (records.Any(x => x.ClassGroupId != firstClassGroupId))
        {
            TempData["ErrorMessage"] = "Solo podés registrar salida múltiple dentro del mismo grupo.";
            return RedirectToAction(nameof(CheckOut));
        }

        if (User.IsInRole("Teacher"))
        {
            var allowedGroups = await GetAssignedGroupIdsAsync();
            if (!allowedGroups.Contains(firstClassGroupId))
            {
                TempData["ErrorMessage"] = "No tenés permiso para registrar salida en ese grupo.";
                return RedirectToAction(nameof(CheckOut));
            }
        }

        var validGuardianIds = await _dbContext.ChildGuardians.AsNoTracking()
            .Where(x => records.Select(r => r.ChildId).Contains(x.ChildId))
            .Select(x => x.GuardianId)
            .Distinct()
            .ToListAsync();
        if (!validGuardianIds.Contains(model.CheckOutGuardianId))
        {
            TempData["ErrorMessage"] = "El tutor seleccionado no está asociado a los niños elegidos.";
            return RedirectToAction(nameof(CheckOut));
        }

        var checkOutSignaturePath = await _signatureService.SaveBase64SignatureAsync(model.CheckOutSignatureBase64, "checkout");
        var now = DateTime.UtcNow;
        var teacherId = _userManager.GetUserId(User);

        foreach (var record in records)
        {
            record.CheckOutGuardianId = model.CheckOutGuardianId;
            record.CheckOutTeacherId = teacherId;
            record.CheckOutTime = now;
            record.CheckOutSignaturePath = checkOutSignaturePath;
            record.Status = "CheckedOut";
            record.UpdatedAt = now;
        }

        await _dbContext.SaveChangesAsync();
        TempData["SuccessMessage"] = "Salida registrada.";
        return RedirectToAction(nameof(Today));
    }

    [HttpGet]
    public async Task<IActionResult> Today(DateTime? date, int? classGroupId, string? guardianName, string? guardianPhone, string? childName)
    {
        var targetDate = date?.Date ?? DateTime.Today;
        var session = await _dbContext.AttendanceSessions.AsNoTracking().FirstOrDefaultAsync(x => x.SessionDate == targetDate);
        ViewBag.Date = targetDate;
        ViewBag.IsTeacher = User.IsInRole("Teacher");
        ViewBag.IsAdmin = User.IsInRole("Admin");
        ViewBag.GuardianName = guardianName;
        ViewBag.GuardianPhone = guardianPhone;
        ViewBag.ChildName = childName;

        if (User.IsInRole("Teacher"))
        {
            var teacherGroup = await GetTeacherActiveGroupAsync();
            if (teacherGroup is null)
            {
                ViewBag.TeacherNoGroup = true;
                ViewBag.Rows = new List<AttendanceTodayRowViewModel>();
                ViewBag.Groups = new List<ClassGroup>();
                return View();
            }

            classGroupId = teacherGroup.Id;
            ViewBag.TeacherFixedGroup = teacherGroup;
            ViewBag.Groups = new List<ClassGroup> { teacherGroup };
        }
        else
        {
            ViewBag.Groups = await _dbContext.ClassGroups.AsNoTracking().OrderBy(x => x.MinAge).ToListAsync();
        }

        ViewBag.SelectedClassGroupId = classGroupId;

        if (session is null)
        {
            ViewBag.Rows = new List<AttendanceTodayRowViewModel>();
            return View();
        }

        var query = _dbContext.AttendanceRecords.AsNoTracking().Where(x => x.AttendanceSessionId == session.Id);
        if (User.IsInRole("Teacher"))
        {
            query = query.Where(x => x.ClassGroupId == classGroupId);
        }
        else if (classGroupId.HasValue)
        {
            query = query.Where(x => x.ClassGroupId == classGroupId.Value);
        }

        var records = await query
            .OrderByDescending(x => x.CheckInTime)
            .Select(x => new
            {
                x.TokenNumber,
                x.Status,
                x.ClassGroupId,
                x.CheckInSignatureData,
                x.CheckInSignaturePath,
                x.CheckInGuardianId,
                x.ChildId
            })
            .ToListAsync();

        var guardianIds = records.Select(x => x.CheckInGuardianId).Distinct().ToList();
        var childIds = records.Select(x => x.ChildId).Distinct().ToList();

        var guardiansMap = await _dbContext.Guardians.AsNoTracking()
            .Where(x => guardianIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, x => new { x.FullName, x.PhoneNumber });

        var childrenMap = await _dbContext.Children.AsNoTracking()
            .Where(x => childIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, x => x.FullName);

        var classGroupIds = records.Select(x => x.ClassGroupId).Distinct().ToList();
        var classGroupsMap = await _dbContext.ClassGroups.AsNoTracking()
            .Where(x => classGroupIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, x => x.Name);

        var rows = records.Select(x =>
        {
            var guardian = guardiansMap.GetValueOrDefault(x.CheckInGuardianId);
            var childDisplayName = childrenMap.GetValueOrDefault(x.ChildId, $"Niño #{x.ChildId}");
            var signatureDataUrl = x.CheckInSignatureData is not null && x.CheckInSignatureData.Length > 0
                ? $"data:image/png;base64,{Convert.ToBase64String(x.CheckInSignatureData)}"
                : null;
            var signaturePath = signatureDataUrl ?? x.CheckInSignaturePath;

            return new AttendanceTodayRowViewModel
            {
                ClassGroupName = classGroupsMap.GetValueOrDefault(x.ClassGroupId, "-"),
                GuardianName = guardian?.FullName ?? $"Tutor #{x.CheckInGuardianId}",
                GuardianPhone = guardian?.PhoneNumber ?? "-",
                ChildName = childDisplayName,
                TokenNumber = x.TokenNumber ?? "-",
                DisplayStatus = MapStatus(x.Status),
                SignaturePath = signaturePath
            };
        }).ToList();

        if (!string.IsNullOrWhiteSpace(guardianName))
        {
            var term = guardianName.Trim();
            rows = rows.Where(x => x.GuardianName.Contains(term, StringComparison.OrdinalIgnoreCase)).ToList();
        }
        if (!string.IsNullOrWhiteSpace(guardianPhone))
        {
            var term = guardianPhone.Trim();
            rows = rows.Where(x => x.GuardianPhone.Contains(term, StringComparison.OrdinalIgnoreCase)).ToList();
        }
        if (!string.IsNullOrWhiteSpace(childName))
        {
            var term = childName.Trim();
            rows = rows.Where(x => x.ChildName.Contains(term, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        ViewBag.Rows = rows;
        return View();
    }

    [HttpGet]
    public async Task<IActionResult> SearchGuardianByPhone(string term, int classGroupId)
    {
        if (string.IsNullOrWhiteSpace(term))
        {
            return Json(Array.Empty<object>());
        }

        if (User.IsInRole("Teacher"))
        {
            var allowedGroups = await GetAssignedGroupIdsAsync();
            if (!allowedGroups.Contains(classGroupId))
            {
                return Forbid();
            }
        }

        var guardianIdsInGroupQuery = _dbContext.ChildGuardians.AsNoTracking()
            .Join(_dbContext.Children.AsNoTracking().Where(c => c.IsActive && c.CurrentClassGroupId == classGroupId),
                cg => cg.ChildId, c => c.Id, (cg, c) => cg.GuardianId)
            .Distinct();

        var normalized = term.Trim();
        var data = await _dbContext.Guardians
            .AsNoTracking()
            .Where(x => x.IsActive
                && guardianIdsInGroupQuery.Contains(x.Id)
                && (x.PhoneNumber.Contains(normalized) || x.FullName.Contains(normalized)))
            .OrderBy(x => x.FullName)
            .Take(20)
            .Select(x => new { x.Id, x.FullName, x.PhoneNumber })
            .ToListAsync();

        return Json(data);
    }

    [HttpGet]
    public async Task<IActionResult> GetChildrenByGuardianAndGroup(int guardianId, int classGroupId)
    {
        var query = _dbContext.ChildGuardians
            .AsNoTracking()
            .Where(x => x.GuardianId == guardianId)
            .Join(_dbContext.Children.AsNoTracking(), cg => cg.ChildId, c => c.Id, (cg, c) => c)
            .Where(c => c.IsActive && c.CurrentClassGroupId == classGroupId)
            .Select(c => new { c.Id, c.FullName })
            .Distinct();

        if (User.IsInRole("Teacher"))
        {
            var allowedGroups = await GetAssignedGroupIdsAsync();
            if (!allowedGroups.Contains(classGroupId))
            {
                return Forbid();
            }
        }

        var data = await query.OrderBy(x => x.FullName).ToListAsync();
        return Json(data);
    }

    [HttpGet]
    public async Task<IActionResult> GetGuardiansForChildren(string childIds)
    {
        var ids = childIds.Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(x => int.TryParse(x, out var id) ? id : 0)
            .Where(x => x > 0)
            .Distinct()
            .ToList();
        if (ids.Count == 0)
        {
            return Json(Array.Empty<object>());
        }

        var guardians = await _dbContext.ChildGuardians.AsNoTracking()
            .Where(x => ids.Contains(x.ChildId))
            .Join(_dbContext.Guardians.AsNoTracking().Where(g => g.IsActive),
                cg => cg.GuardianId,
                g => g.Id,
                (cg, g) => new { g.Id, g.FullName, g.PhoneNumber })
            .Distinct()
            .OrderBy(x => x.FullName)
            .ToListAsync();

        return Json(guardians);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AssociateGuardianToChild(int childId, int guardianId, string relationship = "Tutor")
    {
        var child = await _dbContext.Children.AsNoTracking().FirstOrDefaultAsync(x => x.Id == childId);
        if (child is null)
        {
            return NotFound();
        }

        if (User.IsInRole("Teacher"))
        {
            var allowedGroups = await GetAssignedGroupIdsAsync();
            if (!allowedGroups.Contains(child.CurrentClassGroupId))
            {
                return Forbid();
            }
        }

        var exists = await _dbContext.ChildGuardians.AnyAsync(x => x.ChildId == childId && x.GuardianId == guardianId);
        if (!exists)
        {
            _dbContext.ChildGuardians.Add(new ChildGuardian
            {
                ChildId = childId,
                GuardianId = guardianId,
                Relationship = string.IsNullOrWhiteSpace(relationship) ? "Tutor" : relationship.Trim(),
                IsAuthorizedPickup = true,
                IsPrimary = false,
                CreatedAt = DateTime.UtcNow
            });
            await _dbContext.SaveChangesAsync();
        }

        return Json(new { success = true });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> QuickAddChild(string fullName, int classGroupId, int guardianId, int? age)
    {
        if (string.IsNullOrWhiteSpace(fullName))
        {
            return BadRequest();
        }

        if (User.IsInRole("Teacher"))
        {
            var allowedGroups = await GetAssignedGroupIdsAsync();
            if (!allowedGroups.Contains(classGroupId))
            {
                return Forbid();
            }
        }

        var child = new Child
        {
            FullName = fullName.Trim(),
            Age = age,
            CurrentClassGroupId = classGroupId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        _dbContext.Children.Add(child);
        await _dbContext.SaveChangesAsync();

        _dbContext.ChildGuardians.Add(new ChildGuardian
        {
            ChildId = child.Id,
            GuardianId = guardianId,
            Relationship = "Tutor",
            IsPrimary = false,
            IsAuthorizedPickup = true,
            CreatedAt = DateTime.UtcNow
        });
        await _dbContext.SaveChangesAsync();

        return Json(new { success = true, childId = child.Id, fullName = child.FullName });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> QuickAddGuardian(string fullName, string phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(fullName) || string.IsNullOrWhiteSpace(phoneNumber))
        {
            return BadRequest();
        }

        var normalizedPhone = phoneNumber.Trim();
        var existing = await _dbContext.Guardians.FirstOrDefaultAsync(x => x.PhoneNumber == normalizedPhone);
        if (existing is not null)
        {
            return Json(new { success = true, guardianId = existing.Id, fullName = existing.FullName, phoneNumber = existing.PhoneNumber, existing = true });
        }

        var guardian = new Guardian
        {
            FullName = fullName.Trim(),
            PhoneNumber = normalizedPhone,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        _dbContext.Guardians.Add(guardian);
        await _dbContext.SaveChangesAsync();

        return Json(new { success = true, guardianId = guardian.Id, fullName = guardian.FullName, phoneNumber = guardian.PhoneNumber, existing = false });
    }

    private async Task<AttendanceSession> EnsureTodaySessionAsync()
    {
        var today = DateTime.Today;
        var session = await _dbContext.AttendanceSessions.FirstOrDefaultAsync(x => x.SessionDate == today && x.IsOpen);
        if (session is not null) return session;

        session = new AttendanceSession
        {
            SessionDate = today,
            Name = $"Asistencia {today:dd/MM/yyyy}",
            IsOpen = true,
            CreatedAt = DateTime.UtcNow,
            CreatedByUserId = _userManager.GetUserId(User)
        };
        _dbContext.AttendanceSessions.Add(session);
        await _dbContext.SaveChangesAsync();
        return session;
    }

    private async Task LoadCheckInLookupsAsync()
    {
        var groupsQuery = _dbContext.ClassGroups.AsNoTracking().Where(x => x.IsActive);
        var childrenQuery = _dbContext.Children.AsNoTracking().Where(x => x.IsActive);
        if (User.IsInRole("Teacher"))
        {
            var allowedGroups = await GetAssignedGroupIdsAsync();
            groupsQuery = groupsQuery.Where(x => allowedGroups.Contains(x.Id));
            childrenQuery = childrenQuery.Where(x => allowedGroups.Contains(x.CurrentClassGroupId));
        }

        ViewBag.Children = await childrenQuery.OrderBy(x => x.FullName).ToListAsync();
        ViewBag.Guardians = await _dbContext.Guardians.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.FullName).ToListAsync();
        ViewBag.Groups = await groupsQuery.OrderBy(x => x.MinAge).ToListAsync();
    }

    private async Task LoadCheckOutDataAsync()
    {
        var session = await EnsureTodaySessionAsync();
        var query = _dbContext.AttendanceRecords.AsNoTracking()
            .Where(x => x.AttendanceSessionId == session.Id && x.Status == "CheckedIn");
        if (User.IsInRole("Teacher"))
        {
            var allowedGroups = await GetAssignedGroupIdsAsync();
            query = query.Where(x => allowedGroups.Contains(x.ClassGroupId));
        }

        var data = await query
            .Join(_dbContext.Children.AsNoTracking(), a => a.ChildId, c => c.Id, (a, c) => new { a, c })
            .Join(_dbContext.Guardians.AsNoTracking(), x => x.a.CheckInGuardianId, g => g.Id, (x, g) => new CheckOutSearchRow
            {
                RecordId = x.a.Id,
                ChildId = x.a.ChildId,
                ClassGroupId = x.a.ClassGroupId,
                ChildName = x.c.FullName,
                GuardianName = g.FullName,
                GuardianPhone = g.PhoneNumber,
                TokenNumber = x.a.TokenNumber
            })
            .OrderBy(x => x.ChildName)
            .ToListAsync();

        ViewBag.ActiveRows = data;
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

    private async Task<ClassGroup?> GetTeacherActiveGroupAsync()
    {
        if (!User.IsInRole("Teacher"))
        {
            return null;
        }

        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return null;
        }

        return await _dbContext.TeacherClassGroups.AsNoTracking()
            .Where(x => x.TeacherUserId == userId && x.IsActive)
            .Join(_dbContext.ClassGroups.AsNoTracking().Where(g => g.IsActive),
                t => t.ClassGroupId,
                g => g.Id,
                (t, g) => g)
            .FirstOrDefaultAsync();
    }

    private static string MapStatus(string? status)
    {
        return status switch
        {
            "CheckedIn" => "Presente",
            "CheckedOut" => "Retirado",
            "Cancelled" => "Cancelado",
            _ => status ?? "-"
        };
    }

    private static string? NormalizeToken(string? token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return null;
        }

        return token.Trim();
    }

    public sealed class CheckOutSearchRow
    {
        public int RecordId { get; set; }
        public int ChildId { get; set; }
        public int ClassGroupId { get; set; }
        public string ChildName { get; set; } = string.Empty;
        public string GuardianName { get; set; } = string.Empty;
        public string GuardianPhone { get; set; } = string.Empty;
        public string? TokenNumber { get; set; }
    }
}
