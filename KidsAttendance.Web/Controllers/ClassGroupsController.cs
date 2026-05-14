using KidsAttendance.Application.DTOs;
using KidsAttendance.Application.Interfaces;
using KidsAttendance.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KidsAttendance.Web.Controllers;

[Authorize(Roles = "Admin")]
public class ClassGroupsController : Controller
{
    private readonly IClassGroupService _classGroupService;

    public ClassGroupsController(IClassGroupService classGroupService)
    {
        _classGroupService = classGroupService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var groups = await _classGroupService.GetAllAsync(cancellationToken);
        var model = groups.Select(x => new ClassGroupViewModel
        {
            Id = x.Id,
            Name = x.Name,
            MinAge = x.MinAge,
            MaxAge = x.MaxAge,
            IsActive = x.IsActive
        }).ToList();

        return View(model);
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View(new ClassGroupViewModel { IsActive = true });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ClassGroupViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await _classGroupService.CreateAsync(new ClassGroupDto
        {
            Name = model.Name,
            MinAge = model.MinAge,
            MaxAge = model.MaxAge,
            IsActive = model.IsActive
        }, cancellationToken);

        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "No se pudo crear el grupo.");
            return View(model);
        }

        TempData["SuccessMessage"] = "Grupo creado correctamente.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var group = await _classGroupService.GetByIdAsync(id, cancellationToken);
        if (group is null)
        {
            return NotFound();
        }

        return View(new ClassGroupViewModel
        {
            Id = group.Id,
            Name = group.Name,
            MinAge = group.MinAge,
            MaxAge = group.MaxAge,
            IsActive = group.IsActive
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(ClassGroupViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await _classGroupService.UpdateAsync(new ClassGroupDto
        {
            Id = model.Id,
            Name = model.Name,
            MinAge = model.MinAge,
            MaxAge = model.MaxAge,
            IsActive = model.IsActive
        }, cancellationToken);

        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "No se pudo actualizar el grupo.");
            return View(model);
        }

        TempData["SuccessMessage"] = "Grupo actualizado correctamente.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleActive(int id, bool isActive, CancellationToken cancellationToken)
    {
        var result = await _classGroupService.SetActiveAsync(id, isActive, cancellationToken);
        TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Success
            ? (isActive ? "Grupo activado." : "Grupo desactivado.")
            : result.Error ?? "No se pudo actualizar el estado.";

        return RedirectToAction(nameof(Index));
    }
}
