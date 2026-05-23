using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using events_admin.Entities;

namespace events_admin.Data;

public partial class QuasarDbContext : DbContext
{
    public QuasarDbContext(DbContextOptions<QuasarDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<ASIENTO> ASIENTOs { get; set; }

    public virtual DbSet<ESTADO_TICKET> ESTADO_TICKETs { get; set; }

    public virtual DbSet<EVENTO> EVENTOs { get; set; }

    public virtual DbSet<EVENTO_ASIENTO> EVENTO_ASIENTOs { get; set; }

    public virtual DbSet<FAVORITO> FAVORITOs { get; set; }

    public virtual DbSet<NOTIFICACIONE> NOTIFICACIONEs { get; set; }

    public virtual DbSet<PQR> PQRs { get; set; }

    public virtual DbSet<ROL_STAFF> ROL_STAFFs { get; set; }

    public virtual DbSet<SCAN> SCANs { get; set; }

    public virtual DbSet<STAFF> STAFF { get; set; }

    public virtual DbSet<TICKET> TICKETs { get; set; }

    public virtual DbSet<TIPO_EVENTO> TIPO_EVENTOs { get; set; }

    public virtual DbSet<TRANSACCIONES_PAGO> TRANSACCIONES_PAGOs { get; set; }

    public virtual DbSet<USUARIO> USUARIOs { get; set; }

    public virtual DbSet<VENTA> VENTAs { get; set; }

    public virtual DbSet<WEBHOOK_LOG> WEBHOOK_LOGs { get; set; }

    public virtual DbSet<ZONA> ZONAs { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder
            .UseCollation("utf8mb4_0900_ai_ci")
            .HasCharSet("utf8mb4");

        modelBuilder.Entity<ASIENTO>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("ASIENTOS");

            entity.HasIndex(e => e.SeatCode, "codigo_asiento").IsUnique();

            entity.HasIndex(e => e.ZoneId, "id_zona");

            entity.Property(e => e.IsActive).HasDefaultValueSql("'1'");
            entity.Property(e => e.SeatCode).HasMaxLength(20);
            entity.Property(e => e.Row).HasMaxLength(10);

            entity.HasOne(d => d.id_zonaNavigation).WithMany(p => p.ASIENTOs)
                .HasForeignKey(d => d.ZoneId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("ASIENTOS_ibfk_1");
        });

        modelBuilder.Entity<ESTADO_TICKET>(entity =>
        {
            entity.HasKey(e => e.id_estado_ticket).HasName("PRIMARY");

            entity.ToTable("ESTADO_TICKET");

            entity.HasIndex(e => e.nombre_estado, "nombre_estado").IsUnique();

            entity.Property(e => e.nombre_estado).HasMaxLength(50);
        });

        modelBuilder.Entity<EVENTO>(entity =>
        {
            entity.HasKey(e => e.id_evento).HasName("PRIMARY");

            entity.ToTable("EVENTOS");

            entity.HasIndex(e => e.creado_por_staff, "creado_por_staff");

            entity.HasIndex(e => e.id_tipo_evento, "id_tipo_evento");

            entity.HasIndex(e => e.fecha_evento, "idx_eventos_fecha");

            entity.Property(e => e.activo).HasDefaultValueSql("'1'");
            entity.Property(e => e.descripcion).HasColumnType("text");
            entity.Property(e => e.fecha_cancelacion).HasColumnType("datetime");
            entity.Property(e => e.fecha_creacion)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime");
            entity.Property(e => e.fecha_evento).HasColumnType("datetime");
            entity.Property(e => e.fecha_fin_ventas).HasColumnType("datetime");
            entity.Property(e => e.fecha_inicio_ventas).HasColumnType("datetime");
            entity.Property(e => e.motivo_cancelacion).HasColumnType("text");
            entity.Property(e => e.nombre_evento).HasMaxLength(150);
            entity.Property(e => e.publicado).HasDefaultValueSql("'1'");

            entity.HasOne(d => d.creado_por_staffNavigation).WithMany(p => p.EVENTOs)
                .HasForeignKey(d => d.creado_por_staff)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("EVENTOS_ibfk_2");

            entity.HasOne(d => d.id_tipo_eventoNavigation).WithMany(p => p.EVENTOs)
                .HasForeignKey(d => d.id_tipo_evento)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("EVENTOS_ibfk_1");
        });

        modelBuilder.Entity<EVENTO_ASIENTO>(entity =>
        {
            entity.HasKey(e => e.id_evento_asiento).HasName("PRIMARY");

            entity.ToTable("EVENTO_ASIENTO");

            entity.HasIndex(e => e.id_asiento, "id_asiento");

            entity.HasIndex(e => new { e.id_evento, e.id_asiento }, "id_evento").IsUnique();

            entity.Property(e => e.estado)
                .HasDefaultValueSql("'DISPONIBLE'")
                .HasColumnType("enum('DISPONIBLE','RESERVADO','VENDIDO','BLOQUEADO')");
            entity.Property(e => e.fecha_reserva).HasColumnType("datetime");
            entity.Property(e => e.precio).HasPrecision(10, 2);
            entity.Property(e => e.reserva_expira).HasColumnType("datetime");

            entity.HasOne(d => d.id_asientoNavigation).WithMany(p => p.EVENTO_ASIENTOs)
                .HasForeignKey(d => d.id_asiento)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("EVENTO_ASIENTO_ibfk_2");

            entity.HasOne(d => d.id_eventoNavigation).WithMany(p => p.EVENTO_ASIENTOs)
                .HasForeignKey(d => d.id_evento)
                .HasConstraintName("EVENTO_ASIENTO_ibfk_1");
        });

        modelBuilder.Entity<FAVORITO>(entity =>
        {
            entity.HasKey(e => e.id_favorito).HasName("PRIMARY");

            entity.ToTable("FAVORITOS");

            entity.HasIndex(e => e.id_evento, "id_evento");

            entity.HasIndex(e => new { e.id_usuario, e.id_evento }, "id_usuario").IsUnique();

            entity.Property(e => e.fecha_agregado)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime");

            entity.HasOne(d => d.id_eventoNavigation).WithMany(p => p.FAVORITOs)
                .HasForeignKey(d => d.id_evento)
                .HasConstraintName("FAVORITOS_ibfk_2");

            entity.HasOne(d => d.id_usuarioNavigation).WithMany(p => p.FAVORITOs)
                .HasForeignKey(d => d.id_usuario)
                .HasConstraintName("FAVORITOS_ibfk_1");
        });

        modelBuilder.Entity<NOTIFICACIONE>(entity =>
        {
            entity.HasKey(e => e.id_notificacion).HasName("PRIMARY");

            entity.ToTable("NOTIFICACIONES");

            entity.HasIndex(e => e.id_usuario, "id_usuario");

            entity.Property(e => e.fecha_envio)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime");
            entity.Property(e => e.leido).HasDefaultValueSql("'0'");
            entity.Property(e => e.mensaje).HasColumnType("text");
            entity.Property(e => e.titulo).HasMaxLength(255);

            entity.HasOne(d => d.id_usuarioNavigation).WithMany(p => p.NOTIFICACIONEs)
                .HasForeignKey(d => d.id_usuario)
                .HasConstraintName("NOTIFICACIONES_ibfk_1");
        });

        modelBuilder.Entity<PQR>(entity =>
        {
            entity.HasKey(e => e.id_pqrs).HasName("PRIMARY");

            entity.ToTable("PQRS");

            entity.HasIndex(e => e.asignado_staff, "asignado_staff");

            entity.HasIndex(e => e.id_usuario, "id_usuario");

            entity.HasIndex(e => e.estado, "idx_pqrs_estado");

            entity.Property(e => e.asunto).HasMaxLength(255);
            entity.Property(e => e.estado)
                .HasDefaultValueSql("'ABIERTO'")
                .HasColumnType("enum('ABIERTO','EN_PROCESO','RESPONDIDO','CERRADO')");
            entity.Property(e => e.fecha_creacion)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime");
            entity.Property(e => e.fecha_respuesta).HasColumnType("datetime");
            entity.Property(e => e.mensaje).HasColumnType("text");
            entity.Property(e => e.respuesta).HasColumnType("text");
            entity.Property(e => e.tipo).HasColumnType("enum('PREGUNTA','QUEJA','RECLAMO','SUGERENCIA')");

            entity.HasOne(d => d.asignado_staffNavigation).WithMany(p => p.PQRs)
                .HasForeignKey(d => d.asignado_staff)
                .HasConstraintName("PQRS_ibfk_2");

            entity.HasOne(d => d.id_usuarioNavigation).WithMany(p => p.PQRs)
                .HasForeignKey(d => d.id_usuario)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("PQRS_ibfk_1");
        });

        modelBuilder.Entity<ROL_STAFF>(entity =>
        {
            entity.HasKey(e => e.id_rol_staff).HasName("PRIMARY");

            entity.ToTable("ROL_STAFF");

            entity.HasIndex(e => e.nombre_rol, "nombre_rol").IsUnique();

            entity.Property(e => e.activo).HasDefaultValueSql("'1'");
            entity.Property(e => e.nombre_rol).HasMaxLength(50);
        });

        modelBuilder.Entity<SCAN>(entity =>
        {
            entity.HasKey(e => e.id_scan).HasName("PRIMARY");

            entity.ToTable("SCAN");

            entity.HasIndex(e => e.id_staff, "id_staff");

            entity.HasIndex(e => e.id_ticket, "id_ticket");

            entity.HasIndex(e => e.fecha_scan, "idx_scan_fecha");

            entity.Property(e => e.fecha_scan)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime");
            entity.Property(e => e.observacion).HasColumnType("text");
            entity.Property(e => e.resultado).HasColumnType("enum('VALIDO','DUPLICADO','INVALIDO','FALSO')");

            entity.HasOne(d => d.id_staffNavigation).WithMany(p => p.SCANs)
                .HasForeignKey(d => d.id_staff)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("SCAN_ibfk_2");

            entity.HasOne(d => d.id_ticketNavigation).WithMany(p => p.SCANs)
                .HasForeignKey(d => d.id_ticket)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("SCAN_ibfk_1");
        });

        modelBuilder.Entity<STAFF>(entity =>
        {
            entity.HasKey(e => e.id_staff).HasName("PRIMARY");

            entity.HasIndex(e => e.email, "email").IsUnique();

            entity.HasIndex(e => e.id_rol_staff, "id_rol_staff");

            entity.Property(e => e.activo).HasDefaultValueSql("'1'");
            entity.Property(e => e.email).HasMaxLength(100);
            entity.Property(e => e.fecha_registro)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime");
            entity.Property(e => e.nombre).HasMaxLength(100);
            entity.Property(e => e.password_hash).HasMaxLength(255);

            entity.HasOne(d => d.id_rol_staffNavigation).WithMany(p => p.STAFF)
                .HasForeignKey(d => d.id_rol_staff)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("STAFF_ibfk_1");
        });

        modelBuilder.Entity<TICKET>(entity =>
        {
            entity.HasKey(e => e.id_ticket).HasName("PRIMARY");

            entity.ToTable("TICKETS");

            entity.HasIndex(e => e.codigo_unico, "codigo_unico").IsUnique();

            entity.HasIndex(e => e.id_estado_ticket, "id_estado_ticket");

            entity.HasIndex(e => e.id_evento_asiento, "id_evento_asiento").IsUnique();

            entity.HasIndex(e => e.id_venta, "id_venta");

            entity.HasIndex(e => e.qr_token, "qr_token").IsUnique();

            entity.Property(e => e.fecha_generacion)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime");
            entity.Property(e => e.qr_token).HasMaxLength(500);

            entity.HasOne(d => d.id_estado_ticketNavigation).WithMany(p => p.TICKETs)
                .HasForeignKey(d => d.id_estado_ticket)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("TICKETS_ibfk_2");

            entity.HasOne(d => d.id_evento_asientoNavigation).WithOne(p => p.TICKET)
                .HasForeignKey<TICKET>(d => d.id_evento_asiento)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("TICKETS_ibfk_3");

            entity.HasOne(d => d.id_ventaNavigation).WithMany(p => p.TICKETs)
                .HasForeignKey(d => d.id_venta)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("TICKETS_ibfk_1");
        });

        modelBuilder.Entity<TIPO_EVENTO>(entity =>
        {
            entity.HasKey(e => e.id_tipo_evento).HasName("PRIMARY");

            entity.ToTable("TIPO_EVENTO");

            entity.HasIndex(e => e.nombre_tipo, "nombre_tipo").IsUnique();

            entity.Property(e => e.activo).HasDefaultValueSql("'1'");
            entity.Property(e => e.nombre_tipo).HasMaxLength(100);
        });

        modelBuilder.Entity<TRANSACCIONES_PAGO>(entity =>
        {
            entity.HasKey(e => e.id_transaccion).HasName("PRIMARY");

            entity.ToTable("TRANSACCIONES_PAGO");

            entity.HasIndex(e => e.id_venta, "id_venta");

            entity.HasIndex(e => e.estado, "idx_transacciones_estado");

            entity.Property(e => e.estado)
                .HasDefaultValueSql("'PENDING'")
                .HasColumnType("enum('PENDING','APPROVED','DECLINED','VOIDED','ERROR','REFUNDED')");
            entity.Property(e => e.fecha_actualizacion)
                .ValueGeneratedOnAddOrUpdate()
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime");
            entity.Property(e => e.fecha_creacion)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime");
            entity.Property(e => e.id_transaccion_ext).HasMaxLength(255);
            entity.Property(e => e.metodo_pago).HasMaxLength(50);
            entity.Property(e => e.moneda)
                .HasMaxLength(10)
                .HasDefaultValueSql("'COP'");
            entity.Property(e => e.monto).HasPrecision(10, 2);
            entity.Property(e => e.proveedor_pago)
                .HasDefaultValueSql("'WOMPI'")
                .HasColumnType("enum('WOMPI')");
            entity.Property(e => e.referencia).HasMaxLength(255);
            entity.Property(e => e.respuesta_json).HasColumnType("json");

            entity.HasOne(d => d.id_ventaNavigation).WithMany(p => p.TRANSACCIONES_PAGOs)
                .HasForeignKey(d => d.id_venta)
                .HasConstraintName("TRANSACCIONES_PAGO_ibfk_1");
        });

        modelBuilder.Entity<USUARIO>(entity =>
        {
            entity.HasKey(e => e.id_usuario).HasName("PRIMARY");

            entity.ToTable("USUARIO");

            entity.HasIndex(e => e.email, "email").IsUnique();

            entity.HasIndex(e => e.google_id, "google_id").IsUnique();

            entity.Property(e => e.activo).HasDefaultValueSql("'1'");
            entity.Property(e => e.email).HasMaxLength(100);
            entity.Property(e => e.fecha_registro)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime");
            entity.Property(e => e.foto_perfil).HasMaxLength(255);
            entity.Property(e => e.nombre).HasMaxLength(100);
            entity.Property(e => e.password_hash).HasMaxLength(255);
            entity.Property(e => e.telefono).HasMaxLength(20);
        });

        modelBuilder.Entity<VENTA>(entity =>
        {
            entity.HasKey(e => e.id_venta).HasName("PRIMARY");

            entity.ToTable("VENTAS");

            entity.HasIndex(e => e.id_staff, "id_staff");

            entity.HasIndex(e => e.id_usuario, "id_usuario");

            entity.HasIndex(e => e.fecha_venta, "idx_ventas_fecha");

            entity.HasIndex(e => e.referencia_interna, "referencia_interna").IsUnique();

            entity.Property(e => e.estado_pago)
                .HasDefaultValueSql("'PENDING'")
                .HasColumnType("enum('PENDING','APPROVED','DECLINED','VOIDED','ERROR','REFUNDED')");
            entity.Property(e => e.fecha_pago).HasColumnType("datetime");
            entity.Property(e => e.fecha_venta)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime");
            entity.Property(e => e.id_transaccion_wompi).HasMaxLength(255);
            entity.Property(e => e.json_respuesta).HasColumnType("json");
            entity.Property(e => e.metodo_pago).HasMaxLength(50);
            entity.Property(e => e.moneda)
                .HasMaxLength(10)
                .HasDefaultValueSql("'COP'");
            entity.Property(e => e.referencia_wompi).HasMaxLength(255);
            entity.Property(e => e.tipo_venta).HasColumnType("enum('ONLINE','TAQUILLA')");
            entity.Property(e => e.total).HasPrecision(10, 2);

            entity.HasOne(d => d.id_staffNavigation).WithMany(p => p.VENTAs)
                .HasForeignKey(d => d.id_staff)
                .HasConstraintName("VENTAS_ibfk_2");

            entity.HasOne(d => d.id_usuarioNavigation).WithMany(p => p.VENTAs)
                .HasForeignKey(d => d.id_usuario)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("VENTAS_ibfk_1");
        });

        modelBuilder.Entity<WEBHOOK_LOG>(entity =>
        {
            entity.HasKey(e => e.id_log).HasName("PRIMARY");

            entity.ToTable("WEBHOOK_LOGS");

            entity.Property(e => e.error).HasColumnType("text");
            entity.Property(e => e.evento).HasMaxLength(100);
            entity.Property(e => e.fecha)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime");
            entity.Property(e => e.payload_json).HasColumnType("json");
            entity.Property(e => e.procesado).HasDefaultValueSql("'0'");
            entity.Property(e => e.proveedor).HasMaxLength(50);
        });

        modelBuilder.Entity<ZONA>(entity =>
        {
            entity.HasKey(e => e.id_zona).HasName("PRIMARY");

            entity.ToTable("ZONAS");

            entity.Property(e => e.activo).HasDefaultValueSql("'1'");
            entity.Property(e => e.color_hex).HasMaxLength(10);
            entity.Property(e => e.nombre_zona).HasMaxLength(100);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
