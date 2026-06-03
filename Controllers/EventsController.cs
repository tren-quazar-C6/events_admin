using System.Globalization;
using System.Security.Claims;
using events_admin.Data;
using events_admin.Models;
using events_admin.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace events_admin.Controllers;

[Authorize]
public class EventsController : Controller
{
    private readonly EventService _eventosApi;
    private readonly QuasarDbContext _db;

    public EventsController(EventService eventosApi, QuasarDbContext db)
    {
        _eventosApi = eventosApi;
        _db = db;
    }

    private string? GetJwtToken() => User.FindFirstValue("JWToken");

    private int GetStaffId()
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(raw, out var id) ? id : 0;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        string? busqueda,
        string? status,
        int? id_tipo_evento,
        int page = 1,
        CancellationToken ct = default)
    {
        var token = GetJwtToken();
        if (string.IsNullOrEmpty(token))
            return RedirectToAction("Login", "Home");

        var eventos = await _eventosApi.GetEventosAsync(token, busqueda, status, id_tipo_evento, ct);

        if (eventos is null)
        {
            ViewBag.Error = "No se pudo conectar con el servidor de eventos.";
            eventos = new List<AdminEventoResumenDto>();
        }

        ViewBag.Busqueda = busqueda;
        ViewBag.StatusActivo = status;
        ViewBag.TipoActivo = id_tipo_evento;

        const int pageSize = 5;
        var totalRegistros = eventos.Count;
        var totalPaginas = (int)Math.Ceiling(totalRegistros / (double)pageSize);

        var eventosPagina = eventos
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        ViewBag.Page = page;
        ViewBag.TotalPages = totalPaginas;
        ViewBag.TotalRecords = totalRegistros;

        return View(eventosPagina);
    }

    [HttpGet]
    public IActionResult Create()
    {
        var token = GetJwtToken();
        if (string.IsNullOrEmpty(token))
            return RedirectToAction("Login", "Home");

        return View(new CreateEventoFormModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        [FromForm] CreateEventoFormModel form,
        CancellationToken ct = default)
    {
        var token = GetJwtToken();
        if (string.IsNullOrEmpty(token))
            return RedirectToAction("Login", "Home");

        if (!TryValidateEventoFechas(form, out var fechaEvento, out var fechaInicioVentas, out var fechaFinVentas,
                out var parseErrors))
        {
            ViewBag.Error = string.Join("\n", parseErrors);
            return View(form);
        }

        var apiRequest = BuildApiRequest(form, fechaEvento, fechaInicioVentas, fechaFinVentas, GetStaffId());
        var (ok, error, idEvento) = await _eventosApi.CreateEventoAsync(token, apiRequest, ct);

        if (!ok)
        {
            ViewBag.Error = error;
            return View(form);
        }

        TempData["Success"] = "Evento publicado correctamente.";
        return RedirectToAction(nameof(Detail), new { id = idEvento });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken ct = default)
    {
        var token = GetJwtToken();
        if (string.IsNullOrEmpty(token))
            return RedirectToAction("Login", "Home");

        var evento = await _eventosApi.GetEventoAsync(token, id, ct);
        if (evento is null)
            return NotFound();

        return View(MapToFormModel(evento));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        int id,
        [FromForm] CreateEventoFormModel form,
        CancellationToken ct = default)
    {
        var token = GetJwtToken();
        if (string.IsNullOrEmpty(token))
            return RedirectToAction("Login", "Home");

        if (!TryValidateEventoFechas(form, out var fechaEvento, out var fechaInicioVentas, out var fechaFinVentas,
                out var parseErrors))
        {
            ViewBag.Error = string.Join("\n", parseErrors);
            form.id_evento = id;
            return View(form);
        }

        var apiRequest = BuildApiRequest(form, fechaEvento, fechaInicioVentas, fechaFinVentas, GetStaffId());
        var (ok, error) = await _eventosApi.UpdateEventoAsync(token, id, apiRequest, ct);

        if (!ok)
        {
            ViewBag.Error = error;
            form.id_evento = id;
            return View(form);
        }

        TempData["Success"] = "Evento actualizado correctamente.";
        return RedirectToAction(nameof(Detail), new { id });
    }

    [HttpGet]
    public async Task<IActionResult> Detail(int id, CancellationToken ct = default)
    {
        var token = GetJwtToken();
        if (string.IsNullOrEmpty(token))
            return RedirectToAction("Login", "Home");

        var evento = await _eventosApi.GetEventoAsync(token, id, ct);
        if (evento is null)
            return NotFound();

        return View(evento);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GenerateSeats(int id, CancellationToken ct = default)
    {
        var token = GetJwtToken();
        if (string.IsNullOrEmpty(token))
            return RedirectToAction("Login", "Home");

        var (ok, error) = await _eventosApi.GenerateEventSeatsAsync(token, id, ct);
        TempData[ok ? "Success" : "Error"] = ok
            ? "Butacas generadas correctamente."
            : error ?? "No se pudieron generar las butacas.";

        return RedirectToAction(nameof(Detail), new { id });
    }

    [HttpGet]
    public async Task<IActionResult> ConfigureZones(int id, CancellationToken ct = default)
    {
        var token = GetJwtToken();
        if (string.IsNullOrEmpty(token))
            return RedirectToAction("Login", "Home");

        var evento = await _eventosApi.GetEventoAsync(token, id, ct);
        if (evento is null)
            return NotFound();

        var zonasCatalogo = await _db.ZONAs
            .AsNoTracking()
            .Where(z => z.activo == true)
            .OrderBy(z => z.nombre_zona)
            .Select(z => new ZonaCatalogoDto(z.id_zona, z.nombre_zona, z.color_hex))
            .ToListAsync(ct);

        var capacidadFisicaPorZona = await GetCapacidadesFisicasPorZonaAsync(
            zonasCatalogo.Select(z => z.id_zona),
            ct);

        var zonasEvento = evento.zonas?
            .Select(z => new EventoZonaFormItem
            {
                id_zona = z.id_zona,
                nombre_zona = z.nombre_zona,
                color_hex = z.color_hex,
                activo = z.activo,
                precio = z.precio,
                cargo_servicio = z.cargo_servicio,
                capacidad = z.capacidad,
                capacidad_fisica = capacidadFisicaPorZona.TryGetValue(z.id_zona, out var capacidadFisica)
                    ? capacidadFisica
                    : 0
            })
            .ToList() ?? new List<EventoZonaFormItem>();

        var zonasExistentesIds = zonasEvento.Select(z => z.id_zona).ToHashSet();
        foreach (var zona in zonasCatalogo)
        {
            if (zonasExistentesIds.Contains(zona.id_zona))
                continue;

            zonasEvento.Add(new EventoZonaFormItem
            {
                id_zona = zona.id_zona,
                nombre_zona = zona.nombre_zona,
                color_hex = zona.color_hex,
                activo = true,
                precio = 0,
                cargo_servicio = 0,
                capacidad = 0,
                capacidad_fisica = capacidadFisicaPorZona.TryGetValue(zona.id_zona, out var capacidadFisica)
                    ? capacidadFisica
                    : 0
            });
        }

        var model = new EventoZonasFormModel
        {
            id_evento = evento.id_evento,
            nombre_evento = evento.nombre_evento,
            tipo_evento = evento.tipo_evento,
            fecha_evento = evento.fecha_evento,
            capacidad_total = evento.capacidad_total,
            zonas = zonasEvento.OrderBy(z => z.nombre_zona).ToList(),
            catalogo_zonas = zonasCatalogo
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ConfigureZones(
        int id,
        [FromForm] EventoZonasFormModel form,
        CancellationToken ct = default)
    {
        var token = GetJwtToken();
        if (string.IsNullOrEmpty(token))
            return RedirectToAction("Login", "Home");

        var zonasFormulario = form.zonas ?? new List<EventoZonaFormItem>();
        var capacidadFisicaPorZona = await GetCapacidadesFisicasPorZonaAsync(
            zonasFormulario.Select(z => z.id_zona),
            ct);

        foreach (var zona in zonasFormulario)
        {
            zona.capacidad_fisica = capacidadFisicaPorZona.TryGetValue(zona.id_zona, out var capacidadFisica)
                ? capacidadFisica
                : 0;
        }

        var zonasActivasSinCapacidad = zonasFormulario
            .Where(z => z.activo && z.capacidad_fisica <= 0)
            .Select(z => z.nombre_zona)
            .ToList();

        if (zonasActivasSinCapacidad.Any())
        {
            ViewBag.Error = $"Estas zonas activas no tienen asientos físicos disponibles: {string.Join(", ", zonasActivasSinCapacidad)}";
            await RehydrateZonesFormAsync(form, ct);
            return View(form);
        }

        var payload = zonasFormulario
            .Where(z => z.activo)
            .Select(z => new ZonaEventoRequest
            {
                id_zona = z.id_zona,
                precio = z.precio,
                cargo_servicio = z.cargo_servicio,
                capacidad = z.capacidad_fisica
            })
            .ToList();

        if (payload.Count == 0)
        {
            ViewBag.Error = "Debes activar al menos una zona con asientos físicos disponibles.";
            await RehydrateZonesFormAsync(form, ct);
            return View(form);
        }

        var capacidadAsignada = payload.Sum(z => z.capacidad);
        if (capacidadAsignada > form.capacidad_total)
        {
            ViewBag.Error = $"La suma de capacidades ({capacidadAsignada}) excede la capacidad total del evento ({form.capacidad_total}).";
            await RehydrateZonesFormAsync(form, ct);
            return View(form);
        }

        var (ok, error) = await _eventosApi.UpdateEventoZonasAsync(token, id, payload, ct);
        if (!ok)
        {
            ViewBag.Error = error;
            await RehydrateZonesFormAsync(form, ct);
            return View(form);
        }

        TempData["Success"] = "Zonas del evento actualizadas correctamente.";
        return RedirectToAction(nameof(Detail), new { id });
    }

    private async Task RehydrateZonesFormAsync(EventoZonasFormModel form, CancellationToken ct)
    {
        var zonasCatalogo = await _db.ZONAs
            .AsNoTracking()
            .Where(z => z.activo == true)
            .OrderBy(z => z.nombre_zona)
            .Select(z => new ZonaCatalogoDto(z.id_zona, z.nombre_zona, z.color_hex))
            .ToListAsync(ct);

        var capacidadFisicaPorZona = await GetCapacidadesFisicasPorZonaAsync(
            zonasCatalogo.Select(z => z.id_zona),
            ct);

        foreach (var zona in form.zonas ?? Enumerable.Empty<EventoZonaFormItem>())
        {
            zona.capacidad_fisica = capacidadFisicaPorZona.TryGetValue(zona.id_zona, out var capacidadFisica)
                ? capacidadFisica
                : 0;
        }

        form.catalogo_zonas = zonasCatalogo;
    }

    private async Task<Dictionary<int, int>> GetCapacidadesFisicasPorZonaAsync(
        IEnumerable<int> zonaIds,
        CancellationToken ct)
    {
        var ids = zonaIds.Distinct().ToList();
        if (ids.Count == 0)
            return new Dictionary<int, int>();

        return await _db.ASIENTOs
            .AsNoTracking()
            .Where(a => a.IsActive == true && ids.Contains(a.ZoneId))
            .GroupBy(a => a.ZoneId)
            .Select(g => new { ZoneId = g.Key, Total = g.Count() })
            .ToDictionaryAsync(x => x.ZoneId, x => x.Total, ct);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangeStatus(
        int id, string status, string? motivo, CancellationToken ct = default)
    {
        var token = GetJwtToken();
        if (string.IsNullOrEmpty(token))
            return RedirectToAction("Login", "Home");

        var (ok, error) = await _eventosApi.UpdateStatusAsync(token, id, status, motivo, ct);

        TempData[ok ? "Success" : "Error"] = ok
            ? $"Status actualizado a {status}."
            : error ?? "No se pudo cambiar el status.";

        return RedirectToAction(nameof(Index));
    }

    private static CreateEventoApiRequest BuildApiRequest(
        CreateEventoFormModel form,
        DateTime fechaEvento,
        DateTime fechaInicioVentas,
        DateTime fechaFinVentas,
        int staffId)
    {
        return new CreateEventoApiRequest
        {
            id_tipo_evento = form.id_tipo_evento,
            creado_por_staff = staffId,
            nombre_evento = form.nombre_evento.Trim(),
            descripcion = string.IsNullOrWhiteSpace(form.descripcion) ? null : form.descripcion,
            ruta_url = form.ruta_url?.Trim() ?? string.Empty,
            fecha_evento = fechaEvento,
            fecha_inicio_ventas = fechaInicioVentas,
            fecha_fin_ventas = fechaFinVentas,
            capacidad_total = form.capacidad_total,
            zonas = null
        };
    }

    private CreateEventoFormModel MapToFormModel(AdminEventoDetalleDto evento)
    {
        return new CreateEventoFormModel
        {
            id_evento = evento.id_evento,
            nombre_evento = evento.nombre_evento,
            id_tipo_evento = evento.id_tipo_evento,
            descripcion = evento.descripcion,
            fecha_evento = evento.fecha_evento.ToString("yyyy-MM-ddTHH:mm"),
            fecha_inicio_ventas = evento.fecha_inicio_ventas.ToString("yyyy-MM-ddTHH:mm"),
            fecha_fin_ventas = evento.fecha_fin_ventas.ToString("yyyy-MM-ddTHH:mm"),
            capacidad_total = evento.capacidad_total,
            ruta_url = evento.ruta_url ?? string.Empty
        };
    }

    private static bool TryValidateEventoFechas(
        CreateEventoFormModel form,
        out DateTime fechaEvento,
        out DateTime fechaInicioVentas,
        out DateTime fechaFinVentas,
        out List<string> errors)
    {
        fechaEvento = fechaInicioVentas = fechaFinVentas = default;
        errors = new List<string>();

        var formats = new[] { "yyyy-MM-ddTHH:mm", "yyyy-MM-ddTHH:mm:ss" };
        var culture = CultureInfo.InvariantCulture;

        if (!DateTime.TryParseExact(form.fecha_evento, formats, culture, DateTimeStyles.None, out fechaEvento))
        {
            errors.Add("La fecha del evento no es válida.");
        }

        if (!DateTime.TryParseExact(form.fecha_inicio_ventas, formats, culture, DateTimeStyles.None, out fechaInicioVentas))
        {
            errors.Add("La fecha de inicio de ventas no es válida.");
        }

        if (!DateTime.TryParseExact(form.fecha_fin_ventas, formats, culture, DateTimeStyles.None, out fechaFinVentas))
        {
            errors.Add("La fecha de cierre de ventas no es válida.");
        }

        if (errors.Count > 0)
        {
            return false;
        }

        if (fechaEvento <= DateTime.Now)
        {
            errors.Add("La fecha del evento debe ser futura.");
        }

        if (fechaInicioVentas >= fechaFinVentas)
        {
            errors.Add("La fecha de inicio de ventas debe ser anterior al cierre de ventas.");
        }

        if (fechaFinVentas >= fechaEvento)
        {
            errors.Add("La fecha de cierre de ventas debe ser anterior a la fecha del evento.");
        }

        if (fechaInicioVentas >= fechaEvento)
        {
            errors.Add("La fecha de inicio de ventas debe ser anterior a la fecha del evento.");
        }

        return errors.Count == 0;
    }
}
