using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace events_admin.Entities;

public partial class ASIENTO
{
    [Column("id_asiento")]
    public int Id { get; set; }

    [Column("id_zona")]
    public int ZoneId { get; set; }

    [Column("fila")]
    public string Row { get; set; } = null!;

    [Column("numero")]
    public int Column { get; set; }

    [Column("codigo_asiento")]
    public string SeatCode { get; set; } = null!;

    public int pos_x { get; set; }

    public int pos_y { get; set; }

    [Column("activo")]
    public bool? IsActive { get; set; }

    public virtual ICollection<EVENTO_ASIENTO> EVENTO_ASIENTOs { get; set; } = new List<EVENTO_ASIENTO>();

    public virtual ZONA id_zonaNavigation { get; set; } = null!;
}
