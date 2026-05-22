using System;
using System.Collections.Generic;

namespace events_admin.Entities;

public partial class WEBHOOK_LOG
{
    public int id_log { get; set; }

    public string proveedor { get; set; } = null!;

    public string? evento { get; set; }

    public string payload_json { get; set; } = null!;

    public bool? procesado { get; set; }

    public string? error { get; set; }

    public DateTime? fecha { get; set; }
}
