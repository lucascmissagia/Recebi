using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Recebi.Api.Domain;
using Recebi.Api.Infra;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using System.Text.Json; 

namespace Recebi.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Porteiro,Sindico")] 
    public class EncomendaController : ControllerBase
    {
        private readonly RecebiContext _context;

        public EncomendaController(RecebiContext context)
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

        [HttpGet("todas")]
        public async Task<IActionResult> ListarTodas()
        {
            try
            {
                var encomendasComDados = await _context.Encomendas
                    .Include(e => e.Usuario) 
                    .OrderByDescending(e => e.DataEntrada)
                    .Select(e => new { 
                        Encomenda = e,
                        HistoricoRegistro = _context.Historicos
                                                .Include(h => h.Usuario) 
                                                .FirstOrDefault(h => h.IdEncomenda == e.IdEncomenda && h.Tipo == "Registro de Encomenda")
                    })
                    .ToListAsync();

                var resultadoFormatado = encomendasComDados.Select(item => new
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

                if (!resultadoFormatado.Any())
                    return Ok(new { message = "Nenhuma encomenda registrada." });

                return Ok(resultadoFormatado);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro em ListarTodas Encomendas: {ex.InnerException?.Message ?? ex.Message}");
                return StatusCode(500, new { message = "Erro ao listar encomendas.", error = ex.InnerException?.Message ?? ex.Message });
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> BuscarPorId(int id)
        {
            try
            {
                var encomenda = await _context.Encomendas
                    .Include(e => e.Usuario) 
                    .FirstOrDefaultAsync(e => e.IdEncomenda == id);

                if (encomenda == null)
                    return NotFound(new { message = "Encomenda não encontrada." });

                var historicoRegistro = await _context.Historicos
                                                .Include(h => h.Usuario) 
                                                .FirstOrDefaultAsync(h => h.IdEncomenda == encomenda.IdEncomenda && h.Tipo == "Registro de Encomenda");

                return Ok(new
                {
                    encomenda.IdEncomenda,
                    encomenda.Descricao,
                    encomenda.CodigoRastreio,
                    encomenda.Status,
                    encomenda.DataEntrada,
                    encomenda.DataRetirada,
                    Morador = encomenda.Usuario?.Nome ?? "N/A",
                    encomenda.Apartamento,
                    Porteiro = historicoRegistro?.Usuario?.Nome ?? "N/A" 
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro em BuscarPorId Encomenda: {ex.InnerException?.Message ?? ex.Message}");
                return StatusCode(500, new { message = "Erro ao buscar encomenda.", error = ex.InnerException?.Message ?? ex.Message });
            }
        }

        [HttpDelete("deletar/{id}")]
        [Authorize(Roles = "Sindico")]
        public async Task<IActionResult> DeletarEncomenda(int id)
        {
            try
            {
                var encomenda = await _context.Encomendas.FindAsync(id);
                if (encomenda == null)
                    return NotFound(new { message = "Encomenda não encontrada." });

                var sindicoIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!int.TryParse(sindicoIdStr, out int idDoUsuarioLogado))
                    return Unauthorized("Token inválido.");

                var detalhes = new { EncomendaRemovidaId = encomenda.IdEncomenda, encomenda.Descricao, encomenda.Apartamento, MoradorId = encomenda.IdUsuario };

                _context.Encomendas.Remove(encomenda);
                await _context.SaveChangesAsync();

                await RegistrarHistoricoAsync(idDoUsuarioLogado, $"Encomenda '{encomenda.Descricao}' (ID: {id}) foi removida.",
                                              "Remoção de encomenda", id, detalhes);

                return Ok(new { message = "Encomenda deletada com sucesso." });
            }
            catch (DbUpdateException ex) 
            {
                var innerExMessage = ex.InnerException?.Message ?? ex.Message;
                Console.WriteLine($"Erro DbUpdateException em DeletarEncomenda: {innerExMessage}");
                return StatusCode(400, new { message = "Erro ao deletar encomenda (verifique dependências no histórico).", error = innerExMessage });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro genérico em DeletarEncomenda: {ex.Message}");
                return StatusCode(500, new { message = "Erro ao deletar encomenda.", error = ex.Message });
            }
        }
    }
}