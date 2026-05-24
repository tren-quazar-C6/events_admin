using events_admin.Services;
using Microsoft.AspNetCore.Mvc;

namespace events_admin.Controllers;

[ApiController]
[Route("api/event-business-rules")]
public class EventBusinessRulesController : ControllerBase
{
    private readonly EventBusinessRulesService _rulesService;

    public EventBusinessRulesController(EventBusinessRulesService rulesService)
    {
        _rulesService = rulesService;
    }

    [HttpGet("sales-window/{idEvento:int}")]
    public async Task<IActionResult> ValidateSalesWindow(int idEvento)
    {
        var result = await _rulesService.ValidateSalesWindowAsync(idEvento);
        return Ok(result);
    }

    [HttpGet("capacity/{idEvento:int}")]
    public async Task<IActionResult> ValidateCapacity(int idEvento, [FromQuery] int requestedSeats = 1)
    {
        var result = await _rulesService.ValidateCapacityAsync(idEvento, requestedSeats);
        return Ok(result);
    }
}
