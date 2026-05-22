using System;
using System.Collections.Generic;

namespace events_admin.Entities;

public partial class ASIENTO
{
    public int id_asiento { get; set; }

    public int id_zona { get; set; }

    public string fila { get; set; } = null!;

    public int numero { get; set; }

    public string codigo_asiento { get; set; } = null!;

    public int pos_x { get; set; }

    public int pos_y { get; set; }

    public bool? activo { get; set; }

    public virtual ICollection<EVENTO_ASIENTO> EVENTO_ASIENTOs { get; set; } = new List<EVENTO_ASIENTO>();

    public virtual ZONA id_zonaNavigation { get; set; } = null!;
}
