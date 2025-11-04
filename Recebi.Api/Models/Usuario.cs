using System;
using System.Collections.Generic;

namespace Recebi.Api.Models;

public partial class Usuario
{
    public int IdUsuario { get; set; }

    public string Nome { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string Senha { get; set; } = null!;

    public string? Telefone { get; set; }

    public string? Apart { get; set; }

    public string TipoUsuario { get; set; } = null!;

    public virtual ICollection<Encomenda> Encomenda { get; set; } = new List<Encomenda>();

    public virtual ICollection<Historico> Historicos { get; set; } = new List<Historico>();
}
