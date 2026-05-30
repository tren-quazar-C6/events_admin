using System.Text.Json;

namespace events_admin.Services;

/// <summary>
/// Client to call the Metrics API endpoints
/// This allows the Admin module to get metrics without querying the DB directly
/// </summary>
public class MetricsApiClient
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _config;
    private readonly ILogger<MetricsApiClient> _logger;

    public MetricsApiClient(HttpClient httpClient, IConfiguration config, ILogger<MetricsApiClient> logger)
    {
        _httpClient = httpClient;
        _config = config;
        _logger = logger;
    }

    private string GetApiBaseUrl()
    {
        return _config["ApiSettings:BaseUrl"]
            ?? "http://localhost:5114";
    }

    /// <summary>
    /// Get total revenue in a date range
    /// </summary>
    public async Task<decimal> GetRevenueAsync(DateTime desde, DateTime hasta)
    {
        try
        {
            string url = $"{GetApiBaseUrl()}/api/metrics/revenue-total?desde={desde:yyyy-MM-dd}&hasta={hasta:yyyy-MM-dd}";
            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError($"API error: {response.StatusCode}");
                return 0;
            }

            var json = await response.Content.ReadAsStringAsync();
            var doc = JsonDocument.Parse(json);
            
            if (doc.RootElement.TryGetProperty("valor", out var valorElement))
            {
                return valorElement.GetDecimal();
            }

            if (doc.RootElement.TryGetProperty("total", out var totalElement))
            {
                return totalElement.GetDecimal();
            }

            return 0;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error calling metrics API: {ex.Message}");
            return 0;
        }
    }

    /// <summary>
    /// Get number of tickets sold in a date range
    /// </summary>
    public async Task<int> GetTicketsSoldAsync(DateTime desde, DateTime hasta)
    {
        try
        {
            string url = $"{GetApiBaseUrl()}/api/metrics/tickets-sold?desde={desde:yyyy-MM-dd}&hasta={hasta:yyyy-MM-dd}";
            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError($"API error: {response.StatusCode}");
                return 0;
            }

            var json = await response.Content.ReadAsStringAsync();
            var doc = JsonDocument.Parse(json);

            if (doc.RootElement.TryGetProperty("valor", out var valorElement))
            {
                return valorElement.GetInt32();
            }

            if (doc.RootElement.TryGetProperty("ticketsSold", out var ticketsElement))
            {
                return ticketsElement.GetInt32();
            }

            return 0;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error calling metrics API: {ex.Message}");
            return 0;
        }
    }

    /// <summary>
    /// Get weekly sales breakdown
    /// </summary>
    public async Task<List<WeeklySalesDto>> GetWeeklySalesAsync(DateTime desde, DateTime hasta)
    {
        try
        {
            string url = $"{GetApiBaseUrl()}/api/metrics/weekly-sales?desde={desde:yyyy-MM-dd}&hasta={hasta:yyyy-MM-dd}";
            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError($"API error: {response.StatusCode}");
                return new List<WeeklySalesDto>();
            }

            var json = await response.Content.ReadAsStringAsync();
            var doc = JsonDocument.Parse(json);
            
            var weeksElement = doc.RootElement;
            if (doc.RootElement.ValueKind == JsonValueKind.Object
                && doc.RootElement.TryGetProperty("weeks", out var wrappedWeeksElement))
            {
                weeksElement = wrappedWeeksElement;
            }

            if (weeksElement.ValueKind == JsonValueKind.Array)
            {
                var weeks = new List<WeeklySalesDto>();
                foreach (var week in weeksElement.EnumerateArray())
                {
                    weeks.Add(new WeeklySalesDto
                    {
                        Anio = GetOptionalInt32(week, "anio"),
                        Semana = GetOptionalInt32(week, "semana", "week"),
                        Total = GetOptionalDecimal(week, "total", "revenue"),
                        Cantidad = GetOptionalInt32(week, "cantidad")
                    });
                }
                return weeks;
            }

            return new List<WeeklySalesDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error calling metrics API: {ex.Message}");
            return new List<WeeklySalesDto>();
        }
    }

    /// <summary>
    /// Get attendance rate for an event
    /// </summary>
    public async Task<double> GetAttendanceRateAsync(int idEvento)
    {
        try
        {
            string url = $"{GetApiBaseUrl()}/api/metrics/eventos/{idEvento}/attendance-rate";
            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError($"API error: {response.StatusCode}");
                return 0;
            }

            var json = await response.Content.ReadAsStringAsync();
            var doc = JsonDocument.Parse(json);
            
            if (doc.RootElement.TryGetProperty("attendanceRate", out var rateElement))
            {
                return ParseRate(rateElement);
            }

            if (doc.RootElement.TryGetProperty("tasaAsistencia", out var tasaElement))
            {
                return ParseRate(tasaElement);
            }

            return 0;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error calling metrics API: {ex.Message}");
            return 0;
        }
    }

    /// <summary>
    /// Get complete dashboard metrics in one call
    /// </summary>
    public async Task<DashboardMetricsDto> GetDashboardAsync(DateTime desde, DateTime hasta)
    {
        try
        {
            var totalRevenue = await GetMetricValueAsync<decimal>(
                $"/api/metrics/revenue-total?desde={desde:yyyy-MM-dd}&hasta={hasta:yyyy-MM-dd}",
                value => value.GetDecimal());
            var totalTickets = await GetMetricValueAsync<int>(
                $"/api/metrics/tickets-sold?desde={desde:yyyy-MM-dd}&hasta={hasta:yyyy-MM-dd}",
                value => value.GetInt32());
            var weeklySales = await GetWeeklySalesFromApiAsync(desde, hasta);

            return new DashboardMetricsDto
            {
                Success = true,
                Desde = desde.Date,
                Hasta = hasta.Date,
                TotalRevenue = totalRevenue,
                TotalTickets = totalTickets,
                AveragePerTicket = totalTickets == 0 ? "N/A" : (totalRevenue / totalTickets).ToString("N0", new System.Globalization.CultureInfo("es-CO")),
                WeeklySales = weeklySales
            };
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error calling metrics API: {ex.Message}");
            return new DashboardMetricsDto { Success = false, Error = ex.Message };
        }
    }

    private async Task<T> GetMetricValueAsync<T>(string pathAndQuery, Func<JsonElement, T> parse)
    {
        var response = await _httpClient.GetAsync($"{GetApiBaseUrl()}{pathAndQuery}");
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json);

        if (!doc.RootElement.TryGetProperty("valor", out var valorElement))
        {
            throw new InvalidOperationException("Metrics API response does not include 'valor'.");
        }

        return parse(valorElement);
    }

    private async Task<List<WeeklySalesDto>> GetWeeklySalesFromApiAsync(DateTime desde, DateTime hasta)
    {
        var response = await _httpClient.GetAsync(
            $"{GetApiBaseUrl()}/api/metrics/weekly-sales?desde={desde:yyyy-MM-dd}&hasta={hasta:yyyy-MM-dd}");
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json);
        var weeks = new List<WeeklySalesDto>();

        if (doc.RootElement.ValueKind != JsonValueKind.Array)
        {
            throw new InvalidOperationException("Metrics API weekly sales response is not an array.");
        }

        foreach (var week in doc.RootElement.EnumerateArray())
        {
            weeks.Add(new WeeklySalesDto
            {
                Anio = GetOptionalInt32(week, "anio"),
                Semana = GetOptionalInt32(week, "semana", "week"),
                Total = GetOptionalDecimal(week, "total", "revenue"),
                Cantidad = GetOptionalInt32(week, "cantidad")
            });
        }

        return weeks;
    }

    private static int GetOptionalInt32(JsonElement element, params string[] propertyNames)
    {
        foreach (var propertyName in propertyNames)
        {
            if (element.TryGetProperty(propertyName, out var property) && property.ValueKind == JsonValueKind.Number)
            {
                return property.GetInt32();
            }
        }

        return 0;
    }

    private static decimal GetOptionalDecimal(JsonElement element, params string[] propertyNames)
    {
        foreach (var propertyName in propertyNames)
        {
            if (element.TryGetProperty(propertyName, out var property) && property.ValueKind == JsonValueKind.Number)
            {
                return property.GetDecimal();
            }
        }

        return 0;
    }

    private static double ParseRate(JsonElement rateElement)
    {
        if (rateElement.ValueKind == JsonValueKind.Number)
        {
            var rate = rateElement.GetDouble();
            return rate > 1 ? rate / 100 : rate;
        }

        if (rateElement.ValueKind == JsonValueKind.String)
        {
            string rateStr = rateElement.GetString()?.Replace("%", "") ?? "0";
            if (double.TryParse(rateStr, out double rate))
            {
                return rate > 1 ? rate / 100 : rate;
            }
        }

        return 0;
    }
}
