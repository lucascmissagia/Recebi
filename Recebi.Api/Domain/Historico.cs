using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Recebi.Api.Domain
{
    [Table("historico")]
    public class Historico
    {
        [Key]
        [Column("id_historico")]
        public int IdHistorico { get; set; }

        [Column("id_usuario")]
        public int? IdUsuario { get; set; } 

        [Column("id_encomenda")]
        public int? IdEncomenda { get; set; } 

        [Column("acao")]
        [MaxLength(255)]
        public string Acao { get; set; } = string.Empty; 

        [Column("tipo")]
        [MaxLength(50)]
        public string Tipo { get; set; } = string.Empty; 

        [Column("data_hora")]
        public DateTime DataHora { get; set; } = DateTime.Now;

        [Column("detalhes")]
        public string? Detalhes { get; set; } 

        [ForeignKey("IdUsuario")]
        public Usuario Usuario { get; set; } = null!; 

        [ForeignKey("IdEncomenda")]
        public Encomenda? Encomenda { get; set; } 
    }
}