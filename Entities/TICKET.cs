using System;
using System.Collections.Generic;

namespace events_admin.Entities;

public partial class TICKET
{
    public int id_ticket { get; set; }

    public int id_venta { get; set; }

    public int id_estado_ticket { get; set; }

    public int id_evento_asiento { get; set; }

    public string codigo_unico { get; set; } = null!;

    public string qr_token { get; set; } = null!;

    public DateTime? fecha_generacion { get; set; }

    public virtual ICollection<SCAN> SCANs { get; set; } = new List<SCAN>();

    public virtual ESTADO_TICKET id_estado_ticketNavigation { get; set; } = null!;

    public virtual EVENTO_ASIENTO id_evento_asientoNavigation { get; set; } = null!;

    public virtual VENTA id_ventaNavigation { get; set; } = null!;
}
