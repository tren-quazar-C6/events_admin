using System;
using System.Collections.Generic;

namespace events_admin.Entities;

public partial class VENTA
{
    public int id_venta { get; set; }

    public int id_usuario { get; set; }

    public int? id_staff { get; set; }

    public string tipo_venta { get; set; } = null!;

    public decimal total { get; set; }

    public string? moneda { get; set; }

    public string? estado_pago { get; set; }

    public string? metodo_pago { get; set; }

    public string? referencia_interna { get; set; }

    public string? referencia_wompi { get; set; }

    public string? id_transaccion_wompi { get; set; }

    public string? json_respuesta { get; set; }

    public DateTime? fecha_pago { get; set; }

    public DateTime? fecha_venta { get; set; }

    public virtual ICollection<TICKET> TICKETs { get; set; } = new List<TICKET>();

    public virtual ICollection<TRANSACCIONES_PAGO> TRANSACCIONES_PAGOs { get; set; } = new List<TRANSACCIONES_PAGO>();

    public virtual STAFF? id_staffNavigation { get; set; }

    public virtual USUARIO id_usuarioNavigation { get; set; } = null!;
}
