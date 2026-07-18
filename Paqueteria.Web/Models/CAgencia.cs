using System;
using System.Collections.Generic;

namespace Paqueteria.Web.Models;

public partial class CAgencia
{
    public int IdAgencia { get; set; }

    public int IdCorporativo { get; set; }

    public int IdPais { get; set; }

    public string? Nombre { get; set; }

    public string? Direccion { get; set; }

    public string? EstadoProvincia { get; set; }

    public string? Ciudad { get; set; }

    public string? Telefono { get; set; }

    public DateTime? FechaAlta { get; set; }

    public DateTime? FechaTermino { get; set; }

    public int IdMoneda { get; set; }

    public decimal? LimiteCredito { get; set; }

    public string? Nit { get; set; }

    public bool RequiereAsignacion { get; set; }

    public int IdRecibo { get; set; }

    public decimal Factor { get; set; }

    public bool EsquemaPorFactor { get; set; }

    public decimal CostoAdicional { get; set; }

    public bool GuiaEstafeta { get; set; }

    public string? Noexterior { get; set; }

    public string? Colonia { get; set; }

    public string? Municipio { get; set; }

    public string? CodigoPostal { get; set; }

    public string? Email { get; set; }

    public string? Rfc { get; set; }

    public string? TipoDePago { get; set; }

    public bool PeRemitente { get; set; }

    public bool FedexRemitente { get; set; }

    public virtual CCorporativo IdCorporativoNavigation { get; set; } = null!;

    public virtual CMoneda IdMonedaNavigation { get; set; } = null!;

    public virtual CPaise IdPaisNavigation { get; set; } = null!;

    public virtual CRecibo IdReciboNavigation { get; set; } = null!;
}
