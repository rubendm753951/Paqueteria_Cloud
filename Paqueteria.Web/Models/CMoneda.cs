using System;
using System.Collections.Generic;

namespace Paqueteria.Web.Models;

public partial class CMoneda
{
    public int IdMoneda { get; set; }

    public string? Nombre { get; set; }

    public string Abreviatura { get; set; } = null!;

    public virtual ICollection<CAgencia> CAgencia { get; set; } = new List<CAgencia>();

    public virtual ICollection<CCorporativo> CCorporativos { get; set; } = new List<CCorporativo>();

    public virtual ICollection<CPaise> CPaises { get; set; } = new List<CPaise>();
}
