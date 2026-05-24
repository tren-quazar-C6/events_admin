using Microsoft.AspNetCore.Mvc;
using events_admin.Services;

namespace events_admin.Controllers;

public class DashboardController : Controller
{
    private readonly MetricsApiClient _metricsClient;
    private readonly ILogger<DashboardController> _logger;

    public DashboardController(MetricsApiClient metricsClient, ILogger<DashboardController> logger)
    {
        _metricsClient = metricsClient;
        _logger = logger;
    }

    /// <summary>
    /// Display the main dashboard with all metrics
    /// GET /dashboard or /dashboard/index
    /// </summary>
    public async Task<IActionResult> Index()
    {
        // Default: show last 30 days
        var hasta = DateTime.Now;
        var desde = hasta.AddDays(-30);

        var dashboard = await _metricsClient.GetDashboardAsync(desde, hasta);

        return View(dashboard);
    }

    /// <summary>
    /// Display metrics for a custom date range
    /// GET /dashboard/custom?desde=2026-05-01&hasta=2026-05-31
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Custom([FromQuery] DateTime? desde, [FromQuery] DateTime? hasta)
    {
        // Default to last 30 days if not provided
        var desdeDate = desde ?? DateTime.Now.AddDays(-30);
        var hastaDate = hasta ?? DateTime.Now;

        var dashboard = await _metricsClient.GetDashboardAsync(desdeDate, hastaDate);

        return View("Index", dashboard);
    }

    /// <summary>
    /// API endpoint to get revenue as JSON
    /// GET /api/dashboard/revenue?desde=2026-05-01&hasta=2026-05-31
    /// </summary>
    [HttpGet("api/dashboard/revenue")]
    public async Task<IActionResult> ApiRevenue([FromQuery] DateTime desde, [FromQuery] DateTime hasta)
    {
        var revenue = await _metricsClient.GetRevenueAsync(desde, hasta);
        return Json(new { success = true, revenue });
    }

    /// <summary>
    /// API endpoint to get weekly sales as JSON
    /// GET /api/dashboard/weekly?desde=2026-05-01&hasta=2026-05-31
    /// </summary>
    [HttpGet("api/dashboard/weekly")]
    public async Task<IActionResult> ApiWeeklySales([FromQuery] DateTime desde, [FromQuery] DateTime hasta)
    {
        var weeklySales = await _metricsClient.GetWeeklySalesAsync(desde, hasta);
        return Json(new { success = true, data = weeklySales });
    }

    /// <summary>
    /// API endpoint for attendance rate
    /// GET /api/dashboard/attendance/1
    /// </summary>
    [HttpGet("api/dashboard/attendance/{idEvento}")]
    public async Task<IActionResult> ApiAttendance(int idEvento)
    {
        var rate = await _metricsClient.GetAttendanceRateAsync(idEvento);
        return Json(new { success = true, idEvento, attendanceRate = rate });
    }
}