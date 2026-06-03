namespace events_admin.Models;

// ── Respuestas de la API ──────────────────────────────────────────────────────

public record AdminEventoResumenDto(
    int id_evento,
    string nombre_evento,
    DateTime fecha_evento,
    DateTime fecha_inicio_ventas,
    DateTime fecha_fin_ventas,
    int capacidad_total,
    string tipo_evento,
    string? ruta_url,
    string status, // "DRAFT" | "PUBLISHED" | "CANCELLED"
    int total_zonas
);

public record AdminEventoDetalleDto(
    int id_evento,
    string nombre_evento,
    string? descripcion,
    DateTime fecha_evento,
    DateTime fecha_inicio_ventas,
    DateTime fecha_fin_ventas,
    DateTime fecha_creacion,
    int capacidad_total,
    int id_tipo_evento,
    string tipo_evento,
    string status,
    DateTime? fecha_cancelacion,
    string? motivo_cancelacion,
    string? ruta_url,
    List<EventoZonaDto> zonas,
    int disponibles,
    int reservados,
    int vendidos
);

public record ImagenEventoDto(int id_imagen, string ruta_url, bool principal);

public record EventoZonaDto(
    int id_evento_zona,
    int id_zona,
    string nombre_zona,
    string? color_hex,
    decimal precio,
    decimal cargo_servicio,
    int capacidad,
    bool activo
);

// ── Request que se manda a POST /api/admin/eventos ────────────────────────────

public class CreateEventoApiRequest
{
    public int id_tipo_evento { get; set; }
    public int creado_por_staff { get; set; }
    public string nombre_evento { get; set; } = "";
    public string? descripcion { get; set; }
    public DateTime fecha_evento { get; set; }
    public DateTime fecha_inicio_ventas { get; set; }
    public DateTime fecha_fin_ventas { get; set; }
    public int capacidad_total { get; set; }
    public string ruta_url { get; set; }
    public List<ZonaEventoRequest>? zonas { get; set; }
}

public class ZonaEventoRequest
{
    public int id_zona { get; set; }
    public decimal precio { get; set; }
    public decimal? cargo_servicio { get; set; }
    public int capacidad { get; set; }
}

// ── ViewModel para el formulario de creación (lo que llega del form HTML) ─────

public class CreateEventoFormModel
{
    public string nombre_evento { get; set; } = "";
    public int id_tipo_evento { get; set; }
    public string? descripcion { get; set; }

    // Fecha + hora separadas del form
    public string? fecha_evento { get; set; } // "2025-09-15"
    public string? hora_evento { get; set; } // "20:00"

    public string? fecha_inicio_ventas { get; set; } // "2025-07-01T09:00"
    public string? fecha_fin_ventas { get; set; } // "2025-09-14T23:59"

    public int capacidad_total { get; set; }
    public string? accion { get; set; } // "borrador" | "publicar"

    // Imagen de portada (opcional — se maneja por separado si hay upload service)
    public string ruta_url { get; set; }
}