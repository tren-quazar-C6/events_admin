using System;
using System.Collections.Generic;

namespace events_admin.Entities;

public partial class EVENTO
{
    public int id_evento { get; set; }

    public int id_tipo_evento { get; set; }

    public int creado_por_staff { get; set; }

    public string nombre_evento { get; set; } = null!;

    public string? descripcion { get; set; }

    public DateTime fecha_evento { get; set; }

    public DateTime fecha_inicio_ventas { get; set; }

    public DateTime fecha_fin_ventas { get; set; }

    public int capacidad_total { get; set; }

    public bool? publicado { get; set; }

    public bool? activo { get; set; }

    public DateTime? fecha_creacion { get; set; }

    public DateTime? fecha_cancelacion { get; set; }

    public string? motivo_cancelacion { get; set; }

    public virtual ICollection<EVENTO_ASIENTO> EVENTO_ASIENTOs { get; set; } = new List<EVENTO_ASIENTO>();

    public virtual ICollection<FAVORITO> FAVORITOs { get; set; } = new List<FAVORITO>();

    public virtual STAFF creado_por_staffNavigation { get; set; } = null!;

    public virtual TIPO_EVENTO id_tipo_eventoNavigation { get; set; } = null!;
}
