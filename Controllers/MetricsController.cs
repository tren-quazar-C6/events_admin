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
        [FromQuery(Name = "from")] DateTime from,
        [FromQuery(Name = "to")] DateTime to)
    {
        if (to.Date < from.Date)
        {
            return BadRequest(new { message = "The 'to' date must be greater than or equal to the 'from' date." });
        }

        var fromDate = from.Date;
        var toDate = to.Date.AddDays(1).AddTicks(-1);
        var total = await _metricsService.GetRevenueTotalAsync(fromDate, toDate);

        return Ok(new
        {
            from = fromDate,
            to = to.Date,
            revenue = total
        });
    }
}
