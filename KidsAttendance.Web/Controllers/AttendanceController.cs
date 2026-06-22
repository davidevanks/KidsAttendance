using KidsAttendance.Application.Interfaces;
using KidsAttendance.Infrastructure.Persistence;
using KidsAttendance.Infrastructure.Persistence.Entities;
using KidsAttendance.Infrastructure.Security;
using KidsAttendance.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace KidsAttendance.Web.Controllers;

[Authorize(Roles = ApplicationRoles.CoordinadorOrTeacher)]
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
        if (User.IsInRole(ApplicationRoles.Teacher))
        {
            var teacherGroup = await GetTeacherActiveGroupAsync();
            if (teacherGroup is null)
            {
                ViewBag.CheckInBlocked = true;
                ViewBag.CheckInBlockedMessage = "No tenés un grupo activo asignado. Solicitá asignación al coordinador.";
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
        if (User.IsInRole(ApplicationRoles.Teacher))
        {
            teacherGroup = await GetTeacherActiveGroupAsync();
            if (teacherGroup is null)
            {
                ViewBag.CheckInBlocked = true;
                ViewBag.CheckInBlockedMessage = "No tenés un grupo activo asignado. Solicitá asignación al coordinador.";
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

        if (User.IsInRole(ApplicationRoles.Teacher))
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

        if (!await IsGuardianLinkedToAllChildrenAsync(model.GuardianId, model.ChildIds))
        {
            ModelState.AddModelError(nameof(model.GuardianId), "La persona que entrega debe estar asociada a todos los niños seleccionados.");
            return View(model);
        }

        var session = await EnsureTodaySessionAsync();
        var selectedChildIds = model.ChildIds.Distinct().ToList();
        var normalizedToken = NormalizeToken(model.TokenNumber);

        var existingRecords = await _dbContext.AttendanceRecords
            .Where(x => x.AttendanceSessionId == session.Id && selectedChildIds.Contains(x.ChildId))
            .ToListAsync();

        var alreadyCheckedInIds = existingRecords
            .Where(x => x.Status == "CheckedIn")
            .Select(x => x.ChildId)
            .ToList();
        if (alreadyCheckedInIds.Count > 0)
        {
            ModelState.AddModelError(nameof(model.ChildIds), "Uno o más niños ya están presentes.");
            return View(model);
        }

        try
        {
            var userId = _userManager.GetUserId(User) ?? string.Empty;
            var checkInSignatureData = _signatureService.DecodeBase64Signature(model.CheckInSignatureBase64);
            var now = DateTime.UtcNow;

            var existingRecordMap = existingRecords.ToDictionary(x => x.ChildId);

            foreach (var childId in selectedChildIds)
            {
                if (existingRecordMap.TryGetValue(childId, out var existing))
                {
                    existing.CheckInGuardianId = model.GuardianId;
                    existing.CheckInTeacherId = userId;
                    existing.CheckInTime = now;
                    existing.CheckInSignatureData = checkInSignatureData;
                    existing.TokenNumber = normalizedToken;
                    existing.CheckOutGuardianId = null;
                    existing.CheckOutTeacherId = null;
                    existing.CheckOutTime = null;
                    existing.CheckOutSignatureData = null;
                    existing.CheckOutSignaturePath = null;
                    existing.Status = "CheckedIn";
                    existing.UpdatedAt = now;
                }
                else
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
            }

            await _dbContext.SaveChangesAsync();
            TempData["SuccessMessage"] = "Entrada registrada.";
            return RedirectToAction(nameof(Today));
        }
        catch
        {
            TempData["ErrorMessage"] = "Error al hacer registro. contacte soporte.";
            return RedirectToAction(nameof(CheckIn));
        }
    }

    [HttpGet]
    public async Task<IActionResult> GlobalCheckIn()
    {
        if (!await IsGlobalAttendanceAsync())
        {
            return Forbid();
        }

        var model = new CheckInViewModel();
        ViewBag.FormAction = "GlobalCheckIn";
        ViewBag.IsGlobalAttendance = true;
        await LoadCheckInLookupsAsync(loadAllGroups: true);
        return View("CheckIn", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GlobalCheckIn(CheckInViewModel model)
    {
        if (!await IsGlobalAttendanceAsync())
        {
            return Forbid();
        }

        ViewBag.FormAction = "GlobalCheckIn";
        ViewBag.IsGlobalAttendance = true;
        await LoadCheckInLookupsAsync(loadAllGroups: true);

        if (!ModelState.IsValid) return View("CheckIn", model);
        if (model.ChildIds.Count == 0)
        {
            ModelState.AddModelError(nameof(model.ChildIds), "Seleccioná al menos un niño.");
            return View("CheckIn", model);
        }

        var selectedChildren = await _dbContext.Children.AsNoTracking()
            .Where(x => model.ChildIds.Contains(x.Id))
            .Select(x => new { x.Id, x.CurrentClassGroupId, x.IsActive })
            .ToListAsync();
        if (selectedChildren.Count != model.ChildIds.Distinct().Count())
        {
            ModelState.AddModelError(nameof(model.ChildIds), "Uno o más niños no existen.");
            return View("CheckIn", model);
        }

        if (selectedChildren.Any(x => !x.IsActive || x.CurrentClassGroupId != model.ClassGroupId))
        {
            ModelState.AddModelError(nameof(model.ChildIds), "Todos los niños deben pertenecer al grupo seleccionado.");
            return View("CheckIn", model);
        }

        if (!await IsGuardianLinkedToAllChildrenAsync(model.GuardianId, model.ChildIds))
        {
            ModelState.AddModelError(nameof(model.GuardianId), "La persona que entrega debe estar asociada a todos los niños seleccionados.");
            return View("CheckIn", model);
        }

        var session = await EnsureTodaySessionAsync();
        var selectedChildIds = model.ChildIds.Distinct().ToList();
        var normalizedToken = NormalizeToken(model.TokenNumber);

        var existingRecords = await _dbContext.AttendanceRecords
            .Where(x => x.AttendanceSessionId == session.Id && selectedChildIds.Contains(x.ChildId))
            .ToListAsync();

        var alreadyCheckedInIds = existingRecords
            .Where(x => x.Status == "CheckedIn")
            .Select(x => x.ChildId)
            .ToList();
        if (alreadyCheckedInIds.Count > 0)
        {
            ModelState.AddModelError(nameof(model.ChildIds), "Uno o más niños ya están presentes.");
            return View("CheckIn", model);
        }

        try
        {
            var userId = _userManager.GetUserId(User) ?? string.Empty;
            var checkInSignatureData = _signatureService.DecodeBase64Signature(model.CheckInSignatureBase64);
            var now = DateTime.UtcNow;

            var existingRecordMap = existingRecords.ToDictionary(x => x.ChildId);

            foreach (var childId in selectedChildIds)
            {
                if (existingRecordMap.TryGetValue(childId, out var existing))
                {
                    existing.CheckInGuardianId = model.GuardianId;
                    existing.CheckInTeacherId = userId;
                    existing.CheckInTime = now;
                    existing.CheckInSignatureData = checkInSignatureData;
                    existing.TokenNumber = normalizedToken;
                    existing.CheckOutGuardianId = null;
                    existing.CheckOutTeacherId = null;
                    existing.CheckOutTime = null;
                    existing.CheckOutSignatureData = null;
                    existing.CheckOutSignaturePath = null;
                    existing.Status = "CheckedIn";
                    existing.UpdatedAt = now;
                }
                else
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
            }

            await _dbContext.SaveChangesAsync();
            TempData["SuccessMessage"] = "Entrada registrada.";
            return RedirectToAction(nameof(GlobalToday));
        }
        catch
        {
            TempData["ErrorMessage"] = "Error al hacer registro. contacte soporte.";
            return RedirectToAction(nameof(GlobalCheckIn));
        }
    }

    [HttpGet]
    public async Task<IActionResult> GlobalCheckOut()
    {
        if (!await IsGlobalAttendanceAsync())
        {
            return Forbid();
        }

        ViewBag.FormAction = "GlobalCheckOut";
        ViewBag.IsGlobalAttendance = true;
        await LoadCheckOutDataAsync(loadAllGroups: true);
        return View("CheckOut", new CheckOutViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GlobalCheckOut(CheckOutViewModel model)
    {
        return await ProcessCheckOutAsync(model, isGlobal: true);
    }

    [HttpGet]
    public async Task<IActionResult> GlobalToday(DateTime? date, int? classGroupId)
    {
        if (!await IsGlobalAttendanceAsync())
        {
            return Forbid();
        }

        var targetDate = date?.Date ?? DateTime.Today;
        var session = await _dbContext.AttendanceSessions.AsNoTracking().FirstOrDefaultAsync(x => x.SessionDate == targetDate);
        ViewBag.Date = targetDate;
        ViewBag.IsTeacher = User.IsInRole(ApplicationRoles.Teacher);
        ViewBag.IsCoordinador = User.IsInRole(ApplicationRoles.Coordinador);
        ViewBag.IsGlobalAttendance = true;
        ViewBag.Groups = await _dbContext.ClassGroups.AsNoTracking().OrderBy(x => x.MinAge).ToListAsync();
        ViewBag.SelectedClassGroupId = classGroupId;

        if (session is null)
        {
            ViewBag.Rows = new List<AttendanceTodayRowViewModel>();
            return View("Today");
        }

        var query = _dbContext.AttendanceRecords.AsNoTracking().Where(x => x.AttendanceSessionId == session.Id);
        if (classGroupId.HasValue)
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
                x.CheckOutSignatureData,
                x.CheckInSignaturePath,
                x.CheckOutSignaturePath,
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
            var checkInSignatureDataUrl = x.CheckInSignatureData is not null && x.CheckInSignatureData.Length > 0
                ? $"data:image/png;base64,{Convert.ToBase64String(x.CheckInSignatureData)}"
                : null;
            var checkInSignaturePath = checkInSignatureDataUrl ?? x.CheckInSignaturePath;
            var checkOutSignatureDataUrl = x.CheckOutSignatureData is not null && x.CheckOutSignatureData.Length > 0
                ? $"data:image/png;base64,{Convert.ToBase64String(x.CheckOutSignatureData)}"
                : null;
            var checkOutSignaturePath = checkOutSignatureDataUrl ?? x.CheckOutSignaturePath;

            return new AttendanceTodayRowViewModel
            {
                ClassGroupName = classGroupsMap.GetValueOrDefault(x.ClassGroupId, "-"),
                GuardianName = guardian?.FullName ?? $"Padre #{x.CheckInGuardianId}",
                GuardianPhone = guardian?.PhoneNumber ?? "-",
                ChildName = childDisplayName,
                TokenNumber = x.TokenNumber ?? "-",
                DisplayStatus = MapStatus(x.Status),
                CheckInSignaturePath = checkInSignaturePath,
                CheckOutSignaturePath = checkOutSignaturePath
            };
        }).ToList();

        ViewBag.Rows = rows;
        return View("Today");
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
        return await ProcessCheckOutAsync(model, isGlobal: false);
    }

    [HttpGet]
    public async Task<IActionResult> Today(DateTime? date, int? classGroupId)
    {
        var targetDate = date?.Date ?? DateTime.Today;
        var session = await _dbContext.AttendanceSessions.AsNoTracking().FirstOrDefaultAsync(x => x.SessionDate == targetDate);
        ViewBag.Date = targetDate;
        ViewBag.IsTeacher = User.IsInRole(ApplicationRoles.Teacher);
        ViewBag.IsCoordinador = User.IsInRole(ApplicationRoles.Coordinador);
        if (User.IsInRole(ApplicationRoles.Teacher))
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
        if (User.IsInRole(ApplicationRoles.Teacher))
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
                x.CheckOutSignatureData,
                x.CheckInSignaturePath,
                x.CheckOutSignaturePath,
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
            var checkInSignatureDataUrl = x.CheckInSignatureData is not null && x.CheckInSignatureData.Length > 0
                ? $"data:image/png;base64,{Convert.ToBase64String(x.CheckInSignatureData)}"
                : null;
            var checkInSignaturePath = checkInSignatureDataUrl ?? x.CheckInSignaturePath;
            var checkOutSignatureDataUrl = x.CheckOutSignatureData is not null && x.CheckOutSignatureData.Length > 0
                ? $"data:image/png;base64,{Convert.ToBase64String(x.CheckOutSignatureData)}"
                : null;
            var checkOutSignaturePath = checkOutSignatureDataUrl ?? x.CheckOutSignaturePath;

            return new AttendanceTodayRowViewModel
            {
                ClassGroupName = classGroupsMap.GetValueOrDefault(x.ClassGroupId, "-"),
                GuardianName = guardian?.FullName ?? $"Padre #{x.CheckInGuardianId}",
                GuardianPhone = guardian?.PhoneNumber ?? "-",
                ChildName = childDisplayName,
                TokenNumber = x.TokenNumber ?? "-",
                DisplayStatus = MapStatus(x.Status),
                CheckInSignaturePath = checkInSignaturePath,
                CheckOutSignaturePath = checkOutSignaturePath
            };
        }).ToList();

        ViewBag.Rows = rows;
        return View();
    }

    [HttpGet]
    public async Task<IActionResult> SearchGuardianByPhone(string term, int classGroupId, bool isGlobal = false)
    {
        if (string.IsNullOrWhiteSpace(term))
        {
            return Json(Array.Empty<object>());
        }

        if (!await CanAccessAttendanceGroupAsync(classGroupId, isGlobal))
        {
            return Forbid();
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
    public async Task<IActionResult> GetChildrenByGuardianAndGroup(int guardianId, int classGroupId, bool isGlobal = false)
    {
        var query = _dbContext.ChildGuardians
            .AsNoTracking()
            .Where(x => x.GuardianId == guardianId)
            .Join(_dbContext.Children.AsNoTracking(), cg => cg.ChildId, c => c.Id, (cg, c) => c)
            .Where(c => c.IsActive && c.CurrentClassGroupId == classGroupId)
            .Select(c => new { c.Id, c.FullName })
            .Distinct();

        if (!await CanAccessAttendanceGroupAsync(classGroupId, isGlobal))
        {
            return Forbid();
        }

        var data = await query.OrderBy(x => x.FullName).ToListAsync();
        return Json(data);
    }

    [HttpGet]
    public async Task<IActionResult> GetGuardiansForChildren(
        string childIds,
        bool authorizedPickupOnly = false,
        bool isGlobal = false)
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

        var children = await _dbContext.Children.AsNoTracking()
            .Where(x => ids.Contains(x.Id))
            .Select(x => new { x.Id, x.CurrentClassGroupId })
            .ToListAsync();
        if (children.Count != ids.Count || children.Select(x => x.CurrentClassGroupId).Distinct().Count() != 1)
        {
            return BadRequest(new { message = "Los niños seleccionados no son válidos o no pertenecen al mismo grupo." });
        }

        var classGroupId = children[0].CurrentClassGroupId;
        if (!await CanAccessAttendanceGroupAsync(classGroupId, isGlobal))
        {
            return Forbid();
        }

        var relationshipsQuery = _dbContext.ChildGuardians.AsNoTracking()
            .Where(x => ids.Contains(x.ChildId));
        if (authorizedPickupOnly)
        {
            relationshipsQuery = relationshipsQuery.Where(x => x.IsAuthorizedPickup);
        }

        var relationships = await relationshipsQuery
            .Select(x => new { x.ChildId, x.GuardianId })
            .ToListAsync();
        var commonGuardianIds = relationships
            .GroupBy(x => x.GuardianId)
            .Where(group => group.Select(x => x.ChildId).Distinct().Count() == ids.Count)
            .Select(group => group.Key)
            .ToList();

        var guardians = await _dbContext.Guardians.AsNoTracking()
            .Where(x => x.IsActive && commonGuardianIds.Contains(x.Id))
            .OrderBy(x => x.FullName)
            .Select(x => new { x.Id, x.FullName, x.PhoneNumber })
            .ToListAsync();

        return Json(guardians);
    }

    [HttpGet]
    public async Task<IActionResult> SearchChildren(string term, int classGroupId, bool isGlobal = false)
    {
        if (string.IsNullOrWhiteSpace(term) || term.Trim().Length < 2 || classGroupId <= 0)
        {
            return Json(Array.Empty<object>());
        }

        if (!await CanAccessAttendanceGroupAsync(classGroupId, isGlobal))
        {
            return Json(Array.Empty<object>());
        }

        var normalized = term.Trim();
        var data = await _dbContext.Children
            .AsNoTracking()
            .Where(c => c.IsActive && c.CurrentClassGroupId == classGroupId && c.FullName.Contains(normalized))
            .OrderBy(c => c.FullName)
            .Take(20)
            .Select(c => new { c.Id, c.FullName })
            .ToListAsync();

        return Json(data);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RegisterDropoffGuardian(
        string fullName,
        string phoneNumber,
        string childIds,
        bool isGlobal = false)
    {
        if (string.IsNullOrWhiteSpace(fullName) || string.IsNullOrWhiteSpace(phoneNumber) || string.IsNullOrWhiteSpace(childIds))
        {
            return BadRequest(new { success = false, message = "Nombre, celular y niños son requeridos." });
        }

        var normalizedPhone = phoneNumber.Trim();
        if (!Regex.IsMatch(normalizedPhone, @"^\d{8,15}$"))
        {
            return BadRequest(new { success = false, message = "El celular debe contener solo números (8 a 15 dígitos)." });
        }

        var ids = childIds.Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(x => int.TryParse(x, out var id) ? id : 0)
            .Where(x => x > 0)
            .Distinct()
            .ToList();

        if (ids.Count == 0)
        {
            return BadRequest(new { success = false, message = "Seleccioná al menos un niño." });
        }

        var children = await _dbContext.Children.AsNoTracking()
            .Where(x => ids.Contains(x.Id))
            .Select(x => new { x.Id, x.CurrentClassGroupId, x.IsActive })
            .ToListAsync();
        if (children.Count != ids.Count || children.Any(x => !x.IsActive) ||
            children.Select(x => x.CurrentClassGroupId).Distinct().Count() != 1)
        {
            return BadRequest(new { success = false, message = "Los niños seleccionados no son válidos o no pertenecen al mismo grupo." });
        }

        if (!await CanAccessAttendanceGroupAsync(children[0].CurrentClassGroupId, isGlobal))
        {
            return Forbid();
        }

        var guardian = await _dbContext.Guardians.FirstOrDefaultAsync(x => x.PhoneNumber == normalizedPhone);
        if (guardian is null)
        {
            guardian = new Guardian
            {
                FullName = fullName.Trim(),
                PhoneNumber = normalizedPhone,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            _dbContext.Guardians.Add(guardian);
            await _dbContext.SaveChangesAsync();
        }

        foreach (var childId in ids)
        {
            var exists = await _dbContext.ChildGuardians.AnyAsync(x => x.ChildId == childId && x.GuardianId == guardian.Id);
            if (!exists)
            {
                _dbContext.ChildGuardians.Add(new ChildGuardian
                {
                    ChildId = childId,
                    GuardianId = guardian.Id,
                    Relationship = "Tutor",
                    IsAuthorizedPickup = true,
                    IsPrimary = false,
                    CreatedAt = DateTime.UtcNow
                });
            }
        }

        await _dbContext.SaveChangesAsync();
        return Json(new { success = true, guardianId = guardian.Id, fullName = guardian.FullName, phoneNumber = guardian.PhoneNumber });
    }

    private async Task<IActionResult> ProcessCheckOutAsync(CheckOutViewModel model, bool isGlobal)
    {
        var checkOutAction = isGlobal ? nameof(GlobalCheckOut) : nameof(CheckOut);
        var todayAction = isGlobal ? nameof(GlobalToday) : nameof(Today);

        if (isGlobal && !await IsGlobalAttendanceAsync())
        {
            return Forbid();
        }

        if (model.RecordIds.Count == 0)
        {
            TempData["ErrorMessage"] = "Seleccioná al menos un niño para registrar salida.";
            return RedirectToAction(checkOutAction);
        }

        if (string.IsNullOrWhiteSpace(model.CheckOutSignatureBase64))
        {
            TempData["ErrorMessage"] = "La firma de salida es requerida.";
            return RedirectToAction(checkOutAction);
        }

        var session = await EnsureTodaySessionAsync();
        var recordIds = model.RecordIds.Distinct().ToList();
        var records = await _dbContext.AttendanceRecords
            .Where(x => recordIds.Contains(x.Id) && x.AttendanceSessionId == session.Id)
            .ToListAsync();
        if (records.Count != recordIds.Count || records.Any(x => x.Status != "CheckedIn"))
        {
            TempData["ErrorMessage"] = "Hay registros no válidos para la salida de hoy.";
            return RedirectToAction(checkOutAction);
        }

        var classGroupId = records[0].ClassGroupId;
        if (records.Any(x => x.ClassGroupId != classGroupId))
        {
            TempData["ErrorMessage"] = "Solo podés registrar salida múltiple dentro del mismo grupo.";
            return RedirectToAction(checkOutAction);
        }

        if (!await CanAccessAttendanceGroupAsync(classGroupId, isGlobal))
        {
            return Forbid();
        }

        var childIds = records.Select(x => x.ChildId).Distinct().ToList();
        var authorizedChildCount = await _dbContext.ChildGuardians.AsNoTracking()
            .Where(x => childIds.Contains(x.ChildId) &&
                        x.GuardianId == model.CheckOutGuardianId &&
                        x.IsAuthorizedPickup)
            .Join(
                _dbContext.Guardians.AsNoTracking().Where(x => x.IsActive),
                childGuardian => childGuardian.GuardianId,
                guardian => guardian.Id,
                (childGuardian, _) => childGuardian.ChildId)
            .Distinct()
            .CountAsync();
        if (authorizedChildCount != childIds.Count)
        {
            TempData["ErrorMessage"] = "La persona seleccionada debe estar autorizada para retirar a todos los niños elegidos.";
            return RedirectToAction(checkOutAction);
        }

        try
        {
            var checkOutSignatureData = _signatureService.DecodeBase64Signature(model.CheckOutSignatureBase64);
            var now = DateTime.UtcNow;
            var teacherId = _userManager.GetUserId(User);

            foreach (var record in records)
            {
                record.CheckOutGuardianId = model.CheckOutGuardianId;
                record.CheckOutTeacherId = teacherId;
                record.CheckOutTime = now;
                record.CheckOutSignatureData = checkOutSignatureData;
                record.CheckOutSignaturePath = null;
                record.Status = "CheckedOut";
                record.UpdatedAt = now;
            }

            await _dbContext.SaveChangesAsync();
            TempData["SuccessMessage"] = "Salida registrada.";
            return RedirectToAction(todayAction);
        }
        catch
        {
            TempData["ErrorMessage"] = "Error al hacer registro. contacte soporte.";
            return RedirectToAction(checkOutAction);
        }
    }

    private async Task<bool> CanAccessAttendanceGroupAsync(int classGroupId, bool isGlobal)
    {
        if (isGlobal)
        {
            return await IsGlobalAttendanceAsync();
        }

        if (!User.IsInRole(ApplicationRoles.Teacher))
        {
            return true;
        }

        var allowedGroups = await GetAssignedGroupIdsAsync();
        return allowedGroups.Contains(classGroupId);
    }

    private async Task<bool> IsGuardianLinkedToAllChildrenAsync(int guardianId, IEnumerable<int> childIds)
    {
        var distinctChildIds = childIds.Distinct().ToList();
        var linkedChildCount = await _dbContext.ChildGuardians.AsNoTracking()
            .Where(x => distinctChildIds.Contains(x.ChildId) && x.GuardianId == guardianId)
            .Join(
                _dbContext.Guardians.AsNoTracking().Where(x => x.IsActive),
                childGuardian => childGuardian.GuardianId,
                guardian => guardian.Id,
                (childGuardian, _) => childGuardian.ChildId)
            .Distinct()
            .CountAsync();

        return linkedChildCount == distinctChildIds.Count;
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

    private async Task LoadCheckInLookupsAsync(bool loadAllGroups = false)
    {
        var groupsQuery = _dbContext.ClassGroups.AsNoTracking().Where(x => x.IsActive);
        var childrenQuery = _dbContext.Children.AsNoTracking().Where(x => x.IsActive);
        if (User.IsInRole(ApplicationRoles.Teacher) && !loadAllGroups)
        {
            var allowedGroups = await GetAssignedGroupIdsAsync();
            groupsQuery = groupsQuery.Where(x => allowedGroups.Contains(x.Id));
            childrenQuery = childrenQuery.Where(x => allowedGroups.Contains(x.CurrentClassGroupId));
        }

        ViewBag.Children = await childrenQuery.OrderBy(x => x.FullName).ToListAsync();
        ViewBag.Guardians = await _dbContext.Guardians.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.FullName).ToListAsync();
        ViewBag.Groups = await groupsQuery.OrderBy(x => x.MinAge).ToListAsync();
    }

    private async Task LoadCheckOutDataAsync(bool loadAllGroups = false)
    {
        var session = await EnsureTodaySessionAsync();
        var query = _dbContext.AttendanceRecords.AsNoTracking()
            .Where(x => x.AttendanceSessionId == session.Id && x.Status == "CheckedIn");
        if (User.IsInRole(ApplicationRoles.Teacher) && !loadAllGroups)
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
        if (!User.IsInRole(ApplicationRoles.Teacher))
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

    private async Task<bool> IsGlobalAttendanceAsync()
    {
        if (User.IsInRole(ApplicationRoles.Coordinador))
        {
            return true;
        }

        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return false;
        }

        var user = await _userManager.FindByIdAsync(userId);
        return user?.AsistenciaGlobal == true;
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
