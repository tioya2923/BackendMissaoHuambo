using System.Security.Cryptography;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MissaoBackend.Data;
using MissaoBackend.Models;

namespace MissaoBackend.Controllers;

// Partilha de cânticos (letra e/ou gravação de voz) entre utilizadores, só dentro da app.
// Cada pessoa tem um código curto; quem quer enviar escreve o código de quem vai receber.
[ApiController]
[Route("api/partilhas")]
[Authorize]
public class PartilhasController : ControllerBase
{
    private readonly AppDbContext _db;
    public PartilhasController(AppDbContext db) => _db = db;

    private const int MaxTitulo = 120;
    private const int MaxLetra = 20_000;
    private const int MaxDestinatarios = 20;
    private const int MaxEnviosPorDia = 100;
    private const long MaxAudioBytes = 8L * 1024 * 1024;
    private const int ValidadeDias = 180;
    private const string AlfabetoCodigo = "23456789ABCDEFGHJKMNPQRSTUVWXYZ";

    private int? UtilizadorId()
    {
        var claim = User.FindFirst("utilizadorId")?.Value;
        return int.TryParse(claim, out var id) ? id : null;
    }

    private static string GerarCodigo()
    {
        Span<char> c = stackalloc char[5];
        for (var i = 0; i < c.Length; i++) c[i] = AlfabetoCodigo[RandomNumberGenerator.GetInt32(AlfabetoCodigo.Length)];
        return "NDT-" + new string(c);
    }

    private static string NormalizarCodigo(string? s)
    {
        var t = (s ?? string.Empty).Trim().ToUpperInvariant().Replace(" ", "");
        if (t.Length == 0) return t;
        return t.StartsWith("NDT-") ? t : t.StartsWith("NDT") ? "NDT-" + t[3..] : "NDT-" + t;
    }

    private async Task<string> GarantirCodigo(Utilizador u)
    {
        if (!string.IsNullOrEmpty(u.Codigo)) return u.Codigo;
        for (var tentativa = 0; tentativa < 20; tentativa++)
        {
            var c = GerarCodigo();
            if (await _db.Utilizadores.AnyAsync(x => x.Codigo == c)) continue;
            u.Codigo = c;
            await _db.SaveChangesAsync();
            return c;
        }
        throw new InvalidOperationException("Não foi possível gerar um código.");
    }

    // ── Código pessoal ────────────────────────────────────────────────────────

    [HttpGet("meu-codigo")]
    public async Task<IActionResult> MeuCodigo()
    {
        var id = UtilizadorId();
        if (id == null) return Unauthorized();
        var u = await _db.Utilizadores.FindAsync(id);
        if (u == null) return Unauthorized();
        return Ok(new { codigo = await GarantirCodigo(u), nome = u.Nome });
    }

    // ── Gravação de voz (carregada antes de enviar) ───────────────────────────

    [HttpPost("audio")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(MaxAudioBytes + 1024 * 1024)]
    public async Task<IActionResult> CarregarAudio([FromForm] IFormFile audio)
    {
        var id = UtilizadorId();
        if (id == null) return Unauthorized();
        if (audio == null || audio.Length == 0) return BadRequest("Falta o ficheiro de áudio.");
        if (audio.Length > MaxAudioBytes) return BadRequest("A gravação é demasiado grande (máximo 8 MB).");

        var ext = Path.GetExtension(audio.FileName ?? string.Empty).ToLowerInvariant();
        var tipos = new Dictionary<string, string>
        {
            [".m4a"] = "audio/mp4", [".mp4"] = "audio/mp4", [".aac"] = "audio/aac", [".mp3"] = "audio/mpeg",
            [".wav"] = "audio/wav", [".caf"] = "audio/x-caf", [".3gp"] = "audio/3gpp", [".ogg"] = "audio/ogg",
            [".webm"] = "audio/webm",
        };
        if (!tipos.TryGetValue(ext, out var contentType)) return BadRequest("Formato de áudio não suportado.");

        // Limpa gravações carregadas e nunca enviadas há mais de um dia.
        var limite = DateTime.UtcNow.AddDays(-1);
        var orfaos = await _db.AudiosPartilha
            .Where(a => a.DonoId == id && a.CriadoEm < limite && !_db.PartilhasCanticos.Any(p => p.AudioId == a.Id))
            .ToListAsync();
        if (orfaos.Count > 0) _db.AudiosPartilha.RemoveRange(orfaos);

        using var ms = new MemoryStream();
        await audio.CopyToAsync(ms);
        var novo = new AudioPartilha { DonoId = id.Value, ContentType = contentType, Dados = ms.ToArray() };
        _db.AudiosPartilha.Add(novo);
        await _db.SaveChangesAsync();
        return Ok(new { audioId = novo.Id });
    }

    // ── Enviar ────────────────────────────────────────────────────────────────

    public record EnviarRequest(string Titulo, string? Letra, string? Momento, int? AudioId, List<string> Codigos);

    [HttpPost]
    public async Task<IActionResult> Enviar([FromBody] EnviarRequest req)
    {
        var id = UtilizadorId();
        if (id == null) return Unauthorized();

        var titulo = (req.Titulo ?? string.Empty).Trim();
        var letra = (req.Letra ?? string.Empty).Trim();
        if (titulo.Length == 0 || titulo.Length > MaxTitulo) return BadRequest($"O título é obrigatório (máximo {MaxTitulo} caracteres).");
        if (letra.Length > MaxLetra) return BadRequest($"A letra é demasiado longa (máximo {MaxLetra} caracteres).");
        if (letra.Length == 0 && req.AudioId == null) return BadRequest("Envie a letra, a gravação ou ambas.");

        var codigos = (req.Codigos ?? new()).Select(NormalizarCodigo).Where(c => c.Length > 4).Distinct().ToList();
        if (codigos.Count == 0) return BadRequest("Indique pelo menos um código de destinatário.");
        if (codigos.Count > MaxDestinatarios) return BadRequest($"Pode enviar a no máximo {MaxDestinatarios} pessoas de cada vez.");

        var remetente = await _db.Utilizadores.FindAsync(id);
        if (remetente == null) return Unauthorized();

        var desde = DateTime.UtcNow.AddDays(-1);
        var enviadosHoje = await _db.PartilhasCanticos.CountAsync(p => p.RemetenteId == id && p.CriadoEm >= desde);
        if (enviadosHoje + codigos.Count > MaxEnviosPorDia)
            return StatusCode(429, "Atingiu o limite de envios de hoje. Tente amanhã.");

        if (req.AudioId != null)
        {
            var audioMeu = await _db.AudiosPartilha.AnyAsync(a => a.Id == req.AudioId && a.DonoId == id);
            if (!audioMeu) return BadRequest("Gravação inválida.");
        }

        var destinatarios = await _db.Utilizadores.Where(u => u.Codigo != null && codigos.Contains(u.Codigo)).ToListAsync();
        var bloqueadoresIds = await _db.BloqueiosUtilizador.Where(b => b.BloqueadoId == id).Select(b => b.UtilizadorId).ToListAsync();
        var euBloqueei = await _db.BloqueiosUtilizador.Where(b => b.UtilizadorId == id).Select(b => b.BloqueadoId).ToListAsync();

        var momento = string.IsNullOrWhiteSpace(req.Momento) ? null : req.Momento!.Trim();
        if (momento != null && momento.Length > 60) momento = momento[..60];

        var enviados = 0;
        var naoEnviados = new List<string>();
        foreach (var codigo in codigos)
        {
            var d = destinatarios.FirstOrDefault(x => x.Codigo == codigo);
            // Não distingue "não existe" de "bloqueou-o": quem foi bloqueado não deve saber.
            if (d == null || d.Id == id || bloqueadoresIds.Contains(d.Id) || euBloqueei.Contains(d.Id))
            {
                naoEnviados.Add(codigo);
                continue;
            }
            _db.PartilhasCanticos.Add(new PartilhaCantico
            {
                RemetenteId = id.Value, DestinatarioId = d.Id,
                Titulo = titulo, Letra = letra, Momento = momento, AudioId = req.AudioId,
            });
            enviados++;
        }
        await _db.SaveChangesAsync();
        return Ok(new { enviados, naoEnviados });
    }

    // ── Receber ───────────────────────────────────────────────────────────────

    [HttpGet("recebidas")]
    public async Task<IActionResult> Recebidas()
    {
        var id = UtilizadorId();
        if (id == null) return Unauthorized();

        // Partilhas com mais de 180 dias são apagadas (e as gravações que ficarem sem uso).
        var limite = DateTime.UtcNow.AddDays(-ValidadeDias);
        var velhas = await _db.PartilhasCanticos.Where(p => p.DestinatarioId == id && p.CriadoEm < limite).ToListAsync();
        if (velhas.Count > 0)
        {
            _db.PartilhasCanticos.RemoveRange(velhas);
            await _db.SaveChangesAsync();
            await LimparAudiosSemUso(velhas.Where(v => v.AudioId != null).Select(v => v.AudioId!.Value).Distinct().ToList());
        }

        var lista = await (
            from p in _db.PartilhasCanticos.AsNoTracking()
            where p.DestinatarioId == id
            join u in _db.Utilizadores.AsNoTracking() on p.RemetenteId equals u.Id
            orderby p.CriadoEm descending
            select new
            {
                p.Id, p.Titulo, p.Momento,
                remetenteNome = u.Nome, remetenteCodigo = u.Codigo,
                temAudio = p.AudioId != null, lido = p.LidoEm != null, p.CriadoEm,
            }).Take(200).ToListAsync();
        return Ok(lista);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Detalhe(int id)
    {
        var eu = UtilizadorId();
        if (eu == null) return Unauthorized();
        var p = await _db.PartilhasCanticos.FirstOrDefaultAsync(x => x.Id == id && x.DestinatarioId == eu);
        if (p == null) return NotFound();
        var r = await _db.Utilizadores.AsNoTracking().FirstOrDefaultAsync(u => u.Id == p.RemetenteId);

        if (p.LidoEm == null)
        {
            p.LidoEm = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }
        return Ok(new
        {
            p.Id, p.Titulo, p.Letra, p.Momento, p.CriadoEm,
            remetenteNome = r?.Nome, remetenteCodigo = r?.Codigo,
            temAudio = p.AudioId != null,
        });
    }

    [HttpGet("{id:int}/audio")]
    public async Task<IActionResult> Audio(int id)
    {
        var eu = UtilizadorId();
        if (eu == null) return Unauthorized();
        var p = await _db.PartilhasCanticos.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id && x.DestinatarioId == eu);
        if (p?.AudioId == null) return NotFound();
        var a = await _db.AudiosPartilha.AsNoTracking().FirstOrDefaultAsync(x => x.Id == p.AudioId);
        if (a == null) return NotFound();
        return File(a.Dados, a.ContentType);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Apagar(int id)
    {
        var eu = UtilizadorId();
        if (eu == null) return Unauthorized();
        var p = await _db.PartilhasCanticos.FirstOrDefaultAsync(x => x.Id == id && x.DestinatarioId == eu);
        if (p == null) return NotFound();
        var audioId = p.AudioId;
        _db.PartilhasCanticos.Remove(p);
        await _db.SaveChangesAsync();
        if (audioId != null) await LimparAudiosSemUso(new List<int> { audioId.Value });
        return NoContent();
    }

    private async Task LimparAudiosSemUso(List<int> audioIds)
    {
        if (audioIds.Count == 0) return;
        var semUso = await _db.AudiosPartilha
            .Where(a => audioIds.Contains(a.Id) && !_db.PartilhasCanticos.Any(p => p.AudioId == a.Id))
            .ToListAsync();
        if (semUso.Count == 0) return;
        _db.AudiosPartilha.RemoveRange(semUso);
        await _db.SaveChangesAsync();
    }

    // ── Segurança: denunciar e bloquear ──────────────────────────────────────

    public record DenunciarRequest(string? Motivo);

    [HttpPost("{id:int}/denunciar")]
    public async Task<IActionResult> Denunciar(int id, [FromBody] DenunciarRequest req)
    {
        var eu = UtilizadorId();
        if (eu == null) return Unauthorized();
        var p = await _db.PartilhasCanticos.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id && x.DestinatarioId == eu);
        if (p == null) return NotFound();
        var r = await _db.Utilizadores.AsNoTracking().FirstOrDefaultAsync(u => u.Id == p.RemetenteId);

        var motivo = (req?.Motivo ?? "Sem motivo indicado").Trim();
        if (motivo.Length > 500) motivo = motivo[..500];
        _db.DenunciasPartilha.Add(new DenunciaPartilha
        {
            DenuncianteId = eu, RemetenteId = p.RemetenteId,
            RemetenteNome = r?.Nome ?? "", RemetenteCodigo = r?.Codigo ?? "",
            Titulo = p.Titulo, Letra = p.Letra, Motivo = motivo,
        });
        await _db.SaveChangesAsync();
        return Ok(new { mensagem = "Denúncia recebida. Obrigado." });
    }

    [HttpPost("{id:int}/bloquear")]
    public async Task<IActionResult> BloquearRemetente(int id)
    {
        var eu = UtilizadorId();
        if (eu == null) return Unauthorized();
        var p = await _db.PartilhasCanticos.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id && x.DestinatarioId == eu);
        if (p == null) return NotFound();

        if (!await _db.BloqueiosUtilizador.AnyAsync(b => b.UtilizadorId == eu && b.BloqueadoId == p.RemetenteId))
            _db.BloqueiosUtilizador.Add(new BloqueioUtilizador { UtilizadorId = eu.Value, BloqueadoId = p.RemetenteId });

        var deles = await _db.PartilhasCanticos.Where(x => x.DestinatarioId == eu && x.RemetenteId == p.RemetenteId).ToListAsync();
        var audios = deles.Where(x => x.AudioId != null).Select(x => x.AudioId!.Value).Distinct().ToList();
        _db.PartilhasCanticos.RemoveRange(deles);
        await _db.SaveChangesAsync();
        await LimparAudiosSemUso(audios);
        return NoContent();
    }

    [HttpGet("bloqueados")]
    public async Task<IActionResult> Bloqueados()
    {
        var eu = UtilizadorId();
        if (eu == null) return Unauthorized();
        var lista = await (
            from b in _db.BloqueiosUtilizador.AsNoTracking()
            where b.UtilizadorId == eu
            join u in _db.Utilizadores.AsNoTracking() on b.BloqueadoId equals u.Id
            select new { utilizadorId = u.Id, u.Nome, u.Codigo }).ToListAsync();
        return Ok(lista);
    }

    [HttpDelete("bloqueados/{utilizadorId:int}")]
    public async Task<IActionResult> Desbloquear(int utilizadorId)
    {
        var eu = UtilizadorId();
        if (eu == null) return Unauthorized();
        var b = await _db.BloqueiosUtilizador.FirstOrDefaultAsync(x => x.UtilizadorId == eu && x.BloqueadoId == utilizadorId);
        if (b == null) return NotFound();
        _db.BloqueiosUtilizador.Remove(b);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // ── Moderação (administrador) ────────────────────────────────────────────

    [HttpGet("denuncias")]
    [Authorize(Policy = "Gestor")]
    public async Task<IActionResult> ListarDenuncias()
        => Ok(await _db.DenunciasPartilha.AsNoTracking().OrderByDescending(d => d.CriadoEm).Take(200).ToListAsync());

    [HttpDelete("denuncias/{id:int}")]
    [Authorize(Policy = "Gestor")]
    public async Task<IActionResult> ResolverDenuncia(int id)
    {
        var d = await _db.DenunciasPartilha.FindAsync(id);
        if (d == null) return NotFound();
        _db.DenunciasPartilha.Remove(d);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
