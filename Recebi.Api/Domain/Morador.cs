using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace Recebi.Api.Domain
{
    [NotMapped]
    public class Morador : Usuario
    {
        [NotMapped] public List<Encomenda> Encomendas { get; set; } = new();
        [NotMapped] public List<Historico> HistoricoPessoal { get; set; } = new();

        public List<Historico> VerHistorico()
        {
            return HistoricoPessoal.OrderByDescending(h => h.DataHora).ToList();
        }

        public bool ConfirmarRecebimento(int idEncomenda)
        {
            var encomenda = Encomendas.FirstOrDefault(e => e.IdEncomenda == idEncomenda && e.Status == "Pendente");
            if (encomenda != null)
            {
                encomenda.Status = "Retirada";
                encomenda.DataRetirada = DateTime.Now;

                HistoricoPessoal.Add(new Historico
                {
                    IdUsuario = this.IdUsuario,
                    IdEncomenda = encomenda.IdEncomenda,
                    Acao = $"Morador confirmou o recebimento da encomenda '{encomenda.Descricao}'.",
                    Tipo = "Confirmação de Entrega",
                    DataHora = DateTime.Now
                });

                return true;
            }

            return false;
        }
    }
}
