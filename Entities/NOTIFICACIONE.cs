using System;
using System.Collections.Generic;

namespace events_admin.Entities;

public partial class NOTIFICACIONE
{
    public int id_notificacion { get; set; }

    public int id_usuario { get; set; }

    public string titulo { get; set; } = null!;

    public string mensaje { get; set; } = null!;

    public bool? leido { get; set; }

    public DateTime? fecha_envio { get; set; }

    public virtual USUARIO id_usuarioNavigation { get; set; } = null!;
}
