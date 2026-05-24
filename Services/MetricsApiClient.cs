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
            ?? _config["ASPNETCORE_URLS"]
            ?? "http://localhost:5039";
    }

    /// <summary>
    /// Get total revenue in a date range
    /// </summary>
    public async Task<decimal> GetRevenueAsync(DateTime desde, DateTime hasta)
    {
        try
        {
            string url = $"{GetApiBaseUrl()}/api/metrics/revenue?desde={desde:yyyy-MM-dd}&hasta={hasta:yyyy-MM-dd}";
            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError($"API error: {response.StatusCode}");
                return 0;
            }

            var json = await response.Content.ReadAsStringAsync();
            var doc = JsonDocument.Parse(json);
            
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
            
            if (doc.RootElement.TryGetProperty("weeks", out var weeksElement))
            {
                var weeks = new List<WeeklySalesDto>();
                foreach (var week in weeksElement.EnumerateArray())
                {
                    weeks.Add(new WeeklySalesDto
                    {
                        Semana = week.GetProperty("semana").GetInt32(),
                        Total = week.GetProperty("total").GetDecimal(),
                        Cantidad = week.GetProperty("cantidad").GetInt32()
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
            string url = $"{GetApiBaseUrl()}/api/metrics/attendance/{idEvento}";
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
                if (rateElement.ValueKind == JsonValueKind.Number)
                {
                    return rateElement.GetDouble();
                }

                if (rateElement.ValueKind == JsonValueKind.String)
                {
                    string rateStr = rateElement.GetString()?.Replace("%", "") ?? "0";
                    if (double.TryParse(rateStr, out double rate))
                    {
                        return rate > 1 ? rate / 100 : rate;
                    }
                }
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
            string url = $"{GetApiBaseUrl()}/api/metrics/dashboard?desde={desde:yyyy-MM-dd}&hasta={hasta:yyyy-MM-dd}";
            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError($"API error: {response.StatusCode}");
                return new DashboardMetricsDto { Success = false };
            }

            var json = await response.Content.ReadAsStringAsync();
            var doc = JsonDocument.Parse(json);

            var dashboard = new DashboardMetricsDto
            {
                Success = doc.RootElement.GetProperty("success").GetBoolean(),
                Desde = doc.RootElement.GetProperty("desde").GetDateTime(),
                Hasta = doc.RootElement.GetProperty("hasta").GetDateTime()
            };

            if (doc.RootElement.TryGetProperty("summary", out var summaryElement))
            {
                dashboard.TotalRevenue = summaryElement.GetProperty("totalRevenue").GetDecimal();
                dashboard.TotalTickets = summaryElement.GetProperty("totalTickets").GetInt32();
                dashboard.AveragePerTicket = summaryElement.GetProperty("averagePerTicket").GetString() ?? "N/A";
            }

            if (doc.RootElement.TryGetProperty("weeklySales", out var weeksElement))
            {
                dashboard.WeeklySales = new List<WeeklySalesDto>();
                foreach (var week in weeksElement.EnumerateArray())
                {
                    dashboard.WeeklySales.Add(new WeeklySalesDto
                    {
                        Semana = week.GetProperty("semana").GetInt32(),
                        Total = week.GetProperty("total").GetDecimal(),
                        Cantidad = week.GetProperty("cantidad").GetInt32()
                    });
                }
            }

            return dashboard;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error calling metrics API: {ex.Message}");
            return new DashboardMetricsDto { Success = false, Error = ex.Message };
        }
    }
}
