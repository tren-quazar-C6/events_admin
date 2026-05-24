using events_admin.Data;
using events_admin.Entities;
using Microsoft.EntityFrameworkCore;

namespace events_admin.Services;

public class NotificationTriggerService
{
    private readonly QuasarDbContext _db;

    public NotificationTriggerService(QuasarDbContext db)
    {
        _db = db;
    }

    public async Task<NotificationTriggerDto> TriggerFavoriteUpdateAsync(int idUsuario, int idEvento)
    {
        var favorite = await _db.FAVORITOs
            .Include(f => f.id_eventoNavigation)
            .FirstOrDefaultAsync(f => f.id_usuario == idUsuario && f.id_evento == idEvento);

        if (favorite is null)
        {
            return new NotificationTriggerDto
            {
                Sent = false,
                Reason = "FAVORITE_NOT_FOUND"
            };
        }

        var notification = new NOTIFICACIONE
        {
            id_usuario = idUsuario,
            titulo = "Actualizacion de favorito",
            mensaje = $"Hay novedades del evento favorito: {favorite.id_eventoNavigation.nombre_evento}.",
            leido = false,
            fecha_envio = DateTime.UtcNow
        };

        _db.NOTIFICACIONEs.Add(notification);
        await _db.SaveChangesAsync();

        return new NotificationTriggerDto
        {
            Sent = true,
            Reason = "FAVORITE_NOTIFICATION_CREATED",
            IdNotificacion = notification.id_notificacion
        };
    }

    public async Task<NotificationTriggerDto> TriggerPqrsNotificationAsync(int idPqrs, string message)
    {
        var pqr = await _db.PQRs.FirstOrDefaultAsync(p => p.id_pqrs == idPqrs);

        if (pqr is null)
        {
            return new NotificationTriggerDto { Sent = false, Reason = "PQRS_NOT_FOUND" };
        }

        var notification = new NOTIFICACIONE
        {
            id_usuario = pqr.id_usuario,
            titulo = $"PQRS {pqr.estado ?? "ACTUALIZADA"}",
            mensaje = string.IsNullOrWhiteSpace(message) ? $"Tu PQRS '{pqr.asunto}' fue actualizada." : message,
            leido = false,
            fecha_envio = DateTime.UtcNow
        };

        _db.NOTIFICACIONEs.Add(notification);
        await _db.SaveChangesAsync();

        return new NotificationTriggerDto
        {
            Sent = true,
            Reason = "PQRS_NOTIFICATION_CREATED",
            IdNotificacion = notification.id_notificacion
        };
    }
}

public class NotificationTriggerDto
{
    public bool Sent { get; set; }
    public string Reason { get; set; } = string.Empty;
    public int? IdNotificacion { get; set; }
}
