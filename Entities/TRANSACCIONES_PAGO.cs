using System;
using System.Collections.Generic;

namespace events_admin.Entities;

public partial class TRANSACCIONES_PAGO
{
    public int id_transaccion { get; set; }

    public int id_venta { get; set; }

    public string? proveedor_pago { get; set; }

    public string? id_transaccion_ext { get; set; }

    public string? estado { get; set; }

    public string? metodo_pago { get; set; }

    public decimal monto { get; set; }

    public string? moneda { get; set; }

    public string? referencia { get; set; }

    public string? respuesta_json { get; set; }

    public DateTime? fecha_creacion { get; set; }

    public DateTime? fecha_actualizacion { get; set; }

    public virtual VENTA id_ventaNavigation { get; set; } = null!;
}
