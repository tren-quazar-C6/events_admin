using System;
using System.Collections.Generic;

namespace events_admin.Entities;

public partial class ROL_STAFF
{
    public int id_rol_staff { get; set; }

    public string nombre_rol { get; set; } = null!;

    public bool? activo { get; set; }

    public virtual ICollection<STAFF> STAFF { get; set; } = new List<STAFF>();
}
