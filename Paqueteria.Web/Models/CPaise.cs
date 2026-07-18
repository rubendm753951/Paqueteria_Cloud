using System;
using System.Collections.Generic;

namespace Paqueteria.Web.Models;

public partial class CPaise
{
    public int IdPais { get; set; }

    public string Nombre { get; set; } = null!;

    public int IdMoneda { get; set; }

    public string? CountryCode { get; set; }

    public virtual ICollection<CAgencia> CAgencia { get; set; } = new List<CAgencia>();

    public virtual ICollection<CCorporativo> CCorporativos { get; set; } = new List<CCorporativo>();

    public virtual CMoneda IdMonedaNavigation { get; set; } = null!;
}
