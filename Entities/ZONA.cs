using System;
using System.Collections.Generic;

namespace events_admin.Entities;

public partial class ZONA
{
    public int id_zona { get; set; }

    public string nombre_zona { get; set; } = null!;

    public string? color_hex { get; set; }

    public bool? activo { get; set; }

    public virtual ICollection<ASIENTO> ASIENTOs { get; set; } = new List<ASIENTO>();

    public virtual ICollection<EVENTO_ZONA> EVENTO_ZONAs { get; set; } = new List<EVENTO_ZONA>();
}
