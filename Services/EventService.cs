using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using events_admin.Models;

namespace events_admin.Services;

/// <summary>
/// Wrapper over the external /api/admin/eventos endpoint.
/// </summary>
public class EventService
{
    private readonly IHttpClientFactory _clientFactory;
    private readonly IConfiguration _config;

    private string BaseUrl => _config?["ApiSettings:BaseUrl"] ?? "http://localhost:5114";

    public EventService(IHttpClientFactory clientFactory, IConfiguration config)
    {
        _clientFactory = clientFactory;
        _config = config;
    }

    private HttpClient CreateAuthorizedClient(string jwtToken)
    {
        var client = _clientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", jwtToken);
        return client;
    }

    private static StringContent JsonBody(object payload) =>
        new(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

    public async Task<List<AdminEventoResumenDto>?> GetEventosAsync(
        string jwtToken,
        string? busqueda = null,
        string? status = null,
        int? id_tipo_evento = null,
        CancellationToken ct = default)
    {
        var query = BuildQuery(new Dictionary<string, string?>
        {
            ["busqueda"] = busqueda,
            ["status"] = status,
            ["id_tipo_evento"] = id_tipo_evento?.ToString()
        });

        var client = CreateAuthorizedClient(jwtToken);
        var response = await client.GetAsync($"{BaseUrl}/api/admin/eventos{query}", ct);

        if (!response.IsSuccessStatusCode) return null;

        var json = await response.Content.ReadAsStringAsync(ct);
        using var doc = JsonDocument.Parse(json);

        if (!doc.RootElement.GetProperty("success").GetBoolean()) return null;

        var data = doc.RootElement.GetProperty("data");
        return ParseEventos(data);
    }

    public async Task<AdminEventoDetalleDto?> GetEventoAsync(
        string jwtToken, int id, CancellationToken ct = default)
    {
        var client = CreateAuthorizedClient(jwtToken);
        var response = await client.GetAsync($"{BaseUrl}/api/admin/eventos/{id}", ct);

        if (!response.IsSuccessStatusCode) return null;

        var json = await response.Content.ReadAsStringAsync(ct);
        using var doc = JsonDocument.Parse(json);

        if (!doc.RootElement.GetProperty("success").GetBoolean()) return null;

        var data = doc.RootElement.GetProperty("data");
        return ParseEventoDetalle(data);
    }

    public async Task<(bool ok, string? error, int? id_evento)> CreateEventoAsync(
        string jwtToken,
        CreateEventoApiRequest request,
        CancellationToken ct = default)
    {
        var client = CreateAuthorizedClient(jwtToken);
        var response = await client.PostAsync(
            $"{BaseUrl}/api/admin/eventos", JsonBody(request), ct);

        var json = await response.Content.ReadAsStringAsync(ct);
        return ParseWriteResponse(response.StatusCode, json, "Error al crear el evento");
    }

    public async Task<(bool ok, string? error)> UpdateEventoAsync(
        string jwtToken,
        int id,
        CreateEventoApiRequest request,
        CancellationToken ct = default)
    {
        var client = CreateAuthorizedClient(jwtToken);
        var url = $"{BaseUrl}/api/admin/eventos/{id}";

        var response = await client.PutAsync(url, JsonBody(request), ct);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound ||
            response.StatusCode == System.Net.HttpStatusCode.MethodNotAllowed)
        {
            response = await client.PatchAsync(url, JsonBody(request), ct);
        }

        var json = await response.Content.ReadAsStringAsync(ct);
        var (ok, error, _) = ParseWriteResponse(response.StatusCode, json, "Error al actualizar el evento");
        return (ok, error);
    }

    public async Task<(bool ok, string? error)> UpdateStatusAsync(
        string jwtToken, int id, string status, string? motivo = null, CancellationToken ct = default)
    {
        var client = CreateAuthorizedClient(jwtToken);
        var payload = new { status, motivo_cancelacion = motivo };
        var response = await client.PatchAsync(
            $"{BaseUrl}/api/admin/eventos/{id}/status", JsonBody(payload), ct);

        var json = await response.Content.ReadAsStringAsync(ct);
        return ParseStatusResponse(response.StatusCode, json);
    }

    public async Task<(bool ok, string? error)> UpdateEventoZonasAsync(
        string jwtToken,
        int id,
        IReadOnlyCollection<ZonaEventoRequest> zonas,
        CancellationToken ct = default)
    {
        var client = CreateAuthorizedClient(jwtToken);
        var payload = new { zonas };
        var response = await client.PutAsync(
            $"{BaseUrl}/api/admin/eventos/{id}/zonas", JsonBody(payload), ct);

        var json = await response.Content.ReadAsStringAsync(ct);
        var (ok, error, _) = ParseWriteResponse(response.StatusCode, json, "Error al actualizar las zonas del evento");
        return (ok, error);
    }

    private static (bool ok, string? error, int? id_evento) ParseWriteResponse(
        System.Net.HttpStatusCode statusCode,
        string json,
        string defaultError)
    {
        var isSuccessStatus = (int)statusCode >= 200 && (int)statusCode <= 299;

        if (string.IsNullOrWhiteSpace(json))
        {
            return isSuccessStatus ? (true, null, null) : (false, $"Error HTTP {(int)statusCode}", null);
        }

        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (root.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
            {
                return isSuccessStatus
                    ? (true, null, null)
                    : (false, $"Error HTTP {(int)statusCode}", null);
            }

            if (root.ValueKind != JsonValueKind.Object)
            {
                if (isSuccessStatus)
                {
                    return (true, null, null);
                }

                var raw = root.ToString();
                return (false, string.IsNullOrWhiteSpace(raw) ? $"Error HTTP {(int)statusCode}" : raw, null);
            }

            var hasSuccessField = root.TryGetProperty("success", out var successProp);
            var success = hasSuccessField ? ReadBoolean(successProp, defaultValue: isSuccessStatus) : isSuccessStatus;

            var msg = ExtractErrorMessage(root);

            if (!success && string.IsNullOrWhiteSpace(msg))
            {
                msg = defaultError;
            }

            if (!success)
            {
                return (false, msg, null);
            }

            int? idEvento = null;
            if (root.TryGetProperty("data", out var data) &&
                data.ValueKind == JsonValueKind.Object &&
                TryGetPropertyAny(data, out var idProp, "id_evento", "idEvento", "IdEvento"))
            {
                idEvento = idProp.GetInt32();
            }

            return (true, null, idEvento);
        }
        catch (JsonException)
        {
            var raw = json.Trim();

            if (isSuccessStatus)
            {
                if (string.IsNullOrWhiteSpace(raw) ||
                    raw.Equals("ok", StringComparison.OrdinalIgnoreCase) ||
                    raw.Equals("success", StringComparison.OrdinalIgnoreCase))
                {
                    return (true, null, null);
                }

                if (raw.StartsWith("<", StringComparison.Ordinal))
                {
                    return (false, "La API respondió con una página inesperada.", null);
                }

                return (false, raw, null);
            }

            return (false, $"Error HTTP {(int)statusCode}", null);
        }
    }

    private static (bool ok, string? error) ParseStatusResponse(
        System.Net.HttpStatusCode statusCode,
        string json)
    {
        var isSuccessStatus = (int)statusCode >= 200 && (int)statusCode <= 299;

        if (string.IsNullOrWhiteSpace(json))
        {
            return isSuccessStatus ? (true, null) : (false, $"Error HTTP {(int)statusCode}");
        }

        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (root.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
                return isSuccessStatus ? (true, null) : (false, $"Error HTTP {(int)statusCode}");

            if (root.ValueKind != JsonValueKind.Object)
                return (isSuccessStatus, isSuccessStatus ? null : root.ToString());

            var ok = root.TryGetProperty("success", out var successProp)
                ? ReadBoolean(successProp, isSuccessStatus)
                : isSuccessStatus;

            var msg = ExtractErrorMessage(root);
            return (ok, ok ? null : msg);
        }
        catch (JsonException)
        {
            return isSuccessStatus ? (true, null) : (false, $"Error HTTP {(int)statusCode}");
        }
    }

    private static bool ReadBoolean(JsonElement element, bool defaultValue)
    {
        return element.ValueKind switch
        {
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.String when bool.TryParse(element.GetString(), out var parsed) => parsed,
            _ => defaultValue
        };
    }

    private static string? ExtractErrorMessage(JsonElement root)
    {
        var candidates = new List<string?>();

        if (root.TryGetProperty("message", out var messageProp))
            candidates.Add(messageProp.GetString());

        if (root.TryGetProperty("error", out var errorProp))
            candidates.Add(errorProp.GetString());

        if (root.TryGetProperty("detail", out var detailProp))
            candidates.Add(detailProp.GetString());

        if (root.TryGetProperty("errors", out var errorsProp))
        {
            if (errorsProp.ValueKind == JsonValueKind.Array)
            {
                candidates.AddRange(errorsProp.EnumerateArray().Select(GetScalarString));
            }
            else if (errorsProp.ValueKind == JsonValueKind.Object)
            {
                foreach (var prop in errorsProp.EnumerateObject())
                {
                    candidates.Add(GetScalarString(prop.Value));
                }
            }
        }

        if (root.TryGetProperty("data", out var dataProp) &&
            dataProp.ValueKind == JsonValueKind.Object)
        {
            if (dataProp.TryGetProperty("message", out var dataMessage))
                candidates.Add(dataMessage.GetString());

            if (dataProp.TryGetProperty("error", out var dataError))
                candidates.Add(dataError.GetString());

            if (dataProp.TryGetProperty("errors", out var dataErrors))
            {
                if (dataErrors.ValueKind == JsonValueKind.Array)
                    candidates.AddRange(dataErrors.EnumerateArray().Select(GetScalarString));
                else if (dataErrors.ValueKind == JsonValueKind.Object)
                    candidates.AddRange(dataErrors.EnumerateObject().Select(p => GetScalarString(p.Value)));
            }
        }

        var lines = candidates
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Select(s => s!.Trim())
            .Distinct()
            .ToList();

        return lines.Count == 0 ? null : string.Join(Environment.NewLine, lines);
    }

    private static string? GetScalarString(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number => element.ToString(),
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            JsonValueKind.Null => null,
            JsonValueKind.Undefined => null,
            _ => element.ToString()
        };
    }

    private static string BuildQuery(Dictionary<string, string?> args)
    {
        var parts = args
            .Where(kv => !string.IsNullOrWhiteSpace(kv.Value))
            .Select(kv => $"{Uri.EscapeDataString(kv.Key)}={Uri.EscapeDataString(kv.Value!)}");

        var qs = string.Join("&", parts);
        return qs.Length > 0 ? "?" + qs : string.Empty;
    }

    private static List<AdminEventoResumenDto> ParseEventos(JsonElement data)
    {
        if (data.ValueKind != JsonValueKind.Array)
            return new List<AdminEventoResumenDto>();

        var eventos = new List<AdminEventoResumenDto>();
        foreach (var item in data.EnumerateArray())
        {
            eventos.Add(new AdminEventoResumenDto(
                GetInt32Any(item, "id_evento", "idEvento", "IdEvento"),
                GetStringAny(item, "nombre_evento", "nombreEvento", "NombreEvento") ?? string.Empty,
                GetDateTimeAny(item, "fecha_evento", "fechaEvento", "FechaEvento"),
                GetDateTimeAny(item, "fecha_inicio_ventas", "fechaInicioVentas", "FechaInicioVentas"),
                GetDateTimeAny(item, "fecha_fin_ventas", "fechaFinVentas", "FechaFinVentas"),
                GetInt32Any(item, "capacidad_total", "capacidadTotal", "CapacidadTotal"),
                GetStringAny(item, "tipo_evento", "tipoEvento", "TipoEvento") ?? string.Empty,
                GetStringAny(item, "ruta_url", "rutaUrl", "RutaUrl"),
                GetStringAny(item, "status", "Status") ?? string.Empty,
                GetInt32Any(item, "total_zonas", "totalZonas", "TotalZonas")));
        }

        return eventos;
    }

    private static AdminEventoDetalleDto? ParseEventoDetalle(JsonElement data)
    {
        if (data.ValueKind != JsonValueKind.Object)
            return null;

        var zonas = new List<EventoZonaDto>();
        if (TryGetPropertyAny(data, out var zonasProp, "zonas", "Zonas") && zonasProp.ValueKind == JsonValueKind.Array)
        {
            foreach (var zona in zonasProp.EnumerateArray())
            {
                zonas.Add(new EventoZonaDto(
                    GetInt32Any(zona, "id_evento_zona", "idEventoZona", "IdEventoZona"),
                    GetInt32Any(zona, "id_zona", "idZona", "IdZona"),
                    GetStringAny(zona, "nombre_zona", "nombreZona", "NombreZona") ?? string.Empty,
                    GetStringAny(zona, "color_hex", "colorHex", "ColorHex"),
                    GetDecimalAny(zona, "precio", "Precio"),
                    GetDecimalAny(zona, "cargo_servicio", "cargoServicio", "CargoServicio"),
                    GetInt32Any(zona, "capacidad", "Capacidad"),
                    GetBoolAny(zona, "activo", "Activo")));
            }
        }

        return new AdminEventoDetalleDto(
            GetInt32Any(data, "id_evento", "idEvento", "IdEvento"),
            GetStringAny(data, "nombre_evento", "nombreEvento", "NombreEvento") ?? string.Empty,
            GetStringAny(data, "descripcion", "Descripcion"),
            GetDateTimeAny(data, "fecha_evento", "fechaEvento", "FechaEvento"),
            GetDateTimeAny(data, "fecha_inicio_ventas", "fechaInicioVentas", "FechaInicioVentas"),
            GetDateTimeAny(data, "fecha_fin_ventas", "fechaFinVentas", "FechaFinVentas"),
            GetDateTimeAny(data, "fecha_creacion", "fechaCreacion", "FechaCreacion"),
            GetInt32Any(data, "capacidad_total", "capacidadTotal", "CapacidadTotal"),
            GetInt32Any(data, "id_tipo_evento", "idTipoEvento", "IdTipoEvento"),
            GetStringAny(data, "tipo_evento", "tipoEvento", "TipoEvento") ?? string.Empty,
            GetStringAny(data, "status", "Status") ?? string.Empty,
            GetNullableDateTimeAny(data, "fecha_cancelacion", "fechaCancelacion", "FechaCancelacion"),
            GetStringAny(data, "motivo_cancelacion", "motivoCancelacion", "MotivoCancelacion"),
            GetStringAny(data, "ruta_url", "rutaUrl", "RutaUrl"),
            zonas,
            GetInt32Any(data, "asientos_disponibles", "disponibles", "asientosDisponibles", "AsientosDisponibles"),
            GetInt32Any(data, "asientos_reservados", "reservados", "asientosReservados", "AsientosReservados"),
            GetInt32Any(data, "asientos_vendidos", "vendidos", "asientosVendidos", "AsientosVendidos"));
    }

    private static bool TryGetPropertyAny(JsonElement element, out JsonElement value, params string[] names)
    {
        foreach (var name in names)
        {
            if (element.TryGetProperty(name, out value))
                return true;
        }

        value = default;
        return false;
    }

    private static string? GetStringAny(JsonElement element, params string[] names)
    {
        return TryGetPropertyAny(element, out var prop, names) ? prop.GetString() : null;
    }

    private static int GetInt32Any(JsonElement element, params string[] names)
    {
        if (!TryGetPropertyAny(element, out var prop, names))
            return 0;

        return prop.ValueKind switch
        {
            JsonValueKind.Number => prop.GetInt32(),
            JsonValueKind.String when int.TryParse(prop.GetString(), out var parsed) => parsed,
            _ => 0
        };
    }

    private static decimal GetDecimalAny(JsonElement element, params string[] names)
    {
        if (!TryGetPropertyAny(element, out var prop, names))
            return 0m;

        return prop.ValueKind switch
        {
            JsonValueKind.Number => prop.GetDecimal(),
            JsonValueKind.String when decimal.TryParse(prop.GetString(), out var parsed) => parsed,
            _ => 0m
        };
    }

    private static DateTime GetDateTimeAny(JsonElement element, params string[] names)
    {
        if (!TryGetPropertyAny(element, out var prop, names))
            return default;

        return prop.ValueKind switch
        {
            JsonValueKind.String when DateTime.TryParse(prop.GetString(), out var parsed) => parsed,
            JsonValueKind.Number when prop.TryGetInt64(out var unixMs) => DateTimeOffset.FromUnixTimeMilliseconds(unixMs).DateTime,
            _ => prop.GetDateTime()
        };
    }

    private static DateTime? GetNullableDateTimeAny(JsonElement element, params string[] names)
    {
        if (!TryGetPropertyAny(element, out var prop, names) || prop.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
            return null;

        return GetDateTimeAny(element, names);
    }

    private static bool GetBoolAny(JsonElement element, params string[] names)
    {
        if (!TryGetPropertyAny(element, out var prop, names))
            return false;

        return prop.ValueKind switch
        {
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.String when bool.TryParse(prop.GetString(), out var parsed) => parsed,
            _ => false
        };
    }
}
