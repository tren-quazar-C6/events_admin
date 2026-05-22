using System;
using System.Collections.Generic;

namespace events_admin.Entities;

public partial class PQR
{
    public int id_pqrs { get; set; }

    public int id_usuario { get; set; }

    public int? asignado_staff { get; set; }

    public string tipo { get; set; } = null!;

    public string asunto { get; set; } = null!;

    public string mensaje { get; set; } = null!;

    public string? estado { get; set; }

    public string? respuesta { get; set; }

    public DateTime? fecha_creacion { get; set; }

    public DateTime? fecha_respuesta { get; set; }

    public virtual STAFF? asignado_staffNavigation { get; set; }

    public virtual USUARIO id_usuarioNavigation { get; set; } = null!;
}
