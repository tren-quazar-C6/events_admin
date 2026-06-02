using System.Security.Claims;
using events_admin.Models;
using events_admin.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace events_admin.Controllers;

[Authorize]
public class EventsController : Controller
{
    private readonly EventService _eventosApi;

    public EventsController(EventService eventosApi)
    {
        _eventosApi = eventosApi;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Lee el JWT que guardamos como claim "JWToken" al hacer login.
    /// Si no existe redirige al login.
    /// </summary>
    private string? GetJwtToken() =>
        User.FindFirstValue("JWToken");

    /// <summary>
    /// Lee el id_staff del claim "nameidentifier" (sub del JWT).
    /// </summary>
    private int GetStaffId()
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(raw, out var id) ? id : 0;
    }

    // ── GET /Events ───────────────────────────────────────────────────────────

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

        // Pasar filtros activos a la vista para mantener el estado del buscador
        ViewBag.Busqueda       = busqueda;
        ViewBag.StatusActivo   = status;
        ViewBag.TipoActivo     = id_tipo_evento;
        
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
        
        return View(eventos);
    }

    // ── GET /Events/Create ────────────────────────────────────────────────────

    [HttpGet]
    public IActionResult Create()
    {
        var token = GetJwtToken();
        if (string.IsNullOrEmpty(token))
            return RedirectToAction("Login", "Home");

        return View(); // Usa la vista que ya tienes (Create.cshtml)
    }

    // ── POST /Events/Create ───────────────────────────────────────────────────

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        [FromForm] CreateEventoFormModel form,
        CancellationToken ct = default)
    {
        var token = GetJwtToken();
        if (string.IsNullOrEmpty(token))
            return RedirectToAction("Login", "Home");

        // ── 1. Construir DateTime combinando fecha + hora ──────────────────

        if (!TryParseEventoFechas(form, out var fechaEvento, out var fechaInicioVentas, out var fechaFinVentas, out var parseError))
        {
            ViewBag.Error = parseError;
            return View(form);
        }

        // ── 2. Armar el request para la API ──────────────────────────────

        var apiRequest = new CreateEventoApiRequest
        {
            id_tipo_evento      = form.id_tipo_evento,
            creado_por_staff    = GetStaffId(),
            nombre_evento       = form.nombre_evento.Trim(),
            descripcion         = string.IsNullOrWhiteSpace(form.descripcion) ? null : form.descripcion,
            fecha_evento        = fechaEvento,
            fecha_inicio_ventas = fechaInicioVentas,
            fecha_fin_ventas    = fechaFinVentas,
            capacidad_total     = form.capacidad_total,
            // zonas: se pueden mapear aquí si el formulario las envía.
            // Por ahora el form no envía zonas estructuradas (usa plantilla_sala),
            // así que se deja null para crear en DRAFT y luego asignar zonas.
            zonas = null
        };

        // ── 3. Llamar a la API ─────────────────────────────────────────────

        var (ok, error, idEvento) = await _eventosApi.CreateEventoAsync(token, apiRequest, ct);

        if (!ok)
        {
            ViewBag.Error = error ?? "Ocurrió un error al crear el evento.";
            return View(form);
        }

        // ── 4. Si acción = "publicar", cambiar status después de crear ─────

        // if (form.accion == "publicar" && idEvento.HasValue)
        // {
        //     var (pubOk, pubError) = await _eventosApi.UpdateStatusAsync(
        //         token, idEvento.Value, "PUBLISHED", ct: ct);
        //
        //     if (!pubOk)
        //     {
        //         // Se creó pero no se publicó — ir al detalle con advertencia
        //         TempData["Warning"] = $"El evento se creó como borrador pero no se pudo publicar: {pubError}";
        //         return RedirectToAction(nameof(Create));
        //     }
        // }

        TempData["Success"] = "¡Evento publicado correctamente!";

        return RedirectToAction(nameof(Create));
    }

    // ── GET /Events/Detail/{id} ───────────────────────────────────────────────

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

    // ── POST /Events/ChangeStatus ─────────────────────────────────────────────

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

    // ── Helpers privados ──────────────────────────────────────────────────────

    private static bool TryParseEventoFechas(
        CreateEventoFormModel form,
        out DateTime fechaEvento,
        out DateTime fechaInicioVentas,
        out DateTime fechaFinVentas,
        out string? error)
    {
        fechaEvento = fechaInicioVentas = fechaFinVentas = default;
        error = null;

        // fecha_evento viene como "yyyy-MM-dd" + hora_evento "HH:mm"
        // if (!DateTime.TryParse($"{form.fecha_evento}T{form.hora_evento ?? "00:00"}", out fechaEvento))
        // {
        //     error = "La fecha o hora del evento no es válida.";
        //     return false;
        // }
        
        if (!DateTime.TryParse(form.fecha_evento, out fechaEvento))
        {
            error = "La fecha o hora del evento no es válida.";
            return false;
        }

        if (!DateTime.TryParse(form.fecha_inicio_ventas, out fechaInicioVentas))
        {
            error = "La fecha de inicio de ventas no es válida.";
            return false;
        }

        if (!DateTime.TryParse(form.fecha_fin_ventas, out fechaFinVentas))
        {
            error = "La fecha de cierre de ventas no es válida.";
            return false;
        }

        return true;
    }
}