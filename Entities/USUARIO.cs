using System;
using System.Collections.Generic;

namespace events_admin.Entities;

public partial class USUARIO
{
    public int id_usuario { get; set; }

    public string nombre { get; set; } = null!;

    public string email { get; set; } = null!;

    public string? password_hash { get; set; }

    public string? telefono { get; set; }

    public string? foto_perfil { get; set; }

    public string? google_id { get; set; }

    public DateTime? fecha_registro { get; set; }

    public bool? activo { get; set; }

    public virtual ICollection<FAVORITO> FAVORITOs { get; set; } = new List<FAVORITO>();

    public virtual ICollection<NOTIFICACIONE> NOTIFICACIONEs { get; set; } = new List<NOTIFICACIONE>();

    public virtual ICollection<PQR> PQRs { get; set; } = new List<PQR>();

    public virtual ICollection<VENTA> VENTAs { get; set; } = new List<VENTA>();
}
