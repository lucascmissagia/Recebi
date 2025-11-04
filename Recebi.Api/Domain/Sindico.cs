using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace Recebi.Api.Domain
{
    [NotMapped]
    public class Sindico : Usuario
    {
        [NotMapped] public List<Usuario> UsuariosGerenciados { get; set; } = new();
        [NotMapped] public List<Historico> HistoricoConsultado { get; set; } = new();

        public Usuario CriarUsuario(string nome, string email, string senha, string telefone, string tipoUsuario, string apartamento = "")
        {
            var novoUsuario = new Usuario
            {
                Nome = nome,
                Email = email,
                Senha = senha,
                Telefone = telefone,
                TipoUsuario = tipoUsuario,
                Apartamento = apartamento
            };

            UsuariosGerenciados.Add(novoUsuario);

            HistoricoConsultado.Add(new Historico
            {
                IdUsuario = this.IdUsuario,
                Acao = $"Síndico criou o usuário '{nome}' ({tipoUsuario}).",
                Tipo = "Criação de Usuário",
                DataHora = DateTime.Now
            });

            return novoUsuario;
        }

        public bool DeletarUsuario(int idUsuario)
        {
            var usuario = UsuariosGerenciados.FirstOrDefault(u => u.IdUsuario == idUsuario);
            if (usuario != null)
            {
                UsuariosGerenciados.Remove(usuario);

                HistoricoConsultado.Add(new Historico
                {
                    IdUsuario = this.IdUsuario,
                    Acao = $"Síndico removeu o usuário '{usuario.Nome}'.",
                    Tipo = "Remoção de Usuário",
                    DataHora = DateTime.Now
                });

                return true;
            }
            return false;
        }

        public bool AtualizarUsuario(int idUsuario, string? nome = null, string? telefone = null, string? senha = null)
        {
            var usuario = UsuariosGerenciados.FirstOrDefault(u => u.IdUsuario == idUsuario);
            if (usuario == null) return false;

            if (!string.IsNullOrEmpty(nome)) usuario.Nome = nome;
            if (!string.IsNullOrEmpty(telefone)) usuario.Telefone = telefone;
            if (!string.IsNullOrEmpty(senha)) usuario.Senha = senha;

            HistoricoConsultado.Add(new Historico
            {
                IdUsuario = this.IdUsuario,
                Acao = $"Síndico atualizou informações do usuário '{usuario.Nome}'.",
                Tipo = "Atualização de Usuário",
                DataHora = DateTime.Now
            });

            return true;
        }

        public List<Usuario> BuscarUsuariosPorTipo(string tipo)
        {
            return UsuariosGerenciados
                .Where(u => u.TipoUsuario.Equals(tipo, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        public List<Historico> BuscarLogsAntigos(List<Historico> historicos, int diasAtras = 30)
        {
            var limite = DateTime.Now.AddDays(-diasAtras);
            HistoricoConsultado = historicos
                .Where(h => h.DataHora <= limite)
                .OrderByDescending(h => h.DataHora)
                .ToList();

            return HistoricoConsultado;
        }
    }
}
