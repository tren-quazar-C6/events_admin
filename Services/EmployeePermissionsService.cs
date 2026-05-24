using events_admin.Data;
using Microsoft.EntityFrameworkCore;

namespace events_admin.Services;

public class EmployeePermissionsService
{
    private static readonly Dictionary<string, string[]> PermissionsByRole = new(StringComparer.OrdinalIgnoreCase)
    {
        ["ADMIN"] = ["dashboard.read", "metrics.read", "events.manage", "sales.manage", "pqrs.manage", "staff.manage"],
        ["SUPERVISOR"] = ["dashboard.read", "metrics.read", "events.manage", "pqrs.manage"],
        ["TAQUILLA"] = ["dashboard.read", "sales.manage"],
        ["SCANNER"] = ["dashboard.read", "tickets.scan"],
        ["SOPORTE"] = ["dashboard.read", "pqrs.manage"]
    };

    private readonly QuasarDbContext _db;

    public EmployeePermissionsService(QuasarDbContext db)
    {
        _db = db;
    }

    public async Task<PortalAccessDto> ValidatePortalAccessAsync(int idStaff)
    {
        var staff = await _db.STAFF
            .Include(s => s.id_rol_staffNavigation)
            .FirstOrDefaultAsync(s => s.id_staff == idStaff);

        if (staff is null)
        {
            return new PortalAccessDto { IdStaff = idStaff, CanAccess = false, Reason = "STAFF_NOT_FOUND" };
        }

        if (staff.activo != true || staff.id_rol_staffNavigation.activo != true)
        {
            return new PortalAccessDto
            {
                IdStaff = idStaff,
                Role = staff.id_rol_staffNavigation.nombre_rol,
                CanAccess = false,
                Reason = "STAFF_OR_ROLE_INACTIVE"
            };
        }

        var permissions = MapPermissions(staff.id_rol_staffNavigation.nombre_rol);

        return new PortalAccessDto
        {
            IdStaff = idStaff,
            Role = staff.id_rol_staffNavigation.nombre_rol,
            CanAccess = permissions.Count > 0,
            Reason = permissions.Count > 0 ? "ACCESS_GRANTED" : "ROLE_NOT_MAPPED",
            Permissions = permissions
        };
    }

    public IReadOnlyList<string> MapPermissions(string roleName)
    {
        var normalizedRole = NormalizeRole(roleName);
        return PermissionsByRole.TryGetValue(normalizedRole, out var permissions)
            ? permissions
            : Array.Empty<string>();
    }

    private static string NormalizeRole(string roleName)
    {
        return roleName.Trim().ToUpperInvariant()
            .Replace(" ", "_")
            .Replace("Á", "A")
            .Replace("É", "E")
            .Replace("Í", "I")
            .Replace("Ó", "O")
            .Replace("Ú", "U");
    }
}

public class PortalAccessDto
{
    public int IdStaff { get; set; }
    public string? Role { get; set; }
    public bool CanAccess { get; set; }
    public string Reason { get; set; } = string.Empty;
    public IReadOnlyList<string> Permissions { get; set; } = Array.Empty<string>();
}
