using System;
using System.Collections.Generic;

namespace events_admin.Entities;

public partial class FAVORITO
{
    public int id_favorito { get; set; }

    public int id_usuario { get; set; }

    public int id_evento { get; set; }

    public DateTime? fecha_agregado { get; set; }

    public virtual EVENTO id_eventoNavigation { get; set; } = null!;

    public virtual USUARIO id_usuarioNavigation { get; set; } = null!;
}
