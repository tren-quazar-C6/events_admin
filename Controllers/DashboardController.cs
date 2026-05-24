using Microsoft.AspNetCore.Mvc;
using events_admin.Services;

namespace events_admin.Controllers;

public class DashboardController : Controller
{
    private readonly MetricsService _metricsService;
    private readonly ILogger<DashboardController> _logger;

    public DashboardController(MetricsService metricsService, ILogger<DashboardController> logger)
    {
        _metricsService = metricsService;
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

        var dashboard = await BuildDashboardAsync(desde, hasta);

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

        var dashboard = await BuildDashboardAsync(desdeDate, hastaDate);

        return View("Index", dashboard);
    }

    /// <summary>
    /// API endpoint to get revenue as JSON
    /// GET /api/dashboard/revenue?desde=2026-05-01&hasta=2026-05-31
    /// </summary>
    [HttpGet("api/dashboard/revenue")]
    public async Task<IActionResult> ApiRevenue([FromQuery] DateTime desde, [FromQuery] DateTime hasta)
    {
        var range = ResolveDateRange(desde, hasta);
        var revenue = await _metricsService.GetRevenueTotalAsync(range.Start, range.End);
        return Json(new { success = true, revenue });
    }

    /// <summary>
    /// API endpoint to get weekly sales as JSON
    /// GET /api/dashboard/weekly?desde=2026-05-01&hasta=2026-05-31
    /// </summary>
    [HttpGet("api/dashboard/weekly")]
    public async Task<IActionResult> ApiWeeklySales([FromQuery] DateTime desde, [FromQuery] DateTime hasta)
    {
        var range = ResolveDateRange(desde, hasta);
        var weeklySales = await _metricsService.GetWeeklySalesAsync(range.Start, range.End);
        return Json(new { success = true, data = weeklySales });
    }

    /// <summary>
    /// API endpoint for attendance rate
    /// GET /api/dashboard/attendance/1
    /// </summary>
    [HttpGet("api/dashboard/attendance/{idEvento}")]
    public async Task<IActionResult> ApiAttendance(int idEvento)
    {
        var rate = await _metricsService.GetAttendanceRateAsync(idEvento);
        return Json(new { success = true, idEvento, attendanceRate = rate });
    }

    private async Task<DashboardMetricsDto> BuildDashboardAsync(DateTime desde, DateTime hasta)
    {
        try
        {
            var range = ResolveDateRange(desde, hasta);
            var totalRevenue = await _metricsService.GetRevenueTotalAsync(range.Start, range.End);
            var totalTickets = await _metricsService.GetTicketsSoldAsync(range.Start, range.End);
            var weeklySales = await _metricsService.GetWeeklySalesAsync(range.Start, range.End);

            return new DashboardMetricsDto
            {
                Success = true,
                Desde = range.Start.Date,
                Hasta = range.End.Date,
                TotalRevenue = totalRevenue,
                TotalTickets = totalTickets,
                AveragePerTicket = totalTickets == 0 ? "N/A" : (totalRevenue / totalTickets).ToString("C0"),
                WeeklySales = weeklySales
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading dashboard metrics.");

            return new DashboardMetricsDto
            {
                Success = false,
                Error = ex.Message,
                Desde = desde.Date,
                Hasta = hasta.Date
            };
        }
    }

    private static (DateTime Start, DateTime End) ResolveDateRange(DateTime desde, DateTime hasta)
    {
        var start = desde.Date;
        var endDate = hasta.Date;

        if (endDate < start)
        {
            (start, endDate) = (endDate, start);
        }

        return (start, endDate.AddDays(1).AddTicks(-1));
    }
}
