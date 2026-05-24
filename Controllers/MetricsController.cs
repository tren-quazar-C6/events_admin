using events_admin.Services;
using Microsoft.AspNetCore.Mvc;

namespace events_admin.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MetricsController : ControllerBase
{
    private readonly MetricsService _metricsService;

    public MetricsController(MetricsService metricsService)
    {
        _metricsService = metricsService;
    }

    [HttpGet("revenue")]
    public async Task<IActionResult> GetRevenue(
        [FromQuery] DateTime? desde,
        [FromQuery] DateTime? hasta,
        [FromQuery(Name = "from")] DateTime? from,
        [FromQuery(Name = "to")] DateTime? to)
    {
        var range = ResolveDateRange(desde, hasta, from, to);
        if (range.Error is not null)
        {
            return BadRequest(new { message = range.Error });
        }

        var total = await _metricsService.GetRevenueTotalAsync(range.Start, range.End);

        return Ok(new
        {
            success = true,
            desde = range.Start.Date,
            hasta = range.End.Date,
            revenue = total,
            total
        });
    }

    [HttpGet("tickets-sold")]
    public async Task<IActionResult> GetTicketsSold([FromQuery] DateTime? desde, [FromQuery] DateTime? hasta)
    {
        var range = ResolveDateRange(desde, hasta);
        if (range.Error is not null)
        {
            return BadRequest(new { message = range.Error });
        }

        var ticketsSold = await _metricsService.GetTicketsSoldAsync(range.Start, range.End);
        return Ok(new { success = true, desde = range.Start.Date, hasta = range.End.Date, ticketsSold });
    }

    [HttpGet("weekly-sales")]
    public async Task<IActionResult> GetWeeklySales([FromQuery] DateTime? desde, [FromQuery] DateTime? hasta)
    {
        var range = ResolveDateRange(desde, hasta);
        if (range.Error is not null)
        {
            return BadRequest(new { message = range.Error });
        }

        var weeks = await _metricsService.GetWeeklySalesAsync(range.Start, range.End);
        return Ok(new { success = true, desde = range.Start.Date, hasta = range.End.Date, weeks });
    }

    [HttpGet("attendance/{idEvento:int}")]
    public async Task<IActionResult> GetAttendance(int idEvento)
    {
        var attendanceRate = await _metricsService.GetAttendanceRateAsync(idEvento);
        return Ok(new { success = true, idEvento, attendanceRate });
    }

    [HttpGet("occupancy/{idEvento:int}")]
    public async Task<IActionResult> GetOccupancy(int idEvento)
    {
        var occupancy = await _metricsService.GetOccupancyAsync(idEvento);
        return Ok(new { success = true, occupancy });
    }

    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard([FromQuery] DateTime? desde, [FromQuery] DateTime? hasta)
    {
        var range = ResolveDateRange(desde, hasta);
        if (range.Error is not null)
        {
            return BadRequest(new { success = false, message = range.Error });
        }

        var revenue = await _metricsService.GetRevenueTotalAsync(range.Start, range.End);
        var ticketsSold = await _metricsService.GetTicketsSoldAsync(range.Start, range.End);
        var weeklySales = await _metricsService.GetWeeklySalesAsync(range.Start, range.End);

        return Ok(new
        {
            success = true,
            desde = range.Start.Date,
            hasta = range.End.Date,
            summary = new
            {
                totalRevenue = revenue,
                totalTickets = ticketsSold,
                averagePerTicket = ticketsSold == 0 ? "N/A" : (revenue / ticketsSold).ToString("C")
            },
            weeklySales
        });
    }

    private static (DateTime Start, DateTime End, string? Error) ResolveDateRange(
        DateTime? desde,
        DateTime? hasta,
        DateTime? from = null,
        DateTime? to = null)
    {
        var start = (desde ?? from ?? DateTime.UtcNow.AddDays(-30)).Date;
        var endDate = (hasta ?? to ?? DateTime.UtcNow).Date;

        if (endDate < start)
        {
            return (start, endDate, "The end date must be greater than or equal to the start date.");
        }

        return (start, endDate.AddDays(1).AddTicks(-1), null);
    }
}
