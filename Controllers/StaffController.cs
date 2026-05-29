using Microsoft.AspNetCore.Mvc;

namespace events_admin.Controllers;

public class StaffController : Controller
{
    public IActionResult Index()
    {
        return View();
    }

    public IActionResult Create()
    {
        return View();
    }
}