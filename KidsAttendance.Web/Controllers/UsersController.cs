using KidsAttendance.Infrastructure.Persistence.Entities;
using KidsAttendance.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KidsAttendance.Web.Controllers;

[Authorize(Roles = "Admin")]
public class UsersController : Controller
{
    private readonly UserManager<AppUser> _userManager;
    private readonly RoleManager<AppRole> _roleManager;

    public UsersController(UserManager<AppUser> userManager, RoleManager<AppRole> roleManager)
    {
        _userManager = userManager;
        _roleManager = roleManager;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var users = await _userManager.Users.OrderBy(x => x.FullName).ToListAsync();
        var userRoles = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            userRoles[user.Id] = roles.FirstOrDefault() ?? "-";
        }

        ViewBag.UserRoles = userRoles;
        return View(users);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        await EnsureRolesAsync();
        return View(new UserCreateViewModel { IsActive = true, Role = "Teacher" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(UserCreateViewModel model)
    {
        await EnsureRolesAsync();
        if (!ModelState.IsValid || string.IsNullOrWhiteSpace(model.Password))
        {
            if (string.IsNullOrWhiteSpace(model.Password))
            {
                ModelState.AddModelError(nameof(model.Password), "La contraseña es requerida.");
            }

            return View(model);
        }

        var user = new AppUser
        {
            Id = Guid.NewGuid().ToString(),
            FullName = model.FullName.Trim(),
            Email = model.Email.Trim(),
            UserName = model.Email.Trim(),
            NormalizedEmail = model.Email.Trim().ToUpperInvariant(),
            NormalizedUserName = model.Email.Trim().ToUpperInvariant(),
            EmailConfirmed = true,
            IsActive = model.IsActive,
            CreatedAt = DateTime.UtcNow
        };

        var result = await _userManager.CreateAsync(user, model.Password);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
            return View(model);
        }

        await _userManager.AddToRoleAsync(user, model.Role);
        TempData["SuccessMessage"] = "Usuario creado correctamente.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(string id)
    {
        await EnsureRolesAsync();
        var user = await _userManager.FindByIdAsync(id);
        if (user is null) return NotFound();
        var roles = await _userManager.GetRolesAsync(user);

        return View(new UserCreateViewModel
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email ?? string.Empty,
            IsActive = user.IsActive,
            Role = roles.FirstOrDefault() ?? "Teacher"
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(UserCreateViewModel model)
    {
        await EnsureRolesAsync();
        if (!ModelState.IsValid || string.IsNullOrWhiteSpace(model.Id))
        {
            return View(model);
        }

        var user = await _userManager.FindByIdAsync(model.Id);
        if (user is null) return NotFound();

        user.FullName = model.FullName.Trim();
        user.Email = model.Email.Trim();
        user.UserName = model.Email.Trim();
        user.NormalizedEmail = model.Email.Trim().ToUpperInvariant();
        user.NormalizedUserName = model.Email.Trim().ToUpperInvariant();
        user.IsActive = model.IsActive;
        user.UpdatedAt = DateTime.UtcNow;

        var updateResult = await _userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            foreach (var error in updateResult.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
            return View(model);
        }

        var currentRoles = await _userManager.GetRolesAsync(user);
        if (!currentRoles.Contains(model.Role))
        {
            await _userManager.RemoveFromRolesAsync(user, currentRoles);
            await _userManager.AddToRoleAsync(user, model.Role);
        }

        TempData["SuccessMessage"] = "Usuario actualizado.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(string id, string newPassword)
    {
        if (string.IsNullOrWhiteSpace(newPassword))
        {
            TempData["ErrorMessage"] = "La nueva contraseña es requerida.";
            return RedirectToAction(nameof(Edit), new { id });
        }

        var user = await _userManager.FindByIdAsync(id);
        if (user is null) return NotFound();

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var result = await _userManager.ResetPasswordAsync(user, token, newPassword);
        TempData[result.Succeeded ? "SuccessMessage" : "ErrorMessage"] = result.Succeeded
            ? "Contraseña restablecida."
            : string.Join(", ", result.Errors.Select(x => x.Description));

        return RedirectToAction(nameof(Edit), new { id });
    }

    private async Task EnsureRolesAsync()
    {
        foreach (var roleName in new[] { "Admin", "Teacher", "SnackTeam" })
        {
            if (!await _roleManager.RoleExistsAsync(roleName))
            {
                await _roleManager.CreateAsync(new AppRole { Name = roleName, NormalizedName = roleName.ToUpperInvariant() });
            }
        }
    }
}
