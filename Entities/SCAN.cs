using System;
using System.Collections.Generic;

namespace events_admin.Entities;

public partial class SCAN
{
    public int id_scan { get; set; }

    public int id_ticket { get; set; }

    public int id_staff { get; set; }

    public DateTime? fecha_scan { get; set; }

    public string resultado { get; set; } = null!;

    public string? observacion { get; set; }

    public virtual STAFF id_staffNavigation { get; set; } = null!;

    public virtual TICKET id_ticketNavigation { get; set; } = null!;
}
