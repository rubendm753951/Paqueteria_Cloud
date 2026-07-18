using System;
using System.Collections.Generic;

namespace Paqueteria.Web.Models;

public partial class CCorporativo
{
    public int IdCorporativo { get; set; }

    public string? Nombre { get; set; }

    public string? Direccion { get; set; }

    public string? Ciudad { get; set; }

    public string? EstadoProvincia { get; set; }

    public string? Telefono { get; set; }

    public string? Nit { get; set; }

    public int IdPais { get; set; }

    public int IdMoneda { get; set; }

    public decimal? LimiteCredito { get; set; }

    public int? IdEmpresa { get; set; }

    public virtual ICollection<CAgencia> CAgencia { get; set; } = new List<CAgencia>();

    public virtual CEmpresa? IdEmpresaNavigation { get; set; }

    public virtual CMoneda IdMonedaNavigation { get; set; } = null!;

    public virtual CPaise IdPaisNavigation { get; set; } = null!;
}
