using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Recebi.Api.Domain
{
    public class Usuario
    {
        public int IdUsuario { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Senha { get; set; } = string.Empty;
        public string? Telefone { get; set; }
        public string? Apartamento { get; set; }
        public string TipoUsuario { get; set; } = string.Empty;
        public string Status { get; set; } = "Ativo"; 

        [NotMapped]
        public bool EstaLogado { get; private set; } = false;

        public bool Login(string email, string senha)
        {
            if (Email.Equals(email, StringComparison.OrdinalIgnoreCase) && Senha == senha)
            {
                EstaLogado = true;
                return true;
            }
            return false;
        }

        public void Logout()
        {
            EstaLogado = false;
        }

        public string Resumo()
        {
            return $"{Nome} ({TipoUsuario}) - {Email} [{Status}]"; 
        }
    }
}