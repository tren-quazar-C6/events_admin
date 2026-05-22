using System;
using System.Collections.Generic;

namespace events_admin.Entities;

public partial class ESTADO_TICKET
{
    public int id_estado_ticket { get; set; }

    public string nombre_estado { get; set; } = null!;

    public virtual ICollection<TICKET> TICKETs { get; set; } = new List<TICKET>();
}
