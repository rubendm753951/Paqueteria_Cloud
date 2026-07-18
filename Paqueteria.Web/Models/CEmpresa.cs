using System;
using System.Collections.Generic;

namespace Paqueteria.Web.Models;

public partial class CEmpresa
{
    public int IdEmpresa { get; set; }

    public string? Nombre { get; set; }

    public string? Direccion { get; set; }

    public string? Telefono { get; set; }

    public string? Ciudad { get; set; }

    public string? Estado { get; set; }

    public int? IdPais { get; set; }

    public DateTime? FechaAlta { get; set; }

    public byte[]? Logo { get; set; }

    public int? Alto { get; set; }

    public int? Ancho { get; set; }

    public virtual ICollection<CCorporativo> CCorporativos { get; set; } = new List<CCorporativo>();
}
