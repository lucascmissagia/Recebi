using System;
using System.Collections.Generic;

namespace Recebi.Api.Models;

public partial class Encomenda
{
    public int IdEncomenda { get; set; }

    public string Apartamento { get; set; } = null!;

    public int IdUsuario { get; set; }

    public string Descricao { get; set; } = null!;

    public string? CodigoRastreio { get; set; }

    public string? Status { get; set; }

    public DateTime DataEntrada { get; set; }

    public DateTime? DataRetirada { get; set; }

    public virtual ICollection<Historico> Historicos { get; set; } = new List<Historico>();

    public virtual Usuario IdUsuarioNavigation { get; set; } = null!;
}
