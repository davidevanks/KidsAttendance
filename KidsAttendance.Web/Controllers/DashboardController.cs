using KidsAttendance.Infrastructure.Persistence.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace KidsAttendance.Web.Controllers;

[Authorize]
public class DashboardController : Controller
{
    private readonly UserManager<AppUser> _userManager;

    public DashboardController(UserManager<AppUser> userManager)
    {
        _userManager = userManager;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        if (User.IsInRole("Admin"))
        {
            return View("Admin");
        }

        if (User.IsInRole("SnackTeam"))
        {
            return View("SnackTeam");
        }

        var userId = _userManager.GetUserId(User);
        if (!string.IsNullOrWhiteSpace(userId))
        {
            var user = await _userManager.FindByIdAsync(userId);
            ViewBag.HasAsistenciaGlobal = user?.AsistenciaGlobal == true;
        }

        return View("Teacher");
    }
}
