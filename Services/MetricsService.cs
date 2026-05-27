using events_admin.Data;
using Microsoft.EntityFrameworkCore;

namespace events_admin.Services;

public class MetricsService
{
    private readonly QuasarDbContext _db;

    public MetricsService(QuasarDbContext db)
    {
        _db = db;
    }

    /// Ingresos totales en un rango (solo ventas aprobadas)
    public async Task<decimal> GetRevenueTotalAsync(DateTime desde, DateTime hasta)
    {
        return await _db.VENTAs
            .Where(v => v.estado_pago == "APPROVED")
            .Where(v => v.fecha_venta >= desde && v.fecha_venta <= hasta)
            .SumAsync(v => v.total);
    }

    /// Tickets vendidos en un rango (de ventas aprobadas)
    public async Task<int> GetTicketsSoldAsync(DateTime desde, DateTime hasta)
    {
        return await _db.TICKETs
            .Where(t => t.id_ventaNavigation.estado_pago == "APPROVED")
            .Where(t => t.fecha_generacion >= desde && t.fecha_generacion <= hasta)
            .CountAsync();
    }

    /// Ventas agrupadas por semana ISO
    public async Task<List<WeeklySalesDto>> GetWeeklySalesAsync(DateTime desde, DateTime hasta)
    {
        var ventas = await _db.VENTAs
            .Where(v => v.estado_pago == "APPROVED")
            .Where(v => v.fecha_venta >= desde && v.fecha_venta <= hasta)
            .Select(v => new { v.fecha_venta, v.total, Tickets = v.TICKETs.Count })
            .ToListAsync();

        return ventas
            .GroupBy(v => new
            {
                Anio = System.Globalization.ISOWeek.GetYear(v.fecha_venta!.Value),
                Semana = System.Globalization.ISOWeek.GetWeekOfYear(v.fecha_venta!.Value)
            })
            .Select(g => new WeeklySalesDto
            {
                Anio = g.Key.Anio,
                Semana = g.Key.Semana,
                Total = g.Sum(x => x.total),
                Cantidad = g.Sum(x => x.Tickets)
            })
            .OrderBy(w => w.Anio)
            .ThenBy(w => w.Semana)
            .ToList();
    }

    /// Tasa de asistencia de un evento: scans válidos ÷ tickets emitidos
    public async Task<double> GetAttendanceRateAsync(int idEvento)
    {
        // Tickets del evento (via EVENTO_ASIENTO → EVENTO)
        var totalTickets = await _db.TICKETs
            .Where(t => t.id_evento_asientoNavigation.id_evento == idEvento)
            .CountAsync();

        if (totalTickets == 0) return 0;

        // Scans válidos de esos tickets
        var asistieron = await _db.SCANs
            .Where(s => s.resultado == "VALIDO")
            .Where(s => s.id_ticketNavigation.id_evento_asientoNavigation.id_evento == idEvento)
            .Select(s => s.id_ticket)
            .Distinct()
            .CountAsync();

        return (double)asistieron / totalTickets;
    }

    /// Ocupacion de un evento: asientos vendidos sobre capacidad del evento.
    public async Task<OccupancyDto> GetOccupancyAsync(int idEvento)
    {
        var evento = await _db.EVENTOs
            .Where(e => e.id_evento == idEvento)
            .Select(e => new { e.id_evento, e.capacidad_total })
            .FirstOrDefaultAsync();

        if (evento is null)
        {
            return new OccupancyDto { IdEvento = idEvento };
        }

        var vendidos = await _db.EVENTO_ASIENTOs
            .Where(ea => ea.id_evento == idEvento)
            .CountAsync(ea => ea.estado == "VENDIDO");

        var capacidad = evento.capacidad_total;

        return new OccupancyDto
        {
            IdEvento = idEvento,
            CapacidadTotal = capacidad,
            AsientosVendidos = vendidos,
            AsientosDisponibles = Math.Max(capacidad - vendidos, 0),
            Ocupacion = capacidad == 0 ? 0 : (double)vendidos / capacidad
        };
    }
}
