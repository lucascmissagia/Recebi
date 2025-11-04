using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Recebi.Api.Domain
{
    [Table("encomendas")]
    public class Encomenda
    {
        [Key]
        [Column("id_encomenda")]
        public int IdEncomenda { get; set; }

        [Required]
        [MaxLength(10)]
        [Column("apartamento")]
        public string Apartamento { get; set; } = string.Empty;

        [Required]
        [Column("id_usuario")]
        public int IdUsuario { get; set; }

        [Required]
        [MaxLength(255)]
        [Column("descricao")]
        public string Descricao { get; set; } = string.Empty;

        [MaxLength(50)]
        [Column("codigo_rastreio")]
        public string? CodigoRastreio { get; set; }

        [Required]
        [MaxLength(20)]
        [Column("status")]
        public string Status { get; set; } = "Pendente";

        [Column("data_entrada")]
        public DateTime? DataEntrada { get; set; }

        [Column("data_retirada")]
        public DateTime? DataRetirada { get; set; }

        [ForeignKey("IdUsuario")]
        public Usuario? Usuario { get; set; }

        public void RegistrarEntrada()
        {
            DataEntrada = DateTime.Now;
            Status = "Pendente";
        }

        public void RegistrarRetirada()
        {
            DataRetirada = DateTime.Now;
            Status = "Retirada";
        }
    }
}
