using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using events_admin.Models;

namespace events_admin.Services;

/// <summary>
/// Wrapper sobre el endpoint /api/admin/eventos de la API externa.
/// Siempre recibe el JWT del caller (obtenido del claim "JWToken").
/// </summary>
public class EventService
{
    private readonly IHttpClientFactory _clientFactory;
    private readonly IConfiguration _config;

    // Ruta base de la API, ej. "https://service.quasar.andrescortes.dev/"
    // private string BaseUrl => _config["ApiSettings:BaseUrl"]!.TrimEnd('/');
    private string BaseUrl => _config?["ApiSettings:BaseUrl"] ?? "https://service.quasar.andrescortes.dev/";


    public EventService(IHttpClientFactory clientFactory, IConfiguration config)
    {
        _clientFactory = clientFactory;
        _config = config;
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    /// <summary>Crea un HttpClient con el Authorization: Bearer {token} ya puesto.</summary>
    private HttpClient CreateAuthorizedClient(string jwtToken)
    {
        var client = _clientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", jwtToken);
        return client;
    }

    private static StringContent JsonBody(object payload) =>
        new(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

    // ── Listar eventos ────────────────────────────────────────────────────────

    /// <summary>
    /// GET /api/admin/eventos
    /// Retorna null si la petición falla.
    /// </summary>
    public async Task<List<AdminEventoResumenDto>?> GetEventosAsync(
        string jwtToken,
        string? busqueda = null,
        string? status = null,
        int? id_tipo_evento = null,
        CancellationToken ct = default)
    {
        var query = BuildQuery(new Dictionary<string, string?>
        {
            ["busqueda"]       = busqueda,
            ["status"]         = status,
            ["id_tipo_evento"] = id_tipo_evento?.ToString()
        });

        var client   = CreateAuthorizedClient(jwtToken);
        var response = await client.GetAsync($"{BaseUrl}/api/admin/eventos{query}", ct);

        if (!response.IsSuccessStatusCode) return null;

        var json = await response.Content.ReadAsStringAsync(ct);
        using var doc = JsonDocument.Parse(json);

        if (!doc.RootElement.GetProperty("success").GetBoolean()) return null;

        var data = doc.RootElement.GetProperty("data");
        return JsonSerializer.Deserialize<List<AdminEventoResumenDto>>(data.GetRawText(),
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    }

    // ── Detalle de evento ─────────────────────────────────────────────────────

    public async Task<AdminEventoDetalleDto?> GetEventoAsync(
        string jwtToken, int id, CancellationToken ct = default)
    {
        var client   = CreateAuthorizedClient(jwtToken);
        var response = await client.GetAsync($"{BaseUrl}/api/admin/eventos/{id}", ct);

        if (!response.IsSuccessStatusCode) return null;

        var json = await response.Content.ReadAsStringAsync(ct);
        using var doc = JsonDocument.Parse(json);

        if (!doc.RootElement.GetProperty("success").GetBoolean()) return null;

        var data = doc.RootElement.GetProperty("data");
        return JsonSerializer.Deserialize<AdminEventoDetalleDto>(data.GetRawText(),
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    }

    // ── Crear evento ──────────────────────────────────────────────────────────

    /// <summary>
    /// POST /api/admin/eventos
    /// Retorna (ok: true, error: null) o (ok: false, error: "mensaje").
    /// </summary>
    public async Task<(bool ok, string? error, int? id_evento)> CreateEventoAsync(
        string jwtToken,
        CreateEventoApiRequest request,
        CancellationToken ct = default)
    {
        var client   = CreateAuthorizedClient(jwtToken);
        var response = await client.PostAsync(
            $"{BaseUrl}/api/admin/eventos", JsonBody(request), ct);

        var json = await response.Content.ReadAsStringAsync(ct);

        try
        {
            using var doc  = JsonDocument.Parse(json);
            var root        = doc.RootElement;
            var success     = root.GetProperty("success").GetBoolean();

            if (!success)
            {
                var msg = root.TryGetProperty("message", out var m) ? m.GetString() : "Error al crear el evento";
                return (false, msg, null);
            }

            // Extraer id_evento del data devuelto
            int? idEvento = null;
            if (root.TryGetProperty("data", out var data) &&
                data.TryGetProperty("id_evento", out var idProp))
                idEvento = idProp.GetInt32();

            return (true, null, idEvento);
        }
        catch
        {
            return (false, $"Error HTTP {(int)response.StatusCode}", null);
        }
    }

    // ── Cambiar status ────────────────────────────────────────────────────────

    public async Task<(bool ok, string? error)> UpdateStatusAsync(
        string jwtToken, int id, string status, string? motivo = null, CancellationToken ct = default)
    {
        var client   = CreateAuthorizedClient(jwtToken);
        var payload  = new { status, motivo_cancelacion = motivo };
        var response = await client.PatchAsync(
            $"{BaseUrl}/api/admin/eventos/{id}/status", JsonBody(payload), ct);

        var json = await response.Content.ReadAsStringAsync(ct);
        using var doc = JsonDocument.Parse(json);
        var ok = doc.RootElement.GetProperty("success").GetBoolean();
        var msg = doc.RootElement.TryGetProperty("message", out var m) ? m.GetString() : null;

        return (ok, ok ? null : msg);
    }

    // ── Utilidades ────────────────────────────────────────────────────────────

    private static string BuildQuery(Dictionary<string, string?> args)
    {
        var parts = args
            .Where(kv => !string.IsNullOrWhiteSpace(kv.Value))
            .Select(kv => $"{Uri.EscapeDataString(kv.Key)}={Uri.EscapeDataString(kv.Value!)}");

        var qs = string.Join("&", parts);
        return qs.Length > 0 ? "?" + qs : string.Empty;
    }
}