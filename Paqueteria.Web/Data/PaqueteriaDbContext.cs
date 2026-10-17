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

    public virtual DbSet<UserPagePermission> UserPagePermissions { get; set; }

    public virtual DbSet<UserAgencyPermission> UserAgencyPermissions { get; set; }

    public virtual DbSet<ProductCatalogItem> Products { get; set; }

    public virtual DbSet<AgencyProduct> AgencyProducts { get; set; }

    public virtual DbSet<AgencyProductRateRange> AgencyProductRateRanges { get; set; }

    public virtual DbSet<ShipmentOrder> ShipmentOrders { get; set; }

    public virtual DbSet<ShipmentAddress> ShipmentAddresses { get; set; }

    public virtual DbSet<ShipmentPackage> ShipmentPackages { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ProductCatalogItem>(entity =>
        {
            entity.ToTable("products");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.SkuCodigo).IsUnique();
            entity.Property(e => e.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.SkuCodigo).HasColumnName("sku_codigo").HasMaxLength(50).IsRequired();
            entity.Property(e => e.Nombre).HasColumnName("nombre").HasMaxLength(160).IsRequired();
            entity.Property(e => e.Descripcion).HasColumnName("descripcion");
            entity.Property(e => e.FechaCreacion).HasColumnName("fecha_creacion").HasDefaultValueSql("now()");
            entity.Property(e => e.FechaBaja).HasColumnName("fecha_baja");
            entity.Property(e => e.Estado).HasColumnName("estado").HasMaxLength(12).HasDefaultValue("activo");
        });

        modelBuilder.Entity<AgencyProduct>(entity =>
        {
            entity.ToTable("agency_products");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.IdAgencia, e.IdProducto }).IsUnique();
            entity.Property(e => e.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.IdAgencia).HasColumnName("id_agencia");
            entity.Property(e => e.IdProducto).HasColumnName("id_producto");
            entity.Property(e => e.Moneda).HasColumnName("moneda").HasColumnType("character(3)");
            entity.Property(e => e.IncrementoPesoKg).HasColumnName("incremento_peso_kg").HasPrecision(9, 3).HasDefaultValue(1m);
            entity.Property(e => e.CoberturaExtendidaPrecio).HasColumnName("cobertura_extendida_precio").HasPrecision(12, 2);
            entity.Property(e => e.CargoDimensionesExtrasPrecio).HasColumnName("cargo_dimensiones_extras_precio").HasPrecision(12, 2);
            entity.Property(e => e.LadoMaximoCm).HasColumnName("lado_maximo_cm").HasPrecision(8, 2).HasDefaultValue(120m);
            entity.Property(e => e.SumaLadosMaximaCm).HasColumnName("suma_lados_maxima_cm").HasPrecision(8, 2);
            entity.Property(e => e.Activo).HasColumnName("activo").HasDefaultValue(true);
            entity.Property(e => e.FechaCreacion).HasColumnName("fecha_creacion").HasDefaultValueSql("now()");
        });

        modelBuilder.Entity<AgencyProductRateRange>(entity =>
        {
            entity.ToTable("agency_product_rate_ranges");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.IdAgenciaProducto, e.PesoInicialKg });
            entity.Property(e => e.Id).HasColumnName("id").UseIdentityAlwaysColumn();
            entity.Property(e => e.IdAgenciaProducto).HasColumnName("id_agencia_producto");
            entity.Property(e => e.PesoInicialKg).HasColumnName("peso_inicial_kg").HasPrecision(9, 3);
            entity.Property(e => e.PesoFinalKg).HasColumnName("peso_final_kg").HasPrecision(9, 3);
            entity.Property(e => e.PrecioBaseRango).HasColumnName("precio_base_rango").HasPrecision(12, 2);
            entity.Property(e => e.PrecioKgAdicional).HasColumnName("precio_kg_adicional").HasPrecision(12, 2);
        });

        modelBuilder.Entity<ShipmentOrder>(entity =>
        {
            entity.ToTable("shipment_orders");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.TrackingNumber).IsUnique();
            entity.HasIndex(e => new { e.IdAgencia, e.FechaCreacion });
            entity.Property(e => e.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.TrackingNumber).HasColumnName("tracking_number").HasMaxLength(40).IsRequired();
            entity.Property(e => e.IdAgencia).HasColumnName("id_agencia");
            entity.Property(e => e.IdProducto).HasColumnName("id_producto");
            entity.Property(e => e.IdAgenciaProducto).HasColumnName("id_agencia_producto");
            entity.Property(e => e.FechaCreacion).HasColumnName("fecha_creacion").HasDefaultValueSql("now()");
            entity.Property(e => e.EstadoOrden).HasColumnName("estado_orden").HasMaxLength(16).HasDefaultValue("cotizada");
            entity.Property(e => e.CoberturaExtendida).HasColumnName("cobertura_extendida");
            entity.Property(e => e.CoberturaExtendidaPrecioSnapshot).HasColumnName("cobertura_extendida_precio_snapshot").HasPrecision(12, 2);
            entity.Property(e => e.TotalEnvio).HasColumnName("total_envio").HasPrecision(12, 2);
            entity.Property(e => e.Moneda).HasColumnName("moneda").HasColumnType("character(3)");
            entity.Property(e => e.CreadaPorUserId).HasColumnName("creada_por_user_id");
        });

        modelBuilder.Entity<ShipmentAddress>(entity =>
        {
            entity.ToTable("shipment_addresses");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.IdOrden, e.TipoContacto }).IsUnique();
            entity.Property(e => e.Id).HasColumnName("id").UseIdentityAlwaysColumn();
            entity.Property(e => e.IdOrden).HasColumnName("id_orden");
            entity.Property(e => e.TipoContacto).HasColumnName("tipo_contacto").HasMaxLength(12);
            entity.Property(e => e.Pais).HasColumnName("pais").HasColumnType("character(2)");
            entity.Property(e => e.NombreContacto).HasColumnName("nombre_contacto").HasMaxLength(160);
            entity.Property(e => e.Empresa).HasColumnName("empresa").HasMaxLength(160);
            entity.Property(e => e.DireccionLinea1).HasColumnName("direccion_linea1").HasMaxLength(200);
            entity.Property(e => e.DireccionLinea2).HasColumnName("direccion_linea2").HasMaxLength(200);
            entity.Property(e => e.CodigoPostal).HasColumnName("codigo_postal").HasMaxLength(12);
            entity.Property(e => e.Colonia).HasColumnName("colonia").HasMaxLength(120);
            entity.Property(e => e.Municipio).HasColumnName("municipio").HasMaxLength(120);
            entity.Property(e => e.Estado).HasColumnName("estado").HasMaxLength(120);
            entity.Property(e => e.Ciudad).HasColumnName("ciudad").HasMaxLength(120);
            entity.Property(e => e.Telefono).HasColumnName("telefono").HasMaxLength(30);
            entity.Property(e => e.Email).HasColumnName("email").HasMaxLength(254);
            entity.Property(e => e.TaxIdRfc).HasColumnName("tax_id_rfc").HasMaxLength(20);
        });

        modelBuilder.Entity<ShipmentAddress>()
            .HasOne<ShipmentOrder>()
            .WithMany()
            .HasForeignKey(address => address.IdOrden)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_shipment_addresses_order");

        modelBuilder.Entity<ShipmentPackage>(entity =>
        {
            entity.ToTable("shipment_packages");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.IdOrden, e.SecuenciaPaquete }).IsUnique();
            entity.Property(e => e.Id).HasColumnName("id").UseIdentityAlwaysColumn();
            entity.Property(e => e.IdOrden).HasColumnName("id_orden");
            entity.Property(e => e.SecuenciaPaquete).HasColumnName("secuencia_paquete");
            entity.Property(e => e.PesoBasculaKg).HasColumnName("peso_bascula_kg").HasPrecision(9, 3);
            entity.Property(e => e.PesoVolumetricoKg).HasColumnName("peso_volumetrico_kg").HasPrecision(9, 3);
            entity.Property(e => e.PesoCobrableKg).HasColumnName("peso_cobrable_kg").HasPrecision(9, 3);
            entity.Property(e => e.LargoCm).HasColumnName("largo_cm").HasPrecision(8, 2);
            entity.Property(e => e.AnchoCm).HasColumnName("ancho_cm").HasPrecision(8, 2);
            entity.Property(e => e.AltoCm).HasColumnName("alto_cm").HasPrecision(8, 2);
            entity.Property(e => e.DescripcionContenido).HasColumnName("descripcion_contenido").HasMaxLength(240);
            entity.Property(e => e.EsDimensionExtra).HasColumnName("es_dimension_extra");
            entity.Property(e => e.TarifaBaseSnapshot).HasColumnName("tarifa_base_snapshot").HasPrecision(12, 2);
            entity.Property(e => e.CargoKgAdicionalSnapshot).HasColumnName("cargo_kg_adicional_snapshot").HasPrecision(12, 2);
            entity.Property(e => e.CargoDimensionExtraSnapshot).HasColumnName("cargo_dimension_extra_snapshot").HasPrecision(12, 2);
            entity.Property(e => e.TotalPaqueteSnapshot).HasColumnName("total_paquete_snapshot").HasPrecision(12, 2);
        });

        modelBuilder.Entity<ShipmentPackage>()
            .HasOne<ShipmentOrder>()
            .WithMany()
            .HasForeignKey(package => package.IdOrden)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_shipment_packages_order");

        modelBuilder.Entity<UserPagePermission>(entity =>
        {
            entity.HasKey(e => new { e.UserId, e.PageKey })
                .HasName("pk_user_page_permissions");
            entity.ToTable("user_page_permissions");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.PageKey).HasColumnName("page_key");
            entity.Property(e => e.CanView).HasColumnName("can_view");
            entity.Property(e => e.CanCreate).HasColumnName("can_create");
            entity.Property(e => e.CanEdit).HasColumnName("can_edit");
            entity.Property(e => e.CanDelete).HasColumnName("can_delete");
        });

        modelBuilder.Entity<UserAgencyPermission>(entity =>
        {
            entity.HasKey(e => new { e.UserId, e.AgencyId })
                .HasName("pk_user_agency_permissions");
            entity.ToTable("user_agency_permissions");
            entity.HasIndex(e => e.AgencyId, "ix_user_agency_permissions_agency_id");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.AgencyId).HasColumnName("agency_id");
            entity.Property(e => e.CanView).HasColumnName("can_view");
            entity.Property(e => e.CanEdit).HasColumnName("can_edit");
            entity.Property(e => e.CanDelete).HasColumnName("can_delete");
        });

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
