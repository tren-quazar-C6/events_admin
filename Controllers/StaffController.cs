using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

namespace events_admin.Controllers;

public class StaffController : Controller
{
    private string? GetJwtToken() =>
        User.FindFirstValue("JWToken");

    private int GetStaffId()
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(raw, out var id) ? id : 0;
    }

    public IActionResult Index()
    {
        var token = GetJwtToken();
        if (string.IsNullOrEmpty(token))
            return RedirectToAction("Login", "Home");
        return View();
    }

    public IActionResult Create()
    {
        return View();
    }
}