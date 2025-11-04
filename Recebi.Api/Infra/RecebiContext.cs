using Microsoft.EntityFrameworkCore;
using Recebi.Api.Domain;

namespace Recebi.Api.Infra
{
    public class RecebiContext : DbContext
    {
        public RecebiContext(DbContextOptions<RecebiContext> options) : base(options) { }

        // ===============================
        // TABELAS DO BANCO DE DADOS
        // ===============================
        public DbSet<Usuario> Usuarios { get; set; }
        public DbSet<Encomenda> Encomendas { get; set; }
        public DbSet<Historico> Historicos { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // =========================================
            // MAPEAMENTO: USUARIO
            // =========================================
            modelBuilder.Entity<Usuario>(entity =>
            {
                entity.ToTable("usuarios");

                entity.HasKey(u => u.IdUsuario);

                entity.Property(u => u.IdUsuario)
                      .HasColumnName("id_usuario");

                entity.Property(u => u.Nome)
                      .HasColumnName("nome")
                      .HasMaxLength(100)
                      .IsRequired();

                entity.Property(u => u.Email)
                      .HasColumnName("email")
                      .HasMaxLength(100)
                      .IsRequired();

                entity.Property(u => u.Senha)
                      .HasColumnName("senha")
                      .HasMaxLength(255)
                      .IsRequired();

                entity.Property(u => u.Telefone)
                      .HasColumnName("telefone")
                      .HasMaxLength(15);

                entity.Property(u => u.Apartamento)
                      .HasColumnName("apart")
                      .HasMaxLength(10);

                entity.Property(u => u.TipoUsuario)
                      .HasColumnName("tipo_usuario")
                      .HasMaxLength(20)
                      .IsRequired();
                
                entity.Property(u => u.Status)
                      .HasColumnName("status") 
                      .HasMaxLength(10)      
                      .IsRequired();          
            });

            // =========================================
            // CONFIGURAÇÃO DE HERANÇA (TPH usando tipo_usuario)
            // =========================================
            modelBuilder.Entity<Usuario>()
                .ToTable("usuarios")
                .HasDiscriminator(u => u.TipoUsuario)
                .HasValue<Sindico>("Sindico")
                .HasValue<Porteiro>("Porteiro")
                .HasValue<Morador>("Morador");

            // =========================================
            // MAPEAMENTO: ENCOMENDA
            // =========================================
            modelBuilder.Entity<Encomenda>(entity =>
            {
                entity.ToTable("encomendas");

                entity.HasKey(e => e.IdEncomenda);

                entity.Property(e => e.IdEncomenda)
                      .HasColumnName("id_encomenda");

                entity.Property(e => e.Apartamento)
                      .HasColumnName("apartamento")
                      .HasMaxLength(10)
                      .IsRequired();

                entity.Property(e => e.IdUsuario)
                      .HasColumnName("id_usuario")
                      .IsRequired();

                entity.Property(e => e.Descricao)
                      .HasColumnName("descricao")
                      .HasMaxLength(255)
                      .IsRequired();

                entity.Property(e => e.CodigoRastreio)
                      .HasColumnName("codigo_rastreio")
                      .HasMaxLength(50);

                entity.Property(e => e.Status)
                      .HasColumnName("status")
                      .HasMaxLength(20)
                      .HasDefaultValue("Pendente");

                entity.Property(e => e.DataEntrada)
                      .HasColumnName("data_entrada")
                      .HasColumnType("datetime");

                entity.Property(e => e.DataRetirada)
                      .HasColumnName("data_retirada")
                      .HasColumnType("datetime");

                entity.HasOne(e => e.Usuario)
                       .WithMany()
                      .HasForeignKey(e => e.IdUsuario)
                      .HasConstraintName("fk_encomenda_usuario");
            });

            // =========================================
            // MAPEAMENTO: HISTORICO
            // =========================================
            modelBuilder.Entity<Historico>(entity =>
            {
                entity.ToTable("historico");

                entity.HasKey(h => h.IdHistorico);

                entity.Property(h => h.IdHistorico)
                      .HasColumnName("id_historico");

                entity.Property(h => h.IdUsuario)
                      .HasColumnName("id_usuario")
                      .IsRequired(false);

                entity.Property(h => h.IdEncomenda)
                      .HasColumnName("id_encomenda")
                      .IsRequired(false);

                entity.Property(h => h.Acao)
                      .HasColumnName("acao")
                      .HasMaxLength(255)
                      .IsRequired();

                entity.Property(h => h.Tipo)
                      .HasColumnName("tipo")
                      .HasMaxLength(50)
                      .IsRequired();

                entity.Property(h => h.DataHora)
                      .HasColumnName("data_hora")
                      .HasColumnType("datetime")
                      .IsRequired();



                entity.HasOne(h => h.Usuario)
                      .WithMany()
                      .HasForeignKey(h => h.IdUsuario)
                      .OnDelete(DeleteBehavior.SetNull)
                      .HasConstraintName("fk_historico_usuario");

                entity.HasOne(h => h.Encomenda)
                      .WithMany()
                      .HasForeignKey(h => h.IdEncomenda)
                      .OnDelete(DeleteBehavior.Restrict)
                      .HasConstraintName("fk_historico_encomenda");
            });
        }
    }
}
