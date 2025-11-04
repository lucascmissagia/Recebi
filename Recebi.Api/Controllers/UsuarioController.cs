using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Recebi.Api.Domain;
using Recebi.Api.Infra;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Recebi.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsuarioController : ControllerBase
    {
        private readonly RecebiContext _context;
        private readonly IConfiguration _config;

        public UsuarioController(RecebiContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] Usuario login)
        {
            if (login == null)
                return BadRequest(new { message = "Dados de login inválidos." });

            var user = await _context.Usuarios
                .FirstOrDefaultAsync(u => u.Email == login.Email && u.Senha == login.Senha);

            if (user == null)
                return Unauthorized(new { message = "E-mail ou senha incorretos." });

            if (user.Status != "Ativo")
            {
                return Unauthorized(new { message = "Este usuário está inativo." });
            }
            
            var token = GerarTokenJwt(user);

            return Ok(new
            {
                message = "Login realizado com sucesso.",
                token = token,
                usuario = new
                {
                    user.IdUsuario,
                    user.Nome,
                    user.Email,
                    user.TipoUsuario,
                    user.Telefone,
                    user.Apartamento,
                    user.Status 
                }
            });
        }

        [HttpPost("logout/{id}")]
        public async Task<IActionResult> Logout(int id)
        {
            var user = await _context.Usuarios.FindAsync(id);
            if (user == null)
                return NotFound(new { message = "Usuário não encontrado." });

            return Ok(new { message = $"{user.Nome} saiu do sistema com sucesso." });
        }


        private string GerarTokenJwt(Usuario usuario)
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, usuario.IdUsuario.ToString()),
                new Claim(ClaimTypes.Name, usuario.Nome),
                new Claim(ClaimTypes.Email, usuario.Email),
                new Claim(ClaimTypes.Role, usuario.TipoUsuario),
                new Claim("Status", usuario.Status) 
            };

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.Now.AddHours(8),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}