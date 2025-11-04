using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Pomelo.EntityFrameworkCore.MySql.Scaffolding.Internal;

namespace Recebi.Api.Models;

public partial class RecebiDatabaseContext : DbContext
{
    public RecebiDatabaseContext()
    {
    }

    public RecebiDatabaseContext(DbContextOptions<RecebiDatabaseContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Encomenda> Encomendas { get; set; }

    public virtual DbSet<Historico> Historicos { get; set; }

    public virtual DbSet<Usuario> Usuarios { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        => optionsBuilder.UseMySql("server=localhost;database=recebi_database;user=root;password=123456", Microsoft.EntityFrameworkCore.ServerVersion.Parse("8.0.43-mysql"));

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder
            .UseCollation("utf8mb4_0900_ai_ci")
            .HasCharSet("utf8mb4");

        modelBuilder.Entity<Encomenda>(entity =>
        {
            entity.HasKey(e => e.IdEncomenda).HasName("PRIMARY");

            entity.ToTable("encomendas");

            entity.HasIndex(e => e.IdUsuario, "fk_encomendas_user");

            entity.Property(e => e.IdEncomenda).HasColumnName("id_encomenda");
            entity.Property(e => e.Apartamento)
                .HasMaxLength(10)
                .HasColumnName("apartamento");
            entity.Property(e => e.CodigoRastreio)
                .HasMaxLength(50)
                .HasColumnName("codigo_rastreio");
            entity.Property(e => e.DataEntrada)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime")
                .HasColumnName("data_entrada");
            entity.Property(e => e.DataRetirada)
                .HasColumnType("datetime")
                .HasColumnName("data_retirada");
            entity.Property(e => e.Descricao)
                .HasMaxLength(255)
                .HasColumnName("descricao");
            entity.Property(e => e.IdUsuario).HasColumnName("id_usuario");
            entity.Property(e => e.Status)
                .HasDefaultValueSql("'Pendente'")
                .HasColumnType("enum('Pendente','Retirada')")
                .HasColumnName("status");

            entity.HasOne(d => d.IdUsuarioNavigation).WithMany(p => p.Encomenda)
                .HasForeignKey(d => d.IdUsuario)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_encomendas_user");
        });

        modelBuilder.Entity<Historico>(entity =>
        {
            entity.HasKey(e => e.IdHistorico).HasName("PRIMARY");

            entity.ToTable("historico");

            entity.HasIndex(e => e.IdEncomenda, "fk_historio_encomenda");

            entity.HasIndex(e => e.Responsavel, "historico_user");

            entity.Property(e => e.IdHistorico).HasColumnName("id_historico");
            entity.Property(e => e.Acao)
                .HasMaxLength(50)
                .HasColumnName("acao");
            entity.Property(e => e.DataAcao)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime")
                .HasColumnName("data_acao");
            entity.Property(e => e.IdEncomenda).HasColumnName("id_encomenda");
            entity.Property(e => e.Responsavel).HasColumnName("responsavel");

            entity.HasOne(d => d.IdEncomendaNavigation).WithMany(p => p.Historicos)
                .HasForeignKey(d => d.IdEncomenda)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_historio_encomenda");

            entity.HasOne(d => d.ResponsavelNavigation).WithMany(p => p.Historicos)
                .HasForeignKey(d => d.Responsavel)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("historico_user");
        });

        modelBuilder.Entity<Usuario>(entity =>
        {
            entity.HasKey(e => e.IdUsuario).HasName("PRIMARY");

            entity.ToTable("usuarios");

            entity.HasIndex(e => e.Email, "email").IsUnique();

            entity.Property(e => e.IdUsuario).HasColumnName("id_usuario");
            entity.Property(e => e.Apart)
                .HasMaxLength(10)
                .HasColumnName("apart");
            entity.Property(e => e.Email)
                .HasMaxLength(100)
                .HasColumnName("email");
            entity.Property(e => e.Nome)
                .HasMaxLength(100)
                .HasColumnName("nome");
            entity.Property(e => e.Senha)
                .HasMaxLength(255)
                .HasColumnName("senha");
            entity.Property(e => e.Telefone)
                .HasMaxLength(15)
                .HasColumnName("telefone");
            entity.Property(e => e.TipoUsuario)
                .HasColumnType("enum('Morador','Porteiro','Sindico')")
                .HasColumnName("tipo_usuario");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
