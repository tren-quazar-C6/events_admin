using System;
using System.Collections.Generic;

namespace events_admin.Entities;

public partial class TIPO_EVENTO
{
    public int id_tipo_evento { get; set; }

    public string nombre_tipo { get; set; } = null!;

    public bool? activo { get; set; }

    public virtual ICollection<EVENTO> EVENTOs { get; set; } = new List<EVENTO>();
}
