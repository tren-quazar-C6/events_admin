using events_admin.Data;
using Microsoft.EntityFrameworkCore;

namespace events_admin.Services;

public class EventBusinessRulesService
{
    private readonly QuasarDbContext _db;
    private readonly MetricsService _metricsService;

    public EventBusinessRulesService(QuasarDbContext db, MetricsService metricsService)
    {
        _db = db;
        _metricsService = metricsService;
    }

    public async Task<SalesWindowDto> ValidateSalesWindowAsync(int idEvento, DateTime? when = null)
    {
        var now = when ?? DateTime.UtcNow;
        var evento = await _db.EVENTOs
            .Where(e => e.id_evento == idEvento)
            .Select(e => new
            {
                e.id_evento,
                e.fecha_inicio_ventas,
                e.fecha_fin_ventas,
                e.publicado,
                e.activo
            })
            .FirstOrDefaultAsync();

        if (evento is null)
        {
            return new SalesWindowDto { IdEvento = idEvento, CanSell = false, Reason = "EVENT_NOT_FOUND" };
        }

        if (evento.activo != true || evento.publicado != true)
        {
            return new SalesWindowDto { IdEvento = idEvento, CanSell = false, Reason = "EVENT_NOT_ACTIVE" };
        }

        if (now < evento.fecha_inicio_ventas)
        {
            return new SalesWindowDto { IdEvento = idEvento, CanSell = false, Reason = "SALES_NOT_OPEN" };
        }

        if (now > evento.fecha_fin_ventas)
        {
            return new SalesWindowDto { IdEvento = idEvento, CanSell = false, Reason = "SALES_CLOSED" };
        }

        return new SalesWindowDto { IdEvento = idEvento, CanSell = true, Reason = "SALES_OPEN" };
    }

    public async Task<CapacityValidationDto> ValidateCapacityAsync(int idEvento, int requestedSeats)
    {
        if (requestedSeats <= 0)
        {
            return new CapacityValidationDto
            {
                IdEvento = idEvento,
                RequestedSeats = requestedSeats,
                IsValid = false,
                Reason = "REQUESTED_SEATS_MUST_BE_POSITIVE"
            };
        }

        var occupancy = await _metricsService.GetOccupancyAsync(idEvento);
        var isValid = occupancy.AsientosDisponibles >= requestedSeats;

        return new CapacityValidationDto
        {
            IdEvento = idEvento,
            RequestedSeats = requestedSeats,
            AvailableSeats = occupancy.AsientosDisponibles,
            CapacityTotal = occupancy.CapacidadTotal,
            IsValid = isValid,
            Reason = isValid ? "CAPACITY_AVAILABLE" : "CAPACITY_EXCEEDED"
        };
    }
}

public class SalesWindowDto
{
    public int IdEvento { get; set; }
    public bool CanSell { get; set; }
    public string Reason { get; set; } = string.Empty;
}

public class CapacityValidationDto
{
    public int IdEvento { get; set; }
    public int RequestedSeats { get; set; }
    public int AvailableSeats { get; set; }
    public int CapacityTotal { get; set; }
    public bool IsValid { get; set; }
    public string Reason { get; set; } = string.Empty;
}
