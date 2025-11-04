using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;


namespace Recebi.Api.Domain
{
    [NotMapped]
    public class Porteiro : Usuario
    {
        [NotMapped] public List<Encomenda> EncomendasRegistradas { get; set; } = new();

        public Encomenda RegistrarEncomenda(string descricao, string apartamento, int idUsuario, string codigoRastreio)
        {
            var encomenda = new Encomenda
            {
                Descricao = descricao,
                Apartamento = apartamento,
                IdUsuario = idUsuario,
                CodigoRastreio = codigoRastreio
            };

            encomenda.RegistrarEntrada();
            EncomendasRegistradas.Add(encomenda);
            return encomenda;
        }

        public List<Encomenda> VerificarEncomendas(string? status = null)
        {
            return string.IsNullOrEmpty(status)
                ? EncomendasRegistradas
                : EncomendasRegistradas.Where(e => e.Status.Equals(status, System.StringComparison.OrdinalIgnoreCase)).ToList();
        }
    }
}
