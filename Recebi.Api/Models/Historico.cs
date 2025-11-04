using System;
using System.Collections.Generic;

namespace Recebi.Api.Models;

public partial class Historico
{
    public int IdHistorico { get; set; }

    public int IdEncomenda { get; set; }

    public string Acao { get; set; } = null!;

    public DateTime DataAcao { get; set; }

    public int Responsavel { get; set; }

    public virtual Encomenda IdEncomendaNavigation { get; set; } = null!;

    public virtual Usuario ResponsavelNavigation { get; set; } = null!;
}
