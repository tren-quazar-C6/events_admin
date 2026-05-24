namespace events_admin.Services;

public class WeeklySalesDto
{
    public int Semana { get; set; }
    public decimal Total { get; set; }
    public int Cantidad { get; set; }
}

public class OccupancyDto
{
    public int IdEvento { get; set; }
    public int CapacidadTotal { get; set; }
    public int AsientosVendidos { get; set; }
    public int AsientosDisponibles { get; set; }
    public double Ocupacion { get; set; }
}

public class DashboardMetricsDto
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public DateTime Desde { get; set; }
    public DateTime Hasta { get; set; }
    public decimal TotalRevenue { get; set; }
    public int TotalTickets { get; set; }
    public string AveragePerTicket { get; set; } = "N/A";
    public List<WeeklySalesDto> WeeklySales { get; set; } = new();
}
