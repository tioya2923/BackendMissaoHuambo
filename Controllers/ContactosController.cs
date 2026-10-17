using System.Collections.Concurrent;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MissaoBackend.Data;
using MissaoBackend.Models;

namespace MissaoBackend.Controllers;

// Lista de pessoas com quem o utilizador partilha cânticos. Encontram-se por nome, mas só se
// tornam contacto depois de a outra pessoa aceitar o pedido. Quem desligar a pesquisa só se encontra
// pelo código. A pesquisa nunca mostra o email.
[ApiController]
[Route("api/contactos")]
[Authorize]
public class ContactosController : ControllerBase
{
    private readonly AppDbContext _db;
    public ContactosController(AppDbContext db) => _db = db;

    private const int MinLetrasPesquisa = 3;
    private const int MaxResultados = 20;
    private const int MaxPedidosPendentes = 50;
    private const int MaxContactos = 200;
    private const int MaxPesquisasJanela = 30;
    private static readonly TimeSpan JanelaPesquisa = TimeSpan.FromMinutes(10);
    private static readonly ConcurrentDictionary<int, List<DateTime>> Pesquisas = new();

    private int? UtilizadorId()
    {
        var claim = User.FindFirst("utilizadorId")?.Value;
        return int.TryParse(claim, out var id) ? id : null;
    }

    // Limita pesquisas para ninguém conseguir percorrer a lista de utilizadores.
    private static bool PesquisaPermitida(int utilizadorId)
    {
        var agora = DateTime.UtcNow;
        var lista = Pesquisas.GetOrAdd(utilizadorId, _ => new List<DateTime>());
        lock (lista)
        {
            lista.RemoveAll(t => agora - t > JanelaPesquisa);
            if (lista.Count >= MaxPesquisasJanela) return false;
            lista.Add(agora);
            return true;
        }
    }

    private static string EscaparLike(string s) => s.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");

    // ── Definições de visibilidade ───────────────────────────────────────────

    [HttpGet("definicoes")]
    public async Task<IActionResult> Definicoes()
    {
        var id = UtilizadorId();
        if (id == null) return Unauthorized();
        var u = await _db.Utilizadores.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        if (u == null) return Unauthorized();
        return Ok(new { visivelPesquisa = u.VisivelPesquisa, paroquia = u.Paroquia ?? "" });
    }

    public record DefinicoesRequest(bool VisivelPesquisa, string? Paroquia);

    [HttpPut("definicoes")]
    public async Task<IActionResult> GuardarDefinicoes([FromBody] DefinicoesRequest req)
    {
        var id = UtilizadorId();
        if (id == null) return Unauthorized();
        var u = await _db.Utilizadores.FindAsync(id);
        if (u == null) return Unauthorized();
        u.VisivelPesquisa = req.VisivelPesquisa;
        var paroquia = (req.Paroquia ?? string.Empty).Trim();
        if (paroquia.Length > 100) return BadRequest("O nome da paróquia é demasiado longo (máximo 100 caracteres).");
        u.Paroquia = paroquia.Length == 0 ? null : paroquia;
        await _db.SaveChangesAsync();
        return Ok(new { visivelPesquisa = u.VisivelPesquisa, paroquia = u.Paroquia ?? "" });
    }

    // ── Pesquisa ──────────────────────────────────────────────────────────────

    [HttpGet("pesquisa")]
    public async Task<IActionResult> Pesquisar([FromQuery] string? q)
    {
        var eu = UtilizadorId();
        if (eu == null) return Unauthorized();

        var termo = (q ?? string.Empty).Trim();
        if (termo.Length < MinLetrasPesquisa) return BadRequest($"Escreva pelo menos {MinLetrasPesquisa} letras do nome.");
        if (termo.Length > 60) termo = termo[..60];
        if (!PesquisaPermitida(eu.Value)) return StatusCode(429, "Fez demasiadas pesquisas. Tente daqui a alguns minutos.");

        var padrao = $"%{EscaparLike(termo)}%";
        var bloqueios = _db.BloqueiosUtilizador.AsNoTracking()
            .Where(b => b.UtilizadorId == eu || b.BloqueadoId == eu)
            .Select(b => b.UtilizadorId == eu ? b.BloqueadoId : b.UtilizadorId);

        var encontrados = await _db.Utilizadores.AsNoTracking()
            .Where(u => u.Id != eu && u.VisivelPesquisa && EF.Functions.Like(u.Nome, padrao) && !bloqueios.Contains(u.Id))
            .OrderBy(u => u.Nome)
            .Take(MaxResultados)
            .Select(u => new { u.Id, u.Nome, u.Paroquia })
            .ToListAsync();

        var ids = encontrados.Select(e => e.Id).ToList();
        var ligacoes = await _db.ContactosUtilizador.AsNoTracking()
            .Where(c => (c.SolicitanteId == eu && ids.Contains(c.DestinatarioId)) || (c.DestinatarioId == eu && ids.Contains(c.SolicitanteId)))
            .ToListAsync();

        var resultado = encontrados.Select(e =>
        {
            var l = ligacoes.FirstOrDefault(c => c.SolicitanteId == e.Id || c.DestinatarioId == e.Id);
            var estado = l == null ? "nenhum" : l.Aceite ? "contacto" : l.SolicitanteId == eu ? "enviado" : "recebido";
            return new { utilizadorId = e.Id, nome = e.Nome, paroquia = e.Paroquia, estado };
        });
        return Ok(resultado);
    }

    // ── Contactos e pedidos ──────────────────────────────────────────────────

    public record PessoaDto(int Id, int UtilizadorId, string Nome, string? Paroquia, string? Codigo, DateTime CriadoEm);

    [HttpGet]
    public async Task<IActionResult> Listar()
    {
        var eu = UtilizadorId();
        if (eu == null) return Unauthorized();

        var ligacoes = await _db.ContactosUtilizador.AsNoTracking()
            .Where(c => c.SolicitanteId == eu || c.DestinatarioId == eu)
            .ToListAsync();
        var outrosIds = ligacoes.Select(c => c.SolicitanteId == eu ? c.DestinatarioId : c.SolicitanteId).Distinct().ToList();
        var pessoas = await _db.Utilizadores.AsNoTracking()
            .Where(u => outrosIds.Contains(u.Id))
            .Select(u => new { u.Id, u.Nome, u.Paroquia, u.Codigo })
            .ToListAsync();

        PessoaDto? Mapa(ContactoUtilizador c)
        {
            var outro = c.SolicitanteId == eu ? c.DestinatarioId : c.SolicitanteId;
            var p = pessoas.FirstOrDefault(x => x.Id == outro);
            return p == null ? null : new PessoaDto(c.Id, p.Id, p.Nome, p.Paroquia, p.Codigo, c.CriadoEm);
        }

        List<PessoaDto> Lista(IEnumerable<ContactoUtilizador> l) => l.Select(Mapa).OfType<PessoaDto>().ToList();

        return Ok(new
        {
            contactos = Lista(ligacoes.Where(c => c.Aceite)).OrderBy(x => x.Nome).ToList(),
            recebidos = Lista(ligacoes.Where(c => !c.Aceite && c.DestinatarioId == eu)),
            enviados = Lista(ligacoes.Where(c => !c.Aceite && c.SolicitanteId == eu)),
        });
    }

    [HttpGet("pedidos/contagem")]
    public async Task<IActionResult> ContagemPedidos()
    {
        var eu = UtilizadorId();
        if (eu == null) return Unauthorized();
        var n = await _db.ContactosUtilizador.CountAsync(c => !c.Aceite && c.DestinatarioId == eu);
        return Ok(new { recebidos = n });
    }

    public record PedidoRequest(int UtilizadorId);

    [HttpPost("pedidos")]
    public async Task<IActionResult> Pedir([FromBody] PedidoRequest req)
    {
        var eu = UtilizadorId();
        if (eu == null) return Unauthorized();
        if (req.UtilizadorId == eu) return BadRequest("Não se pode adicionar a si próprio.");

        var alvo = await _db.Utilizadores.AsNoTracking().FirstOrDefaultAsync(u => u.Id == req.UtilizadorId);
        // Quem não existe, desligou a pesquisa ou bloqueou/foi bloqueado: responde igual, sem revelar nada.
        var bloqueado = await _db.BloqueiosUtilizador.AnyAsync(b =>
            (b.UtilizadorId == eu && b.BloqueadoId == req.UtilizadorId) || (b.UtilizadorId == req.UtilizadorId && b.BloqueadoId == eu));
        if (alvo == null || !alvo.VisivelPesquisa || bloqueado) return Ok(new { estado = "enviado" });

        var existente = await _db.ContactosUtilizador.FirstOrDefaultAsync(c =>
            (c.SolicitanteId == eu && c.DestinatarioId == req.UtilizadorId) || (c.SolicitanteId == req.UtilizadorId && c.DestinatarioId == eu));
        if (existente != null)
        {
            if (existente.Aceite) return Ok(new { estado = "contacto" });
            // A outra pessoa já tinha pedido: pedir de volta equivale a aceitar.
            if (existente.DestinatarioId == eu)
            {
                existente.Aceite = true;
                await _db.SaveChangesAsync();
                return Ok(new { estado = "contacto" });
            }
            return Ok(new { estado = "enviado" });
        }

        if (await _db.ContactosUtilizador.CountAsync(c => !c.Aceite && c.SolicitanteId == eu) >= MaxPedidosPendentes)
            return StatusCode(429, "Tem demasiados pedidos por responder. Aguarde que alguns sejam aceites ou cancele-os.");
        if (await _db.ContactosUtilizador.CountAsync(c => c.Aceite && (c.SolicitanteId == eu || c.DestinatarioId == eu)) >= MaxContactos)
            return BadRequest("Atingiu o limite de contactos.");

        _db.ContactosUtilizador.Add(new ContactoUtilizador { SolicitanteId = eu.Value, DestinatarioId = req.UtilizadorId });
        await _db.SaveChangesAsync();
        return Ok(new { estado = "enviado" });
    }

    [HttpPost("pedidos/{id:int}/aceitar")]
    public async Task<IActionResult> Aceitar(int id)
    {
        var eu = UtilizadorId();
        if (eu == null) return Unauthorized();
        var c = await _db.ContactosUtilizador.FirstOrDefaultAsync(x => x.Id == id && x.DestinatarioId == eu && !x.Aceite);
        if (c == null) return NotFound();
        c.Aceite = true;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // Recusa um pedido recebido ou cancela um pedido enviado.
    [HttpDelete("pedidos/{id:int}")]
    public async Task<IActionResult> RecusarOuCancelar(int id)
    {
        var eu = UtilizadorId();
        if (eu == null) return Unauthorized();
        var c = await _db.ContactosUtilizador.FirstOrDefaultAsync(x => x.Id == id && !x.Aceite && (x.SolicitanteId == eu || x.DestinatarioId == eu));
        if (c == null) return NotFound();
        _db.ContactosUtilizador.Remove(c);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{utilizadorId:int}")]
    public async Task<IActionResult> Remover(int utilizadorId)
    {
        var eu = UtilizadorId();
        if (eu == null) return Unauthorized();
        var c = await _db.ContactosUtilizador.FirstOrDefaultAsync(x => x.Aceite &&
            ((x.SolicitanteId == eu && x.DestinatarioId == utilizadorId) || (x.SolicitanteId == utilizadorId && x.DestinatarioId == eu)));
        if (c == null) return NotFound();
        _db.ContactosUtilizador.Remove(c);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
