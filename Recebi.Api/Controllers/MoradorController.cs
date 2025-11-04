using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Recebi.Api.Domain;
using Recebi.Api.Infra;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using System.Text.Json; // Adicionado para RegistrarHistorico

namespace Recebi.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Morador")]
    public class MoradorController : ControllerBase
    {
        private readonly RecebiContext _context;

        public MoradorController(RecebiContext context)
        {
            _context = context;
        }

        // FIX CS0161, CS1998: Adicionado async Task<>, await, e return no catch
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
            // NOTE: Usar SaveChangesAsync aqui
            await _context.SaveChangesAsync();
        }

        // Endpoint antigo 'pendentes' - Marcar como obsoleto ou remover se 'encomendas' o substitui
        [HttpGet("pendentes")]
        [Obsolete("Use o endpoint /encomendas?status=Pendente")] // Marca como obsoleto
        public async Task<IActionResult> ConsultarPendentes()
        {
            // FIX CS1998, CS0161: Adicionado async/await e return no catch
            var idDoMoradorLogado = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(idDoMoradorLogado, out int moradorId))
            {
                return Unauthorized("Token com ID de usuário inválido.");
            }

            try
            {
                var moradorAtual = await _context.Usuarios.FindAsync(moradorId);
                if (moradorAtual == null || moradorAtual.Status != "Ativo")
                {
                    return Forbid("Usuário não encontrado ou inativo.");
                }

                var encomendas = await _context.Encomendas
                    .Where(e => e.IdUsuario == moradorId && e.Status == "Pendente")
                    .OrderByDescending(e => e.DataEntrada)
                    .ToListAsync(); // FIX CS1998: Usar ToListAsync com await

                if (!encomendas.Any())
                    return Ok(new { message = "Nenhuma encomenda pendente encontrada." });

                return Ok(encomendas);
            }
            catch (Exception ex)
            {
                // FIX CS0168: Usar ex.Message
                // FIX CS0161: Garantir retorno no catch
                Console.WriteLine($"Erro em ConsultarPendentes: {ex.Message}");
                return StatusCode(500, new { message = "Erro ao consultar encomendas pendentes.", error = ex.Message });
            }
        }

        [HttpPut("confirmar-recebimento/{idEncomenda}")]
        public async Task<IActionResult> ConfirmarRecebimento(int idEncomenda)
        {
            // FIX CS1998, CS0161: Adicionado async/await e return no catch
            var idDoMoradorLogado = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(idDoMoradorLogado, out int moradorId))
            {
                return Unauthorized("Token com ID de usuário inválido.");
            }

            try
            {
                var moradorAtual = await _context.Usuarios.FindAsync(moradorId);
                if (moradorAtual == null || moradorAtual.Status != "Ativo")
                {
                    return Forbid("Usuário não encontrado ou inativo.");
                }

                var encomenda = await _context.Encomendas.FindAsync(idEncomenda); // FIX CS1998: Usar FindAsync

                if (encomenda == null)
                    return NotFound(new { message = "Encomenda não encontrada." });

                if (encomenda.IdUsuario != moradorId)
                {
                    return Forbid("Você não tem permissão para confirmar esta encomenda.");
                }

                if (encomenda.Status == "Retirada")
                    return BadRequest(new { message = "Esta encomenda já foi retirada." });

                encomenda.Status = "Retirada";
                encomenda.DataRetirada = DateTime.Now;

                _context.Entry(encomenda).State = EntityState.Modified;
                await _context.SaveChangesAsync(); // FIX CS1998: Usar SaveChangesAsync

                // Gera detalhes para o histórico
                var detalhes = new { EncomendaId = encomenda.IdEncomenda, encomenda.Descricao, StatusAnterior = "Pendente" };

                await RegistrarHistoricoAsync(moradorId, $"Morador confirmou recebimento: {encomenda.Descricao}",
                                             "Confirmação de retirada", encomenda.IdEncomenda, detalhes);

                return Ok(new { message = "Encomenda confirmada como recebida.", encomenda });
            }
            catch (Exception ex)
            {
                // FIX CS0168, CS0161
                Console.WriteLine($"Erro em ConfirmarRecebimento: {ex.Message}");
                return StatusCode(500, new { message = "Erro ao confirmar recebimento.", error = ex.Message });
            }
        }

        [HttpGet("historico")]
        public async Task<IActionResult> ConsultarHistorico()
        {
            // FIX CS1998, CS0161: Adicionado async/await e return no catch
            var idDoMoradorLogado = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(idDoMoradorLogado, out int moradorId))
            {
                return Unauthorized("Token com ID de usuário inválido.");
            }

            try
            {
                var moradorAtual = await _context.Usuarios.FindAsync(moradorId);
                if (moradorAtual == null || moradorAtual.Status != "Ativo")
                {
                    return Forbid("Usuário não encontrado ou inativo.");
                }

                var historico = await _context.Historicos
                    .Include(h => h.Encomenda)
                    .Where(h => h.IdUsuario == moradorId)
                    .OrderByDescending(h => h.DataHora)
                    .Select(h => new
                    {
                        h.IdHistorico,
                        h.Acao,
                        h.Tipo,
                        h.DataHora,
                        Encomenda = h.Encomenda != null ? h.Encomenda.Descricao : "Sem referência",
                        h.IdEncomenda,
                        h.Detalhes
                    })
                    .ToListAsync(); 

                if (!historico.Any())
                    return Ok(new { message = "Nenhum histórico encontrado para este morador." });

                return Ok(historico);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro em ConsultarHistorico: {ex.Message}");
                return StatusCode(500, new { message = "Erro ao consultar histórico.", error = ex.Message });
            }
        }

        [HttpGet("encomendas")]
        public async Task<IActionResult> ConsultarMinhasEncomendas([FromQuery] string? status = null)
        {
            var idDoMoradorLogado = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(idDoMoradorLogado, out int moradorId))
            {
                return Unauthorized("Token com ID de usuário inválido.");
            }

            try
            {
                var moradorAtual = await _context.Usuarios.FindAsync(moradorId);
                if (moradorAtual == null || moradorAtual.Status != "Ativo")
                {
                    return Forbid("Usuário não encontrado ou inativo.");
                }

                var query = _context.Encomendas
                                    .Where(e => e.IdUsuario == moradorId);

                if (!string.IsNullOrEmpty(status) && status.ToLower() != "todas")
                {
                    query = query.Where(e => EF.Functions.Like(e.Status, status));
                }

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
                    Morador = item.Encomenda.Usuario?.Nome,
                    item.Encomenda.Descricao,
                    item.Encomenda.CodigoRastreio,
                    item.Encomenda.Status,
                    item.Encomenda.DataEntrada,
                    item.Encomenda.DataRetirada,
                    Porteiro = item.HistoricoRegistro?.Usuario?.Nome ?? "N/A"
                }).ToList();


                if (!resultadoFormatado.Any())
                    return Ok(new { message = "Nenhuma encomenda encontrada." });

                return Ok(resultadoFormatado);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro em ConsultarMinhasEncomendas: {ex.Message}");
                var innerExMessage = ex.InnerException?.Message ?? ex.Message;
                return StatusCode(500, new { message = "Erro ao buscar suas encomendas.", error = innerExMessage });
            }
        }

        [HttpGet("encomenda/{idEncomenda}")]
        public async Task<IActionResult> BuscarMinhaEncomendaPorId(int idEncomenda)
        {
            var idDoMoradorLogado = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(idDoMoradorLogado, out int moradorId))
            {
                return Unauthorized("Token com ID de usuário inválido.");
            }

            try
            {
                var moradorAtual = await _context.Usuarios.FindAsync(moradorId);
                if (moradorAtual == null || moradorAtual.Status != "Ativo")
                {
                    return Forbid("Usuário não encontrado ou inativo.");
                }

                var encomenda = await _context.Encomendas
                    .Include(e => e.Usuario)
                    .FirstOrDefaultAsync(e => e.IdEncomenda == idEncomenda); 

                if (encomenda == null)
                    return NotFound(new { message = "Encomenda não encontrada." });

                if (encomenda.IdUsuario != moradorId)
                {
                    return Forbid("Você não tem permissão para ver esta encomenda.");
                }

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
                Console.WriteLine($"Erro em BuscarMinhaEncomendaPorId: {ex.Message}");
                return StatusCode(500, new { message = "Erro ao buscar detalhes da encomenda.", error = ex.Message });
            }
        }
    }
}