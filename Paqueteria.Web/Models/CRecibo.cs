using System;
using System.Collections.Generic;

namespace Paqueteria.Web.Models;

public partial class CRecibo
{
    public int IdRecibo { get; set; }

    public string? Archivo { get; set; }

    public string? Descripcion { get; set; }

    public virtual ICollection<CAgencia> CAgencia { get; set; } = new List<CAgencia>();
}
