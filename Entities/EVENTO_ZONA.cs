using System;
using System.Collections.Generic;

namespace events_admin.Entities;

public partial class EVENTO_ZONA
{
    public int id_evento_zona { get; set; }

    public int id_evento { get; set; }

    public int id_zona { get; set; }

    public decimal precio { get; set; }

    public decimal? cargo_servicio { get; set; }

    public int capacidad { get; set; }

    public bool? activo { get; set; }

    public virtual EVENTO id_eventoNavigation { get; set; } = null!;

    public virtual ZONA id_zonaNavigation { get; set; } = null!;
}
