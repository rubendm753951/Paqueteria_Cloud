using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Paqueteria.Web.Models;

namespace Paqueteria.Web.Data;

public partial class PaqueteriaDbContext : DbContext
{
    public PaqueteriaDbContext(DbContextOptions<PaqueteriaDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<CAgencia> CAgencias { get; set; }

    public virtual DbSet<CCorporativo> CCorporativos { get; set; }

    public virtual DbSet<CEmpresa> CEmpresas { get; set; }

    public virtual DbSet<CMoneda> CMonedas { get; set; }

    public virtual DbSet<CPaise> CPaises { get; set; }

    public virtual DbSet<CRecibo> CRecibos { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CAgencia>(entity =>
        {
            entity.HasKey(e => e.IdAgencia).HasName("pk_c_agencias");

            entity.ToTable("c_agencias");

            entity.HasIndex(e => e.IdCorporativo, "ix_c_agencias_id_corporativo");

            entity.HasIndex(e => e.IdMoneda, "ix_c_agencias_id_moneda");

            entity.HasIndex(e => e.IdPais, "ix_c_agencias_id_pais");

            entity.HasIndex(e => e.IdRecibo, "ix_c_agencias_id_recibo");

            entity.Property(e => e.IdAgencia).HasColumnName("id_agencia");
            entity.Property(e => e.Ciudad)
                .HasMaxLength(100)
                .HasColumnName("ciudad");
            entity.Property(e => e.CodigoPostal)
                .HasMaxLength(50)
                .HasColumnName("codigo_postal");
            entity.Property(e => e.Colonia)
                .HasMaxLength(100)
                .HasColumnName("colonia");
            entity.Property(e => e.CostoAdicional)
                .HasPrecision(18, 4)
                .HasColumnName("costo_adicional");
            entity.Property(e => e.Direccion)
                .HasMaxLength(200)
                .HasColumnName("direccion");
            entity.Property(e => e.Email)
                .HasMaxLength(100)
                .HasColumnName("email");
            entity.Property(e => e.EsquemaPorFactor).HasColumnName("esquema_por_factor");
            entity.Property(e => e.EstadoProvincia)
                .HasMaxLength(100)
                .HasColumnName("estado_provincia");
            entity.Property(e => e.Factor)
                .HasPrecision(18, 4)
                .HasColumnName("factor");
            entity.Property(e => e.FechaAlta)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("fecha_alta");
            entity.Property(e => e.FechaTermino)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("fecha_termino");
            entity.Property(e => e.FedexRemitente).HasColumnName("fedex_remitente");
            entity.Property(e => e.GuiaEstafeta).HasColumnName("guia_estafeta");
            entity.Property(e => e.IdCorporativo).HasColumnName("id_corporativo");
            entity.Property(e => e.IdMoneda).HasColumnName("id_moneda");
            entity.Property(e => e.IdPais).HasColumnName("id_pais");
            entity.Property(e => e.IdRecibo).HasColumnName("id_recibo");
            entity.Property(e => e.LimiteCredito)
                .HasPrecision(10)
                .HasColumnName("limite_credito");
            entity.Property(e => e.Municipio)
                .HasMaxLength(100)
                .HasColumnName("municipio");
            entity.Property(e => e.Nit)
                .HasMaxLength(100)
                .HasColumnName("nit");
            entity.Property(e => e.Noexterior)
                .HasMaxLength(50)
                .HasColumnName("noexterior");
            entity.Property(e => e.Nombre)
                .HasMaxLength(200)
                .HasColumnName("nombre");
            entity.Property(e => e.PeRemitente).HasColumnName("pe_remitente");
            entity.Property(e => e.RequiereAsignacion).HasColumnName("requiere_asignacion");
            entity.Property(e => e.Rfc)
                .HasMaxLength(50)
                .HasColumnName("rfc");
            entity.Property(e => e.Telefono)
                .HasMaxLength(50)
                .HasColumnName("telefono");
            entity.Property(e => e.TipoDePago)
                .HasMaxLength(20)
                .HasColumnName("tipo_de_pago");

            entity.HasOne(d => d.IdCorporativoNavigation).WithMany(p => p.CAgencia)
                .HasForeignKey(d => d.IdCorporativo)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_c_agencias_c_corporativos");

            entity.HasOne(d => d.IdMonedaNavigation).WithMany(p => p.CAgencia)
                .HasForeignKey(d => d.IdMoneda)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_c_agencias_c_monedas");

            entity.HasOne(d => d.IdPaisNavigation).WithMany(p => p.CAgencia)
                .HasForeignKey(d => d.IdPais)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_c_agencias_c_paises");

            entity.HasOne(d => d.IdReciboNavigation).WithMany(p => p.CAgencia)
                .HasForeignKey(d => d.IdRecibo)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_c_agencias_c_recibos");
        });

        modelBuilder.Entity<CCorporativo>(entity =>
        {
            entity.HasKey(e => e.IdCorporativo).HasName("pk_c_corporativos");

            entity.ToTable("c_corporativos");

            entity.HasIndex(e => e.IdEmpresa, "ix_c_corporativos_id_empresa");

            entity.HasIndex(e => e.IdMoneda, "ix_c_corporativos_id_moneda");

            entity.HasIndex(e => e.IdPais, "ix_c_corporativos_id_pais");

            entity.Property(e => e.IdCorporativo).HasColumnName("id_corporativo");
            entity.Property(e => e.Ciudad)
                .HasMaxLength(100)
                .HasColumnName("ciudad");
            entity.Property(e => e.Direccion)
                .HasMaxLength(200)
                .HasColumnName("direccion");
            entity.Property(e => e.EstadoProvincia)
                .HasMaxLength(100)
                .HasColumnName("estado_provincia");
            entity.Property(e => e.IdEmpresa).HasColumnName("id_empresa");
            entity.Property(e => e.IdMoneda).HasColumnName("id_moneda");
            entity.Property(e => e.IdPais).HasColumnName("id_pais");
            entity.Property(e => e.LimiteCredito)
                .HasPrecision(19, 4)
                .HasColumnName("limite_credito");
            entity.Property(e => e.Nit)
                .HasMaxLength(100)
                .HasColumnName("nit");
            entity.Property(e => e.Nombre)
                .HasMaxLength(200)
                .HasColumnName("nombre");
            entity.Property(e => e.Telefono)
                .HasMaxLength(50)
                .HasColumnName("telefono");

            entity.HasOne(d => d.IdEmpresaNavigation).WithMany(p => p.CCorporativos)
                .HasForeignKey(d => d.IdEmpresa)
                .HasConstraintName("fk_c_corporativos_c_empresas");

            entity.HasOne(d => d.IdMonedaNavigation).WithMany(p => p.CCorporativos)
                .HasForeignKey(d => d.IdMoneda)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_c_corporativos_c_monedas");

            entity.HasOne(d => d.IdPaisNavigation).WithMany(p => p.CCorporativos)
                .HasForeignKey(d => d.IdPais)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_c_corporativos_c_paises");
        });

        modelBuilder.Entity<CEmpresa>(entity =>
        {
            entity.HasKey(e => e.IdEmpresa).HasName("pk_c_empresas");

            entity.ToTable("c_empresas");

            entity.HasIndex(e => e.IdPais, "ix_c_empresas_id_pais");

            entity.Property(e => e.IdEmpresa).HasColumnName("id_empresa");
            entity.Property(e => e.Alto).HasColumnName("alto");
            entity.Property(e => e.Ancho).HasColumnName("ancho");
            entity.Property(e => e.Ciudad)
                .HasMaxLength(50)
                .HasColumnName("ciudad");
            entity.Property(e => e.Direccion)
                .HasMaxLength(500)
                .HasColumnName("direccion");
            entity.Property(e => e.Estado)
                .HasMaxLength(50)
                .HasColumnName("estado");
            entity.Property(e => e.FechaAlta)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("fecha_alta");
            entity.Property(e => e.IdPais).HasColumnName("id_pais");
            entity.Property(e => e.Logo).HasColumnName("logo");
            entity.Property(e => e.Nombre)
                .HasMaxLength(300)
                .HasColumnName("nombre");
            entity.Property(e => e.Telefono)
                .HasMaxLength(25)
                .HasColumnName("telefono");
        });

        modelBuilder.Entity<CMoneda>(entity =>
        {
            entity.HasKey(e => e.IdMoneda).HasName("pk_c_monedas");

            entity.ToTable("c_monedas");

            entity.Property(e => e.IdMoneda)
                .ValueGeneratedNever()
                .HasColumnName("id_moneda");
            entity.Property(e => e.Abreviatura)
                .HasMaxLength(20)
                .HasColumnName("abreviatura");
            entity.Property(e => e.Nombre)
                .HasMaxLength(100)
                .HasColumnName("nombre");
        });

        modelBuilder.Entity<CPaise>(entity =>
        {
            entity.HasKey(e => e.IdPais).HasName("pk_c_paises");

            entity.ToTable("c_paises");

            entity.HasIndex(e => e.IdMoneda, "ix_c_paises_id_moneda");

            entity.Property(e => e.IdPais)
                .ValueGeneratedNever()
                .HasColumnName("id_pais");
            entity.Property(e => e.CountryCode)
                .HasMaxLength(3)
                .HasColumnName("country_code");
            entity.Property(e => e.IdMoneda).HasColumnName("id_moneda");
            entity.Property(e => e.Nombre)
                .HasMaxLength(200)
                .HasColumnName("nombre");

            entity.HasOne(d => d.IdMonedaNavigation).WithMany(p => p.CPaises)
                .HasForeignKey(d => d.IdMoneda)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_c_paises_c_monedas");
        });

        modelBuilder.Entity<CRecibo>(entity =>
        {
            entity.HasKey(e => e.IdRecibo).HasName("pk_c_recibos");

            entity.ToTable("c_recibos");

            entity.Property(e => e.IdRecibo)
                .ValueGeneratedNever()
                .HasColumnName("id_recibo");
            entity.Property(e => e.Archivo)
                .HasMaxLength(50)
                .HasColumnName("archivo");
            entity.Property(e => e.Descripcion)
                .HasMaxLength(50)
                .HasColumnName("descripcion");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
