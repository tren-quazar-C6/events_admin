using events_admin.Services;
using Microsoft.AspNetCore.Mvc;

namespace events_admin.Controllers;

[ApiController]
[Route("api/employee-permissions")]
public class EmployeePermissionsController : ControllerBase
{
    private readonly EmployeePermissionsService _permissionsService;

    public EmployeePermissionsController(EmployeePermissionsService permissionsService)
    {
        _permissionsService = permissionsService;
    }

    [HttpGet("portal-access/{idStaff:int}")]
    public async Task<IActionResult> ValidatePortalAccess(int idStaff)
    {
        var result = await _permissionsService.ValidatePortalAccessAsync(idStaff);
        return Ok(result);
    }

    [HttpGet("role-mapping")]
    public IActionResult MapRole([FromQuery] string role)
    {
        var permissions = _permissionsService.MapPermissions(role);
        return Ok(new { role, permissions });
    }
}
