using System;
using System.Collections.Generic;

namespace events_admin.Entities;

public partial class EVENTO_ASIENTO
{
    public int id_evento_asiento { get; set; }

    public int id_evento { get; set; }

    public int id_asiento { get; set; }

    public string? estado { get; set; }

    public DateTime? fecha_reserva { get; set; }

    public DateTime? reserva_expira { get; set; }

    public virtual TICKET? TICKET { get; set; }

    public virtual ASIENTO id_asientoNavigation { get; set; } = null!;

    public virtual EVENTO id_eventoNavigation { get; set; } = null!;
}
