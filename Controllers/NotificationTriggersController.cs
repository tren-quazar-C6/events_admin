using events_admin.Services;
using Microsoft.AspNetCore.Mvc;

namespace events_admin.Controllers;

[ApiController]
[Route("api/notification-triggers")]
public class NotificationTriggersController : ControllerBase
{
    private readonly NotificationTriggerService _notificationService;

    public NotificationTriggersController(NotificationTriggerService notificationService)
    {
        _notificationService = notificationService;
    }

    [HttpPost("favorites-update")]
    public async Task<IActionResult> TriggerFavoriteUpdate([FromBody] FavoriteUpdateRequest request)
    {
        var result = await _notificationService.TriggerFavoriteUpdateAsync(request.IdUsuario, request.IdEvento);
        return Ok(result);
    }

    [HttpPost("pqrs")]
    public async Task<IActionResult> TriggerPqrs([FromBody] PqrsNotificationRequest request)
    {
        var result = await _notificationService.TriggerPqrsNotificationAsync(request.IdPqrs, request.Message ?? string.Empty);
        return Ok(result);
    }
}

public class FavoriteUpdateRequest
{
    public int IdUsuario { get; set; }
    public int IdEvento { get; set; }
}

public class PqrsNotificationRequest
{
    public int IdPqrs { get; set; }
    public string? Message { get; set; }
}
