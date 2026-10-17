using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MissaoBackend.Data;
using MissaoBackend.Models;
using MissaoBackend.Services;

namespace MissaoBackend.Controllers;

// Grupos (coral, oração, pastoral…) para partilhar avisos, cânticos, orações e programas da Missa.
// Só se entra por convite de um administrador ou com o código do grupo (aprovado por um administrador).
// Os grupos não aparecem em pesquisas. Não há comentários nem conversa: é um mural.
[ApiController]
[Route("api/grupos")]
[Authorize]
public class GruposController : ControllerBase
{
    private readonly AppDbContext _db;
    public GruposController(AppDbContext db) => _db = db;

    private const int MaxGruposCriados = 5;
    private const int MaxGruposPorPessoa = 20;
    private const int MaxMembros = 300;
    private const int MaxFixadas = 3;
    private const int MaxPublicacoesPorDia = 30;
    private const int MaxItensPrograma = 60;
    private const int MaxTitulo = 120;
    private const int MaxTexto = 20_000;
    private const int MaxTextoAviso = 5_000;
    private const int MaxConvitesDeUmaVez = 30;
    private const int MaxTentativasCodigo = 10;
    private static readonly TimeSpan JanelaCodigo = TimeSpan.FromMinutes(10);
    private static readonly ConcurrentDictionary<int, List<DateTime>> TentativasCodigo = new();
    private static readonly string[] TiposGrupo = { "coral", "oracao", "pastoral", "juventude", "outro" };
    private const string Alfabeto = "23456789ABCDEFGHJKMNPQRSTUVWXYZ";

    private static readonly JsonSerializerOptions JsonOpcoes = new(JsonSerializerDefaults.Web);

    private int? UtilizadorId()
    {
        var claim = User.FindFirst("utilizadorId")?.Value;
        return int.TryParse(claim, out var id) ? id : null;
    }

    private static string GerarCodigo()
    {
        Span<char> c = stackalloc char[5];
        for (var i = 0; i < c.Length; i++) c[i] = Alfabeto[RandomNumberGenerator.GetInt32(Alfabeto.Length)];
        return "GRP-" + new string(c);
    }

    private async Task<string> CodigoNovo()
    {
        for (var i = 0; i < 20; i++)
        {
            var c = GerarCodigo();
            if (!await _db.Grupos.AnyAsync(g => g.Codigo == c)) return c;
        }
        throw new InvalidOperationException("Não foi possível gerar um código.");
    }

    private static string NormalizarCodigo(string? s)
    {
        var t = (s ?? string.Empty).Trim().ToUpperInvariant().Replace(" ", "");
        if (t.Length == 0) return t;
        return t.StartsWith("GRP-") ? t : t.StartsWith("GRP") ? "GRP-" + t[3..] : "GRP-" + t;
    }

    private static bool TentativaPermitida(int utilizadorId)
    {
        var agora = DateTime.UtcNow;
        var lista = TentativasCodigo.GetOrAdd(utilizadorId, _ => new List<DateTime>());
        lock (lista)
        {
            lista.RemoveAll(t => agora - t > JanelaCodigo);
            if (lista.Count >= MaxTentativasCodigo) return false;
            lista.Add(agora);
            return true;
        }
    }

    private Task<MembroGrupo?> MeuMembro(int grupoId, int eu) =>
        _db.MembrosGrupo.FirstOrDefaultAsync(m => m.GrupoId == grupoId && m.UtilizadorId == eu && m.Estado == EstadoMembro.Activo);

    private static string Limpar(string? s, int max) { var t = (s ?? string.Empty).Trim(); return t.Length > max ? t[..max] : t; }

    // ── Os meus grupos ────────────────────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> Listar()
    {
        var eu = UtilizadorId();
        if (eu == null) return Unauthorized();

        var meus = await (
            from m in _db.MembrosGrupo.AsNoTracking()
            where m.UtilizadorId == eu && m.Estado == EstadoMembro.Activo
            join g in _db.Grupos.AsNoTracking() on m.GrupoId equals g.Id
            orderby g.Nome
            select new
            {
                g.Id, g.Nome, g.Tipo, g.Descricao, g.Paroquia,
                administrador = m.Administrador,
                membros = _db.MembrosGrupo.Count(x => x.GrupoId == g.Id && x.Estado == EstadoMembro.Activo),
                naoLidas = _db.PublicacoesGrupo.Count(p => p.GrupoId == g.Id && p.Id > m.UltimaLeituraId && p.AutorId != eu),
                pedidos = m.Administrador ? _db.MembrosGrupo.Count(x => x.GrupoId == g.Id && x.Estado == EstadoMembro.Pedido) : 0,
            }).ToListAsync();

        var convites = await (
            from m in _db.MembrosGrupo.AsNoTracking()
            where m.UtilizadorId == eu && m.Estado == EstadoMembro.Convidado
            join g in _db.Grupos.AsNoTracking() on m.GrupoId equals g.Id
            select new { g.Id, g.Nome, g.Tipo, g.Descricao, g.Paroquia }).ToListAsync();

        var aguardam = await (
            from m in _db.MembrosGrupo.AsNoTracking()
            where m.UtilizadorId == eu && m.Estado == EstadoMembro.Pedido
            join g in _db.Grupos.AsNoTracking() on m.GrupoId equals g.Id
            select new { g.Id, g.Nome, g.Tipo }).ToListAsync();

        return Ok(new { grupos = meus, convites, aguardam });
    }

    // Número total de novidades (para o aviso no cartão "Grupos").
    [HttpGet("resumo")]
    public async Task<IActionResult> Resumo()
    {
        var eu = UtilizadorId();
        if (eu == null) return Unauthorized();
        var activos = await _db.MembrosGrupo.AsNoTracking().Where(m => m.UtilizadorId == eu && m.Estado == EstadoMembro.Activo).ToListAsync();
        var naoLidas = 0;
        foreach (var m in activos)
            naoLidas += await _db.PublicacoesGrupo.CountAsync(p => p.GrupoId == m.GrupoId && p.Id > m.UltimaLeituraId && p.AutorId != eu);
        var adminIds = activos.Where(m => m.Administrador).Select(m => m.GrupoId).ToList();
        var pedidos = adminIds.Count == 0 ? 0 : await _db.MembrosGrupo.CountAsync(m => adminIds.Contains(m.GrupoId) && m.Estado == EstadoMembro.Pedido);
        var convites = await _db.MembrosGrupo.CountAsync(m => m.UtilizadorId == eu && m.Estado == EstadoMembro.Convidado);
        return Ok(new { naoLidas, pedidos, convites });
    }

    public record GrupoRequest(string? Nome, string? Tipo, string? Descricao, string? Paroquia);

    [HttpPost]
    public async Task<IActionResult> Criar([FromBody] GrupoRequest req)
    {
        var eu = UtilizadorId();
        if (eu == null) return Unauthorized();
        var nome = Limpar(req.Nome, 100);
        if (nome.Length < 2) return BadRequest("Indique o nome do grupo.");
        var tipo = (req.Tipo ?? "outro").Trim().ToLowerInvariant();
        if (!TiposGrupo.Contains(tipo)) tipo = "outro";

        if (await _db.Grupos.CountAsync(g => g.CriadoPorId == eu) >= MaxGruposCriados)
            return BadRequest($"Pode criar no máximo {MaxGruposCriados} grupos.");
        if (await _db.MembrosGrupo.CountAsync(m => m.UtilizadorId == eu && m.Estado == EstadoMembro.Activo) >= MaxGruposPorPessoa)
            return BadRequest($"Pode pertencer a no máximo {MaxGruposPorPessoa} grupos.");

        var g = new Grupo
        {
            Nome = nome, Tipo = tipo, Codigo = await CodigoNovo(), CriadoPorId = eu,
            Descricao = Limpar(req.Descricao, 500) is { Length: > 0 } d ? d : null,
            Paroquia = Limpar(req.Paroquia, 100) is { Length: > 0 } p ? p : null,
        };
        _db.Grupos.Add(g);
        await _db.SaveChangesAsync();
        _db.MembrosGrupo.Add(new MembroGrupo { GrupoId = g.Id, UtilizadorId = eu.Value, Administrador = true, Estado = EstadoMembro.Activo, EntrouEm = DateTime.UtcNow });
        await _db.SaveChangesAsync();
        return Ok(new { g.Id });
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Detalhe(int id)
    {
        var eu = UtilizadorId();
        if (eu == null) return Unauthorized();
        var m = await MeuMembro(id, eu.Value);
        var g = m == null ? null : await _db.Grupos.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        if (m == null || g == null) return NotFound();
        return Ok(new
        {
            g.Id, g.Nome, g.Tipo, g.Descricao, g.Paroquia, g.MembrosPublicam,
            administrador = m.Administrador,
            criador = g.CriadoPorId == eu,
            // O código só é mostrado a quem administra.
            codigo = m.Administrador ? g.Codigo : null,
            codigoActivo = m.Administrador ? (bool?)g.CodigoActivo : null,
            membros = await _db.MembrosGrupo.CountAsync(x => x.GrupoId == id && x.Estado == EstadoMembro.Activo),
        });
    }

    public record DefinicoesGrupoRequest(string? Nome, string? Tipo, string? Descricao, string? Paroquia, bool MembrosPublicam, bool CodigoActivo);

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Editar(int id, [FromBody] DefinicoesGrupoRequest req)
    {
        var eu = UtilizadorId();
        if (eu == null) return Unauthorized();
        var m = await MeuMembro(id, eu.Value);
        if (m == null) return NotFound();
        if (!m.Administrador) return Forbid();
        var g = await _db.Grupos.FindAsync(id);
        if (g == null) return NotFound();

        var nome = Limpar(req.Nome, 100);
        if (nome.Length < 2) return BadRequest("Indique o nome do grupo.");
        var tipo = (req.Tipo ?? g.Tipo).Trim().ToLowerInvariant();
        g.Nome = nome;
        g.Tipo = TiposGrupo.Contains(tipo) ? tipo : g.Tipo;
        g.Descricao = Limpar(req.Descricao, 500) is { Length: > 0 } d ? d : null;
        g.Paroquia = Limpar(req.Paroquia, 100) is { Length: > 0 } p ? p : null;
        g.MembrosPublicam = req.MembrosPublicam;
        g.CodigoActivo = req.CodigoActivo;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost("{id:int}/codigo/renovar")]
    public async Task<IActionResult> RenovarCodigo(int id)
    {
        var eu = UtilizadorId();
        if (eu == null) return Unauthorized();
        var m = await MeuMembro(id, eu.Value);
        if (m == null) return NotFound();
        if (!m.Administrador) return Forbid();
        var g = await _db.Grupos.FindAsync(id);
        if (g == null) return NotFound();
        g.Codigo = await CodigoNovo();
        g.CodigoActivo = true;
        await _db.SaveChangesAsync();
        return Ok(new { codigo = g.Codigo });
    }

    // Só quem criou o grupo o pode apagar (ou qualquer administrador, se o criador já saiu).
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Apagar(int id)
    {
        var eu = UtilizadorId();
        if (eu == null) return Unauthorized();
        var m = await MeuMembro(id, eu.Value);
        if (m == null) return NotFound();
        var g = await _db.Grupos.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        if (g == null) return NotFound();
        if (!m.Administrador || (g.CriadoPorId != null && g.CriadoPorId != eu)) return Forbid();
        var audios = await GruposRegras.ApagarGrupo(_db, id);
        await GruposRegras.LimparAudiosSemUso(_db, audios);
        return NoContent();
    }

    [HttpPost("{id:int}/sair")]
    public async Task<IActionResult> Sair(int id)
    {
        var eu = UtilizadorId();
        if (eu == null) return Unauthorized();
        if (!await _db.MembrosGrupo.AnyAsync(m => m.GrupoId == id && m.UtilizadorId == eu && m.Estado == EstadoMembro.Activo)) return NotFound();
        await GruposRegras.RetirarMembro(_db, id, eu.Value);
        return NoContent();
    }

    // ── Membros, convites e pedidos ──────────────────────────────────────────

    [HttpGet("{id:int}/membros")]
    public async Task<IActionResult> Membros(int id)
    {
        var eu = UtilizadorId();
        if (eu == null) return Unauthorized();
        var meu = await MeuMembro(id, eu.Value);
        if (meu == null) return NotFound();

        var todos = await (
            from m in _db.MembrosGrupo.AsNoTracking()
            where m.GrupoId == id
            join u in _db.Utilizadores.AsNoTracking() on m.UtilizadorId equals u.Id
            select new { m.UtilizadorId, u.Nome, u.Paroquia, m.Administrador, m.Estado, m.EntrouEm, m.CriadoEm }).ToListAsync();

        var membros = todos.Where(x => x.Estado == EstadoMembro.Activo)
            .OrderByDescending(x => x.Administrador).ThenBy(x => x.Nome)
            .Select(x => new PessoaGrupoDto(x.UtilizadorId, x.Nome, x.Paroquia, x.Administrador, x.UtilizadorId == eu)).ToList();
        var pedidos = meu.Administrador
            ? todos.Where(x => x.Estado == EstadoMembro.Pedido).OrderBy(x => x.CriadoEm).Select(x => new PessoaGrupoDto(x.UtilizadorId, x.Nome, x.Paroquia, false, false)).ToList()
            : new List<PessoaGrupoDto>();
        var convidados = meu.Administrador
            ? todos.Where(x => x.Estado == EstadoMembro.Convidado).OrderBy(x => x.CriadoEm).Select(x => new PessoaGrupoDto(x.UtilizadorId, x.Nome, x.Paroquia, false, false)).ToList()
            : new List<PessoaGrupoDto>();
        return Ok(new { membros, pedidos, convidados });
    }

    public record PessoaGrupoDto(int UtilizadorId, string Nome, string? Paroquia, bool Administrador, bool EuProprio);

    public record ConvidarRequest(List<int>? UtilizadorIds);

    [HttpPost("{id:int}/convidar")]
    public async Task<IActionResult> Convidar(int id, [FromBody] ConvidarRequest req)
    {
        var eu = UtilizadorId();
        if (eu == null) return Unauthorized();
        var meu = await MeuMembro(id, eu.Value);
        if (meu == null) return NotFound();
        if (!meu.Administrador) return Forbid();

        var ids = (req.UtilizadorIds ?? new()).Distinct().Where(x => x != eu).ToList();
        if (ids.Count == 0) return BadRequest("Escolha pelo menos um contacto.");
        if (ids.Count > MaxConvitesDeUmaVez) return BadRequest($"Pode convidar no máximo {MaxConvitesDeUmaVez} pessoas de cada vez.");

        // Só se convidam contactos (quem já aceitou ligar-se a si).
        var contactos = await _db.ContactosUtilizador.AsNoTracking()
            .Where(c => c.Aceite && ((c.SolicitanteId == eu && ids.Contains(c.DestinatarioId)) || (c.DestinatarioId == eu && ids.Contains(c.SolicitanteId))))
            .Select(c => c.SolicitanteId == eu ? c.DestinatarioId : c.SolicitanteId).ToListAsync();
        var existentes = await _db.MembrosGrupo.Where(m => m.GrupoId == id && ids.Contains(m.UtilizadorId)).ToListAsync();
        var activos = await _db.MembrosGrupo.CountAsync(m => m.GrupoId == id && m.Estado == EstadoMembro.Activo);

        var convidados = 0; var ignorados = new List<int>();
        foreach (var uid in ids)
        {
            var ex = existentes.FirstOrDefault(m => m.UtilizadorId == uid);
            if (!contactos.Contains(uid) || (ex != null && ex.Estado != EstadoMembro.Pedido)) { ignorados.Add(uid); continue; }
            if (ex != null) // já tinha pedido para entrar: o convite equivale a aprovar
            {
                if (activos >= MaxMembros) { ignorados.Add(uid); continue; }
                ex.Estado = EstadoMembro.Activo; ex.EntrouEm = DateTime.UtcNow; activos++;
            }
            else _db.MembrosGrupo.Add(new MembroGrupo { GrupoId = id, UtilizadorId = uid, Estado = EstadoMembro.Convidado });
            convidados++;
        }
        await _db.SaveChangesAsync();
        return Ok(new { convidados, ignorados });
    }

    public record EntrarRequest(string? Codigo);

    [HttpPost("entrar")]
    public async Task<IActionResult> EntrarComCodigo([FromBody] EntrarRequest req)
    {
        var eu = UtilizadorId();
        if (eu == null) return Unauthorized();
        if (!TentativaPermitida(eu.Value)) return StatusCode(429, "Demasiadas tentativas. Tente daqui a alguns minutos.");
        var codigo = NormalizarCodigo(req.Codigo);
        var g = codigo.Length < 5 ? null : await _db.Grupos.AsNoTracking().FirstOrDefaultAsync(x => x.Codigo == codigo && x.CodigoActivo);
        if (g == null) return NotFound("Código inválido ou desativado.");

        var ex = await _db.MembrosGrupo.FirstOrDefaultAsync(m => m.GrupoId == g.Id && m.UtilizadorId == eu);
        if (ex != null)
        {
            if (ex.Estado == EstadoMembro.Activo) return Ok(new { estado = "membro", grupoId = g.Id, nome = g.Nome });
            if (ex.Estado == EstadoMembro.Convidado) // foi convidado e usou o código: entra logo
            {
                var r = await Ativar(ex, g.Id, eu.Value);
                return r ?? Ok(new { estado = "membro", grupoId = g.Id, nome = g.Nome });
            }
            return Ok(new { estado = "pedido", grupoId = g.Id, nome = g.Nome });
        }
        if (await _db.MembrosGrupo.CountAsync(m => m.UtilizadorId == eu && m.Estado == EstadoMembro.Pedido) >= 10)
            return BadRequest("Tem demasiados pedidos por responder.");
        _db.MembrosGrupo.Add(new MembroGrupo { GrupoId = g.Id, UtilizadorId = eu.Value, Estado = EstadoMembro.Pedido });
        await _db.SaveChangesAsync();
        return Ok(new { estado = "pedido", grupoId = g.Id, nome = g.Nome });
    }

    private async Task<IActionResult?> Ativar(MembroGrupo m, int grupoId, int utilizadorId)
    {
        if (await _db.MembrosGrupo.CountAsync(x => x.UtilizadorId == utilizadorId && x.Estado == EstadoMembro.Activo) >= MaxGruposPorPessoa)
            return BadRequest($"Pode pertencer a no máximo {MaxGruposPorPessoa} grupos.");
        if (await _db.MembrosGrupo.CountAsync(x => x.GrupoId == grupoId && x.Estado == EstadoMembro.Activo) >= MaxMembros)
            return BadRequest("O grupo atingiu o limite de membros.");
        m.Estado = EstadoMembro.Activo;
        m.EntrouEm = DateTime.UtcNow;
        // Quem acabou de entrar não vê o histórico antigo como "novo".
        m.UltimaLeituraId = await _db.PublicacoesGrupo.Where(p => p.GrupoId == grupoId).Select(p => (int?)p.Id).MaxAsync() ?? 0;
        await _db.SaveChangesAsync();
        return null;
    }

    [HttpPost("{id:int}/convite/aceitar")]
    public async Task<IActionResult> AceitarConvite(int id)
    {
        var eu = UtilizadorId();
        if (eu == null) return Unauthorized();
        var m = await _db.MembrosGrupo.FirstOrDefaultAsync(x => x.GrupoId == id && x.UtilizadorId == eu && x.Estado == EstadoMembro.Convidado);
        if (m == null) return NotFound();
        return await Ativar(m, id, eu.Value) ?? NoContent();
    }

    // Recusar um convite ou cancelar o meu pedido de entrada.
    [HttpDelete("{id:int}/convite")]
    public async Task<IActionResult> RecusarConviteOuPedido(int id)
    {
        var eu = UtilizadorId();
        if (eu == null) return Unauthorized();
        var m = await _db.MembrosGrupo.FirstOrDefaultAsync(x => x.GrupoId == id && x.UtilizadorId == eu && x.Estado != EstadoMembro.Activo);
        if (m == null) return NotFound();
        _db.MembrosGrupo.Remove(m);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost("{id:int}/pedidos/{utilizadorId:int}/aprovar")]
    public async Task<IActionResult> AprovarPedido(int id, int utilizadorId)
    {
        var eu = UtilizadorId();
        if (eu == null) return Unauthorized();
        var meu = await MeuMembro(id, eu.Value);
        if (meu == null) return NotFound();
        if (!meu.Administrador) return Forbid();
        var m = await _db.MembrosGrupo.FirstOrDefaultAsync(x => x.GrupoId == id && x.UtilizadorId == utilizadorId && x.Estado == EstadoMembro.Pedido);
        if (m == null) return NotFound();
        return await Ativar(m, id, utilizadorId) ?? NoContent();
    }

    // Recusar um pedido (administrador) ou cancelar um convite enviado.
    [HttpDelete("{id:int}/pedidos/{utilizadorId:int}")]
    public async Task<IActionResult> RecusarPedido(int id, int utilizadorId)
    {
        var eu = UtilizadorId();
        if (eu == null) return Unauthorized();
        var meu = await MeuMembro(id, eu.Value);
        if (meu == null) return NotFound();
        if (!meu.Administrador) return Forbid();
        var m = await _db.MembrosGrupo.FirstOrDefaultAsync(x => x.GrupoId == id && x.UtilizadorId == utilizadorId && x.Estado != EstadoMembro.Activo);
        if (m == null) return NotFound();
        _db.MembrosGrupo.Remove(m);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // Um administrador remove um membro (não remove outros administradores: primeiro retira-lhes a administração).
    [HttpDelete("{id:int}/membros/{utilizadorId:int}")]
    public async Task<IActionResult> RemoverMembro(int id, int utilizadorId)
    {
        var eu = UtilizadorId();
        if (eu == null) return Unauthorized();
        var meu = await MeuMembro(id, eu.Value);
        if (meu == null) return NotFound();
        if (!meu.Administrador) return Forbid();
        if (utilizadorId == eu) return BadRequest("Para sair do grupo use «Sair do grupo».");
        var alvo = await MeuMembro(id, utilizadorId);
        if (alvo == null) return NotFound();
        if (alvo.Administrador) return BadRequest("Retire primeiro a administração a esta pessoa.");
        await GruposRegras.RetirarMembro(_db, id, utilizadorId);
        return NoContent();
    }

    public record AdminRequest(bool Valor);

    [HttpPost("{id:int}/membros/{utilizadorId:int}/administrador")]
    public async Task<IActionResult> DefinirAdministrador(int id, int utilizadorId, [FromBody] AdminRequest req)
    {
        var eu = UtilizadorId();
        if (eu == null) return Unauthorized();
        var meu = await MeuMembro(id, eu.Value);
        if (meu == null) return NotFound();
        if (!meu.Administrador) return Forbid();
        var alvo = await MeuMembro(id, utilizadorId);
        if (alvo == null) return NotFound();
        var g = await _db.Grupos.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        if (!req.Valor && (g?.CriadoPorId == utilizadorId)) return BadRequest("Quem criou o grupo mantém sempre a administração.");
        if (!req.Valor && utilizadorId == eu) return BadRequest("Para deixar a administração, saia do grupo.");
        alvo.Administrador = req.Valor;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // ── Publicações (mural) ──────────────────────────────────────────────────

    public record ItemPrograma(string? Momento, string? Tipo, string? Titulo, string? Idioma, string? Slug, string? Letra, int? AudioId);

    public record PublicarRequest(string? Tipo, string? Titulo, string? Texto, string? Momento, int? AudioId, List<ItemPrograma>? Itens);

    [HttpPost("{id:int}/publicacoes")]
    public async Task<IActionResult> Publicar(int id, [FromBody] PublicarRequest req)
    {
        var eu = UtilizadorId();
        if (eu == null) return Unauthorized();
        var m = await MeuMembro(id, eu.Value);
        var g = m == null ? null : await _db.Grupos.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        if (m == null || g == null) return NotFound();

        var tipo = (req.Tipo ?? string.Empty).Trim().ToLowerInvariant();
        if (!TipoPublicacao.Todos.Contains(tipo)) return BadRequest("Tipo de publicação inválido.");
        var podeMembro = tipo is TipoPublicacao.Cantico or TipoPublicacao.Oracao;
        if (!m.Administrador && !(podeMembro && g.MembrosPublicam))
            return StatusCode(403, "Só os administradores do grupo podem fazer este tipo de publicação.");

        var titulo = Limpar(req.Titulo, MaxTitulo);
        if (titulo.Length == 0) return BadRequest("Indique o título.");
        var texto = (req.Texto ?? string.Empty).Trim();
        if (texto.Length > (tipo == TipoPublicacao.Aviso ? MaxTextoAviso : MaxTexto)) return BadRequest("O texto é demasiado longo.");

        var audioIds = new List<int>();
        string? itensJson = null;
        switch (tipo)
        {
            case TipoPublicacao.Oracao when texto.Length == 0:
                return BadRequest("Escreva o texto da oração.");
            case TipoPublicacao.Cantico:
                if (texto.Length == 0 && req.AudioId == null) return BadRequest("Envie a letra, a gravação ou ambas.");
                if (req.AudioId != null) audioIds.Add(req.AudioId.Value);
                break;
            case TipoPublicacao.Programa:
            {
                var itens = req.Itens ?? new();
                if (itens.Count == 0) return BadRequest("O programa não tem cânticos.");
                if (itens.Count > MaxItensPrograma) return BadRequest($"O programa pode ter no máximo {MaxItensPrograma} cânticos.");
                var limpos = new List<ItemPrograma>();
                foreach (var it in itens)
                {
                    var t = Limpar(it.Titulo, MaxTitulo);
                    var tp = (it.Tipo ?? string.Empty).Trim().ToLowerInvariant();
                    if (t.Length == 0 || (tp != "app" && tp != "proprio")) return BadRequest("Há um cântico inválido no programa.");
                    var letra = tp == "proprio" ? (it.Letra ?? string.Empty).Trim() : null;
                    if (letra != null && letra.Length > MaxTexto) return BadRequest("Uma das letras é demasiado longa.");
                    if (tp == "app" && (string.IsNullOrWhiteSpace(it.Idioma) || string.IsNullOrWhiteSpace(it.Slug))) return BadRequest("Há um cântico inválido no programa.");
                    if (tp == "proprio" && it.AudioId != null) audioIds.Add(it.AudioId.Value);
                    limpos.Add(new ItemPrograma(Limpar(it.Momento, 60), tp, t,
                        tp == "app" ? Limpar(it.Idioma, 20) : null, tp == "app" ? Limpar(it.Slug, 200) : null,
                        letra, tp == "proprio" ? it.AudioId : null));
                }
                itensJson = JsonSerializer.Serialize(limpos, JsonOpcoes);
                break;
            }
        }

        audioIds = audioIds.Distinct().ToList();
        if (audioIds.Count > 0)
        {
            var meus = await _db.AudiosPartilha.CountAsync(a => audioIds.Contains(a.Id) && a.DonoId == eu);
            if (meus != audioIds.Count) return BadRequest("Gravação inválida.");
        }

        var desde = DateTime.UtcNow.AddDays(-1);
        if (await _db.PublicacoesGrupo.CountAsync(p => p.AutorId == eu && p.CriadoEm >= desde) >= MaxPublicacoesPorDia)
            return StatusCode(429, "Atingiu o limite de publicações de hoje. Tente amanhã.");

        var pub = new PublicacaoGrupo
        {
            GrupoId = id, AutorId = eu.Value, Tipo = tipo, Titulo = titulo, Texto = texto,
            Momento = string.IsNullOrWhiteSpace(req.Momento) ? null : Limpar(req.Momento, 60), ItensJson = itensJson,
        };
        _db.PublicacoesGrupo.Add(pub);
        await _db.SaveChangesAsync();
        foreach (var a in audioIds) _db.PublicacoesAudio.Add(new PublicacaoAudio { PublicacaoId = pub.Id, AudioId = a });
        await _db.SaveChangesAsync();
        return Ok(new { pub.Id });
    }

    private static object? ParseItens(string? json) =>
        string.IsNullOrEmpty(json) ? null : JsonSerializer.Deserialize<JsonElement>(json);

    [HttpGet("{id:int}/publicacoes")]
    public async Task<IActionResult> Publicacoes(int id)
    {
        var eu = UtilizadorId();
        if (eu == null) return Unauthorized();
        var m = await MeuMembro(id, eu.Value);
        if (m == null) return NotFound();

        var linhas = await (
            from p in _db.PublicacoesGrupo.AsNoTracking()
            where p.GrupoId == id
            join u in _db.Utilizadores.AsNoTracking() on p.AutorId equals u.Id
            orderby p.Fixada descending, p.CriadoEm descending
            select new { p, autorNome = u.Nome }).Take(100).ToListAsync();

        var ids = linhas.Select(l => l.p.Id).ToList();
        var audios = await _db.PublicacoesAudio.AsNoTracking().Where(a => ids.Contains(a.PublicacaoId)).ToListAsync();
        var jaVistos = await _db.VistosPublicacao.AsNoTracking().Where(v => ids.Contains(v.PublicacaoId) && v.UtilizadorId == eu).Select(v => v.PublicacaoId).ToListAsync();
        var totalVistos = await _db.VistosPublicacao.AsNoTracking().Where(v => ids.Contains(v.PublicacaoId))
            .GroupBy(v => v.PublicacaoId).Select(gp => new { gp.Key, N = gp.Count() }).ToListAsync();

        var ultima = m.UltimaLeituraId;
        var resultado = linhas.Select(l => new
        {
            l.p.Id, l.p.Tipo, l.p.Titulo, l.p.Texto, l.p.Momento, l.p.Fixada, l.p.CriadoEm, l.p.EditadoEm,
            autorId = l.p.AutorId, autorNome = l.autorNome, minha = l.p.AutorId == eu,
            novo = l.p.Id > ultima && l.p.AutorId != eu,
            itens = ParseItens(l.p.ItensJson),
            audioIds = audios.Where(a => a.PublicacaoId == l.p.Id).Select(a => a.AudioId).ToList(),
            vistos = l.p.Tipo == TipoPublicacao.Aviso && (m.Administrador || l.p.AutorId == eu) ? totalVistos.FirstOrDefault(t => t.Key == l.p.Id)?.N ?? 0 : (int?)null,
        }).ToList();

        // Marca como lidos os avisos que esta pessoa acabou de receber e actualiza o que está por ler.
        var novosAvisos = linhas.Where(l => l.p.Tipo == TipoPublicacao.Aviso && l.p.AutorId != eu && !jaVistos.Contains(l.p.Id)).Select(l => l.p.Id).ToList();
        foreach (var pid in novosAvisos) _db.VistosPublicacao.Add(new VistoPublicacao { PublicacaoId = pid, UtilizadorId = eu.Value });
        var maxId = await _db.PublicacoesGrupo.Where(p => p.GrupoId == id).Select(p => (int?)p.Id).MaxAsync() ?? 0;
        if (maxId > m.UltimaLeituraId) m.UltimaLeituraId = maxId;
        try { await _db.SaveChangesAsync(); }
        catch (DbUpdateException) { /* pedido repetido em paralelo: o registo de "visto" já existe */ }
        return Ok(resultado);
    }

    private async Task<(MembroGrupo? membro, PublicacaoGrupo? pub)> PublicacaoDoMembro(int grupoId, int pid, int eu)
    {
        var m = await MeuMembro(grupoId, eu);
        if (m == null) return (null, null);
        var p = await _db.PublicacoesGrupo.FirstOrDefaultAsync(x => x.Id == pid && x.GrupoId == grupoId);
        return (m, p);
    }

    public record EditarPublicacaoRequest(string? Titulo, string? Texto);

    [HttpPut("{id:int}/publicacoes/{pid:int}")]
    public async Task<IActionResult> EditarPublicacao(int id, int pid, [FromBody] EditarPublicacaoRequest req)
    {
        var eu = UtilizadorId();
        if (eu == null) return Unauthorized();
        var (m, p) = await PublicacaoDoMembro(id, pid, eu.Value);
        if (m == null || p == null) return NotFound();
        if (!m.Administrador && p.AutorId != eu) return Forbid();
        var titulo = Limpar(req.Titulo, MaxTitulo);
        var texto = (req.Texto ?? string.Empty).Trim();
        if (titulo.Length == 0) return BadRequest("Indique o título.");
        if (texto.Length > (p.Tipo == TipoPublicacao.Aviso ? MaxTextoAviso : MaxTexto)) return BadRequest("O texto é demasiado longo.");
        if (p.Tipo == TipoPublicacao.Oracao && texto.Length == 0) return BadRequest("Escreva o texto da oração.");
        p.Titulo = titulo; p.Texto = texto; p.EditadoEm = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    public record FixarRequest(bool Fixada);

    [HttpPost("{id:int}/publicacoes/{pid:int}/fixar")]
    public async Task<IActionResult> Fixar(int id, int pid, [FromBody] FixarRequest req)
    {
        var eu = UtilizadorId();
        if (eu == null) return Unauthorized();
        var (m, p) = await PublicacaoDoMembro(id, pid, eu.Value);
        if (m == null || p == null) return NotFound();
        if (!m.Administrador) return Forbid();
        if (req.Fixada && !p.Fixada && await _db.PublicacoesGrupo.CountAsync(x => x.GrupoId == id && x.Fixada) >= MaxFixadas)
            return BadRequest($"Pode fixar no máximo {MaxFixadas} publicações.");
        p.Fixada = req.Fixada;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:int}/publicacoes/{pid:int}")]
    public async Task<IActionResult> ApagarPublicacao(int id, int pid)
    {
        var eu = UtilizadorId();
        if (eu == null) return Unauthorized();
        var (m, p) = await PublicacaoDoMembro(id, pid, eu.Value);
        if (m == null || p == null) return NotFound();
        if (!m.Administrador && p.AutorId != eu) return Forbid();
        var audios = await _db.PublicacoesAudio.Where(a => a.PublicacaoId == pid).Select(a => a.AudioId).ToListAsync();
        _db.PublicacoesGrupo.Remove(p);
        await _db.SaveChangesAsync();
        await GruposRegras.LimparAudiosSemUso(_db, audios);
        return NoContent();
    }

    public record DenunciarRequest(string? Motivo);

    [HttpPost("{id:int}/publicacoes/{pid:int}/denunciar")]
    public async Task<IActionResult> Denunciar(int id, int pid, [FromBody] DenunciarRequest req)
    {
        var eu = UtilizadorId();
        if (eu == null) return Unauthorized();
        var (m, p) = await PublicacaoDoMembro(id, pid, eu.Value);
        if (m == null || p == null) return NotFound();
        var autor = await _db.Utilizadores.AsNoTracking().FirstOrDefaultAsync(u => u.Id == p.AutorId);
        var g = await _db.Grupos.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        var motivo = Limpar(req?.Motivo ?? "Sem motivo indicado", 400);
        var tituloDenuncia = Limpar($"[Grupo {g?.Nome}] {p.Titulo}", 200);
        _db.DenunciasPartilha.Add(new DenunciaPartilha
        {
            DenuncianteId = eu, RemetenteId = p.AutorId,
            RemetenteNome = autor?.Nome ?? "", RemetenteCodigo = autor?.Codigo ?? "",
            Titulo = tituloDenuncia,
            Letra = p.Texto, Motivo = motivo,
        });
        await _db.SaveChangesAsync();
        return Ok(new { mensagem = "Denúncia recebida. Obrigado." });
    }

    // Quem já viu um aviso (administradores e autor).
    [HttpGet("{id:int}/publicacoes/{pid:int}/vistos")]
    public async Task<IActionResult> Vistos(int id, int pid)
    {
        var eu = UtilizadorId();
        if (eu == null) return Unauthorized();
        var (m, p) = await PublicacaoDoMembro(id, pid, eu.Value);
        if (m == null || p == null || p.Tipo != TipoPublicacao.Aviso) return NotFound();
        if (!m.Administrador && p.AutorId != eu) return Forbid();

        var viram = await (
            from v in _db.VistosPublicacao.AsNoTracking()
            where v.PublicacaoId == pid
            join u in _db.Utilizadores.AsNoTracking() on v.UtilizadorId equals u.Id
            orderby u.Nome
            select new { nome = u.Nome, v.VistoEm }).ToListAsync();
        var membros = await _db.MembrosGrupo.CountAsync(x => x.GrupoId == id && x.Estado == EstadoMembro.Activo && x.UtilizadorId != p.AutorId);
        return Ok(new { viram, totalMembros = membros });
    }

    // Gravação anexada a uma publicação do grupo: só os membros a podem ouvir.
    [HttpGet("audio/{audioId:int}")]
    public async Task<IActionResult> Audio(int audioId)
    {
        var eu = UtilizadorId();
        if (eu == null) return Unauthorized();
        var permitido = await (
            from pa in _db.PublicacoesAudio.AsNoTracking()
            where pa.AudioId == audioId
            join p in _db.PublicacoesGrupo.AsNoTracking() on pa.PublicacaoId equals p.Id
            join m in _db.MembrosGrupo.AsNoTracking() on p.GrupoId equals m.GrupoId
            where m.UtilizadorId == eu && m.Estado == EstadoMembro.Activo
            select pa.Id).AnyAsync();
        if (!permitido) return NotFound();
        var a = await _db.AudiosPartilha.AsNoTracking().FirstOrDefaultAsync(x => x.Id == audioId);
        if (a == null) return NotFound();
        return File(a.Dados, a.ContentType);
    }
}
