using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KidsAttendance.Web.Controllers;

[Authorize]
public class DashboardController : Controller
{
    [HttpGet]
    public IActionResult Index()
    {
        if (User.IsInRole("Admin"))
        {
            return View("Admin");
        }

        if (User.IsInRole("SnackTeam"))
        {
            return View("SnackTeam");
        }

        return View("Teacher");
    }
}
