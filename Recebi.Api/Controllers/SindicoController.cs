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
    [Authorize(Roles = "Sindico")]
    public class SindicoController : ControllerBase
    {
        private readonly RecebiContext _context;

        public SindicoController(RecebiContext context)
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
                Detalhes = detalhes != null ? JsonSerializer.Serialize(detalhes, new JsonSerializerOptions { WriteIndented = false }) : null 
            };
            _context.Historicos.Add(historico);
            await _context.SaveChangesAsync();
        }

        // --- CRUD DE USUÁRIOS ---

        [HttpPost("criar")]
        public async Task<IActionResult> CriarUsuario([FromBody] Usuario novoUsuario)
        {
            if (novoUsuario == null)
                return BadRequest(new { message = "Os dados do usuário são obrigatórios." });

            var idDoUsuarioLogado = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(idDoUsuarioLogado, out int sindicoId))
                return Unauthorized("Token inválido.");

            try
            {
                novoUsuario.Status = "Ativo"; 
                _context.Usuarios.Add(novoUsuario);
                await _context.SaveChangesAsync();

                var detalhes = new { UsuarioCriadoId = novoUsuario.IdUsuario, novoUsuario.Nome, novoUsuario.Email, novoUsuario.TipoUsuario, novoUsuario.Apartamento, novoUsuario.Telefone };
                await RegistrarHistoricoAsync(sindicoId, $"Síndico criou: {novoUsuario.Nome} ({novoUsuario.TipoUsuario})",
                                              "Criação de usuário", detalhes: detalhes);

                return Ok(new { message = "Usuário criado com sucesso!", usuario = new { novoUsuario.IdUsuario, novoUsuario.Nome, novoUsuario.Email, novoUsuario.TipoUsuario, novoUsuario.Apartamento, novoUsuario.Status } });
            }
            catch (DbUpdateException ex)
            {
                var innerExMessage = ex.InnerException?.Message ?? ex.Message;
                Console.WriteLine($"Erro DbUpdateException em CriarUsuario: {innerExMessage}");
                return StatusCode(400, new { message = "Erro ao salvar no banco de dados (verifique se o email já existe).", error = innerExMessage });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro Genérico em CriarUsuario: {ex.Message}");
                return StatusCode(500, new { message = "Erro ao criar usuário.", error = ex.Message });
            }
        }

        [HttpPut("atualizar/{id}")]
        public async Task<IActionResult> AtualizarUsuario(int id, [FromBody] Usuario usuarioAtualizado)
        {
            if (usuarioAtualizado == null || id <= 0)
                return BadRequest(new { message = "Dados inválidos." });

            var usuarioOriginal = await _context.Usuarios.AsNoTracking().FirstOrDefaultAsync(u => u.IdUsuario == id);
            if (usuarioOriginal == null)
                return NotFound(new { message = "Usuário não encontrado." });

            var usuarioParaAtualizar = await _context.Usuarios.FindAsync(id);
            if (usuarioParaAtualizar == null)
                return NotFound(new { message = "Usuário não encontrado (erro interno)." }); 

            var idDoUsuarioLogado = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(idDoUsuarioLogado, out int sindicoId))
                return Unauthorized("Token inválido.");


            if (usuarioParaAtualizar.IdUsuario == sindicoId && usuarioAtualizado.Status == "Inativo")
                return BadRequest(new { message = "Você não pode inativar a si mesmo." });

            var detalhesAlteracao = new Dictionary<string, object>();

            try
            {
                if (usuarioParaAtualizar.Nome != usuarioAtualizado.Nome) { detalhesAlteracao["Nome"] = new { Old = usuarioOriginal.Nome, New = usuarioAtualizado.Nome }; usuarioParaAtualizar.Nome = usuarioAtualizado.Nome; }
                if (usuarioParaAtualizar.Email != usuarioAtualizado.Email) { detalhesAlteracao["Email"] = new { Old = usuarioOriginal.Email, New = usuarioAtualizado.Email }; usuarioParaAtualizar.Email = usuarioAtualizado.Email; }
                if (!string.IsNullOrEmpty(usuarioAtualizado.Senha) && usuarioParaAtualizar.Senha != usuarioAtualizado.Senha) { detalhesAlteracao["Senha"] = new { Info = "Senha alterada" }; usuarioParaAtualizar.Senha = usuarioAtualizado.Senha; }
                if (usuarioParaAtualizar.Telefone != usuarioAtualizado.Telefone) { detalhesAlteracao["Telefone"] = new { Old = usuarioOriginal.Telefone, New = usuarioAtualizado.Telefone }; usuarioParaAtualizar.Telefone = usuarioAtualizado.Telefone; }
                if (usuarioParaAtualizar.Apartamento != usuarioAtualizado.Apartamento) { detalhesAlteracao["Apartamento"] = new { Old = usuarioOriginal.Apartamento, New = usuarioAtualizado.Apartamento }; usuarioParaAtualizar.Apartamento = usuarioAtualizado.Apartamento; }
                if (usuarioParaAtualizar.Status != usuarioAtualizado.Status && (usuarioAtualizado.Status == "Ativo" || usuarioAtualizado.Status == "Inativo")) { detalhesAlteracao["Status"] = new { Old = usuarioOriginal.Status, New = usuarioAtualizado.Status }; usuarioParaAtualizar.Status = usuarioAtualizado.Status; }

                if (detalhesAlteracao.Count > 0)
                {
                    _context.Usuarios.Update(usuarioParaAtualizar);
                    await _context.SaveChangesAsync();
                    await RegistrarHistoricoAsync(sindicoId, $"Síndico atualizou o usuário {usuarioParaAtualizar.Nome}", "Atualização de usuário", detalhes: detalhesAlteracao);
                }
                else
                {
                    return Ok(new { message = "Nenhuma alteração detectada.", usuario = usuarioParaAtualizar });
                }

                return Ok(new { message = "Usuário atualizado com sucesso!", usuario = usuarioParaAtualizar });
            }
            catch (DbUpdateException ex)
            {
                var innerExMessage = ex.InnerException?.Message ?? ex.Message;
                Console.WriteLine($"Erro DbUpdateException em AtualizarUsuario: {innerExMessage}");
                return StatusCode(400, new { message = "Erro ao salvar no banco de dados (verifique se o email já existe).", error = innerExMessage });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro Genérico em AtualizarUsuario: {ex.Message}");
                return StatusCode(500, new { message = "Erro inesperado ao atualizar usuário.", error = ex.Message });
            }
        }


        [HttpGet("usuarios")]
        public async Task<IActionResult> ListarUsuarios([FromQuery] string? status = null) 
        {
            try
            {
                var query = _context.Usuarios.AsQueryable();

                if (!string.IsNullOrEmpty(status) && status.ToLower() != "todos")
                {
                    query = query.Where(u => EF.Functions.Like(u.Status, status));
                }

                var usuarios = await query
                    .Select(u => new { u.IdUsuario, u.Nome, u.Email, u.Apartamento, u.TipoUsuario, u.Status }) 
                    .OrderBy(u => u.Nome)
                    .ToListAsync();

                return usuarios.Any()
                    ? Ok(usuarios)
                    : Ok(new { message = $"Nenhum usuário encontrado{(string.IsNullOrEmpty(status) || status.ToLower() == "todos" ? "" : $" com status '{status}'")}." });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro em ListarUsuarios: {ex.Message}");
                return StatusCode(500, new { message = "Erro ao listar usuários.", error = ex.Message });
            }
        }

        [HttpGet("usuarios/{id}")]
        public async Task<IActionResult> BuscarUsuarioPorId(int id)
        {
            try
            {
                var usuario = await _context.Usuarios
                                        .Select(u => new {
                                            u.IdUsuario,
                                            u.Nome,
                                            u.Email,
                                            u.Telefone,
                                            u.Apartamento,
                                            u.TipoUsuario,
                                            u.Status 
                                        })
                                        .FirstOrDefaultAsync(u => u.IdUsuario == id);

                if (usuario == null)
                    return NotFound(new { message = "Usuário não encontrado." });

                return Ok(usuario);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro em BuscarUsuarioPorId: {ex.Message}");
                return StatusCode(500, new { message = "Erro ao buscar usuário.", error = ex.Message });
            }
        }

        // --- OUTROS MÉTODOS (Logs) ---

        [HttpGet("logs")]
        public async Task<IActionResult> ConsultarLogs()
        {
            try
            {
                var historicos = await (from h in _context.Historicos
                                        join u in _context.Usuarios on h.IdUsuario equals u.IdUsuario into uhGroup
                                        from u in uhGroup.DefaultIfEmpty()
                                        join e in _context.Encomendas on h.IdEncomenda equals e.IdEncomenda into ehGroup
                                        from e in ehGroup.DefaultIfEmpty()
                                        orderby h.DataHora descending
                                        select new
                                        {
                                            h.IdHistorico,
                                            Usuario = u != null ? u.Nome : "Usuário removido",
                                            h.Acao,
                                            h.Tipo,
                                            h.DataHora,
                                            EncomendaDescricao = e != null ? e.Descricao : "Sem referência",
                                            EncomendaApartamento = e != null ? e.Apartamento : "Sem apartamento",
                                            h.Detalhes
                                        }).ToListAsync();

                if (!historicos.Any())
                    return Ok(new { message = "Nenhum histórico encontrado." });

                return Ok(historicos);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro em ConsultarLogs: {ex.InnerException?.Message ?? ex.Message}");
                return StatusCode(500, new { message = "Erro ao consultar logs.", error = ex.InnerException?.Message ?? ex.Message });
            }
        }

        [HttpGet("logs/{id}")]
        public async Task<IActionResult> BuscarLogPorId(int id)
        {
            try
            {
                var historico = await (from h in _context.Historicos
                                       join u in _context.Usuarios on h.IdUsuario equals u.IdUsuario into uhGroup
                                       from u in uhGroup.DefaultIfEmpty()
                                       join e in _context.Encomendas on h.IdEncomenda equals e.IdEncomenda into ehGroup
                                       from e in ehGroup.DefaultIfEmpty()
                                       where h.IdHistorico == id 
                                       select new
                                       {
                                           h.IdHistorico,
                                           h.IdUsuario,
                                           Usuario = u != null ? u.Nome : "Usuário removido",
                                           h.Acao,
                                           h.Tipo,
                                           h.DataHora,
                                           h.IdEncomenda,
                                           EncomendaDescricao = e != null ? e.Descricao : "Sem referência",
                                           EncomendaApartamento = e != null ? e.Apartamento : "Sem apartamento",
                                           h.Detalhes
                                       }).FirstOrDefaultAsync();

                if (historico == null)
                    return NotFound(new { message = "Registro de histórico não encontrado." });

                return Ok(historico);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro em BuscarLogPorId: {ex.InnerException?.Message ?? ex.Message}");
                return StatusCode(500, new { message = "Erro ao buscar registro.", error = ex.InnerException?.Message ?? ex.Message });
            }
        }
    }
}  