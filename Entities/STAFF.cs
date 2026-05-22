using System;
using System.Collections.Generic;

namespace events_admin.Entities;

public partial class STAFF
{
    public int id_staff { get; set; }

    public int id_rol_staff { get; set; }

    public string nombre { get; set; } = null!;

    public string email { get; set; } = null!;

    public string password_hash { get; set; } = null!;

    public bool? activo { get; set; }

    public DateTime? fecha_registro { get; set; }

    public virtual ICollection<EVENTO> EVENTOs { get; set; } = new List<EVENTO>();

    public virtual ICollection<PQR> PQRs { get; set; } = new List<PQR>();

    public virtual ICollection<SCAN> SCANs { get; set; } = new List<SCAN>();

    public virtual ICollection<VENTA> VENTAs { get; set; } = new List<VENTA>();

    public virtual ROL_STAFF id_rol_staffNavigation { get; set; } = null!;
}
