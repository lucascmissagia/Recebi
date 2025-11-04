using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Recebi.Api.Domain;
using Recebi.Api.Infra;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using System.Text.Json; // Adicionado

namespace Recebi.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Porteiro")]
    public class PorteiroController : ControllerBase
    {
        private readonly RecebiContext _context;

        public PorteiroController(RecebiContext context) 
        {
            _context = context; 
        }

        private async Task RegistrarHistoricoAsync(int idUsuario, string acao, string tipo, int? idEncomenda = null, object? detalhes = null)
        {
            var historico = new Historico
            {
                IdUsuario = idUsuario,
                Acao = acao,
                Tipo = tipo,
                IdEncomenda = idEncomenda,
                DataHora = DateTime.Now,
                Detalhes = detalhes != null ? JsonSerializer.Serialize(detalhes) : null
            };
            _context.Historicos.Add(historico);
            await _context.SaveChangesAsync(); 
        }


        [HttpPost("RegistrarEncomenda")]
        public async Task<IActionResult> RegistrarEncomenda([FromBody] Encomenda novaEncomenda)
        {
            if (novaEncomenda == null)
                return BadRequest(new { message = "Os dados da encomenda são inválidos." });

            var idDoPorteiroLogado = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(idDoPorteiroLogado, out int porteiroId))
                return Unauthorized("Token com ID de usuário inválido.");

            var morador = await _context.Usuarios.FirstOrDefaultAsync(u => u.IdUsuario == novaEncomenda.IdUsuario && u.TipoUsuario == "Morador");
            if (morador == null) return NotFound(new { message = "Morador não encontrado." });
            if (morador.Status != "Ativo") return BadRequest(new { message = $"O morador {morador.Nome} está inativo." });

            try
            {
                novaEncomenda.Status = "Pendente";
                novaEncomenda.DataEntrada = DateTime.Now;

                _context.Encomendas.Add(novaEncomenda);
                await _context.SaveChangesAsync(); 

                var detalhes = new { EncomendaId = novaEncomenda.IdEncomenda, MoradorId = novaEncomenda.IdUsuario, MoradorNome = morador.Nome, Apartamento = novaEncomenda.Apartamento, Descricao = novaEncomenda.Descricao, CodigoRastreio = novaEncomenda.CodigoRastreio };

                await RegistrarHistoricoAsync(
                    porteiroId,
                    $"Porteiro registrou: {novaEncomenda.Descricao} p/ Apt {novaEncomenda.Apartamento}",
                    "Registro de Encomenda",
                    novaEncomenda.IdEncomenda,
                    detalhes
                );

                return Ok(new { message = "Encomenda registrada com sucesso!", encomenda = novaEncomenda });
            }
            catch (Exception ex)
            {
                var innerEx = ex.InnerException?.Message ?? ex.Message;
                Console.WriteLine($"Erro em RegistrarEncomenda: {innerEx}"); 
                return StatusCode(500, new { message = "Erro ao registrar encomenda.", error = innerEx });
            }
        }

        [HttpGet("Encomendas")]
        public async Task<IActionResult> VerificarEncomendas([FromQuery] string? status = null)
        {
            try
            {
                var query = _context.Encomendas.AsQueryable();

                if (!string.IsNullOrEmpty(status))
                    query = query.Where(e => e.Status == status);

                var encomendasComPorteiro = await query
                    .Include(e => e.Usuario) 
                    .OrderByDescending(e => e.DataEntrada)
                    .Select(e => new {
                        Encomenda = e,
                        HistoricoRegistro = _context.Historicos
                                                .Include(h => h.Usuario)
                                                .FirstOrDefault(h => h.IdEncomenda == e.IdEncomenda && h.Tipo == "Registro de Encomenda")
                    })
                    .ToListAsync(); 

                var resultadoFormatado = encomendasComPorteiro.Select(item => new
                {
                    item.Encomenda.IdEncomenda,
                    item.Encomenda.Apartamento,
                    Morador = item.Encomenda.Usuario?.Nome ?? "N/A", 
                    item.Encomenda.Descricao,
                    item.Encomenda.CodigoRastreio,
                    item.Encomenda.Status,
                    item.Encomenda.DataEntrada,
                    item.Encomenda.DataRetirada,
                    Porteiro = item.HistoricoRegistro?.Usuario?.Nome ?? "N/A"
                }).ToList();


                return resultadoFormatado.Any()
                    ? Ok(resultadoFormatado)
                    : Ok(new { message = "Nenhuma encomenda encontrada." });
            }
            catch (Exception ex)
            {
                var innerExMessage = ex.InnerException?.Message ?? ex.Message;
                Console.WriteLine($"Erro em VerificarEncomendas: {innerExMessage}");
                return StatusCode(500, new { message = "Erro ao buscar encomendas.", error = innerExMessage });
            }
        }

        [HttpGet("BuscarMoradores")]
        public async Task<IActionResult> BuscarMoradores([FromQuery] string query)
        {
            if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
            {
                return Ok(new List<object>());
            }

            try
            {
                var moradores = await _context.Usuarios
                    .Where(u => u.TipoUsuario == "Morador" &&
                                u.Status == "Ativo" &&
                                EF.Functions.Like(u.Nome, $"%{query}%"))
                    .OrderBy(u => u.Nome)
                    .Take(10)
                    .Select(u => new {
                        u.IdUsuario,
                        u.Nome,
                        u.Apartamento
                    })
                    .ToListAsync(); 

                return Ok(moradores);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro em BuscarMoradores: {ex.Message}");
                return StatusCode(500, new { message = "Erro ao buscar moradores.", error = ex.Message });
            }
        }
    }
}