using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MissaoBackend.Data;
using MissaoBackend.Models;
using MissaoBackend.Utils;

namespace MissaoBackend.Controllers;

[ApiController]
[Route("api/kimbundu/canticos")]
public class KimbunduCanticosController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IWebHostEnvironment _env;
    private readonly MissaoBackend.Services.ArmazenamentoService _armazenamento;

    public KimbunduCanticosController(AppDbContext db, IWebHostEnvironment env, MissaoBackend.Services.ArmazenamentoService armazenamento)
    {
        _db = db;
        _env = env;
        _armazenamento = armazenamento;
    }

    // ============================
    // LISTAR TÓPICOS
    // ============================
    [HttpGet("topicos")]
    public async Task<ActionResult> ListarTopicos()
    {
        var topicos = await _db.TopicosKmb
            .AsNoTracking()
            .OrderBy(t => t.Nome)
            .Select(t => new { t.Id, t.Nome, t.Slug })
            .ToListAsync();

        return Ok(topicos);
    }

    // ============================
    // LISTAR CÂNTICOS + TÓPICO
    // ============================
    [HttpGet("canticos-com-topico")]
    public async Task<ActionResult> ListarCanticosComTopico()
    {
        var canticos = await _db.CanticosKmb
            .Include(c => c.Topico)
            .AsNoTracking()
            .OrderBy(c => c.Titulo)
            .Select(c => new {
                c.Id,
                c.Titulo,
                c.Slug,
                Topico = c.Topico == null ? null : new { c.Topico.Id, c.Topico.Nome, c.Topico.Slug }
            })
            .ToListAsync();

        return Ok(canticos);
    }

    // ============================
    // LISTAR CÂNTICOS POR TÓPICO (SLUG)
    // ============================
    [HttpGet("topico/{slug}")]
    public async Task<ActionResult> GetByTopico(string slug)
    {
        var topico = await _db.TopicosKmb
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Slug == slug);

        if (topico == null)
            return NotFound();

        var canticos = await _db.CanticosKmb
            .Where(c => c.TopicoId == topico.Id)
            .OrderBy(c => c.Titulo)
            .Select(c => new { c.Id, c.Titulo, c.Slug })
            .ToListAsync();

        return Ok(canticos);
    }

    // ============================
    // OBTER CÂNTICO POR SLUG
    // ============================
    [HttpGet("{slug}")]
    public async Task<ActionResult<CanticoKmb>> GetBySlug(string slug)
    {
        var cantico = await _db.CanticosKmb
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Slug == slug);

        if (cantico == null)
            return NotFound();

        return Ok(cantico);
    }

    // ============================
    // PESQUISA
    // ============================
    [HttpGet("search")]
    public async Task<ActionResult> Search([FromQuery] string q)
    {
        if (string.IsNullOrWhiteSpace(q))
            return Ok(Array.Empty<object>());

        q = q.Trim();

        var results = await _db.CanticosKmb
            .Where(c =>
                EF.Functions.Like(c.Titulo, $"%{q}%") ||
                EF.Functions.Like(c.Letra, $"%{q}%"))
            .OrderBy(c => c.Titulo)
            .Select(c => new { c.Id, c.Titulo, c.Slug })
            .ToListAsync();

        return Ok(results);
    }

    // ============================
    // CRIAR
    // ============================
    [HttpPost]
    [Authorize(Policy = "Gestor")]
    public async Task<ActionResult<CanticoKmb>> Create(CanticoKmb input)
    {
        input.Slug = SlugHelper.Slugify(input.Titulo);
        if (await _db.CanticosKmb.AnyAsync(c => c.Slug == input.Slug))
            return Conflict("Já existe um cântico com este título.");

        _db.CanticosKmb.Add(input);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetBySlug), new { slug = input.Slug }, input);
    }

    // ============================
    // ATUALIZAR
    // ============================
    [HttpPut("{id:int}")]
    [Authorize(Policy = "Gestor")]
    public async Task<IActionResult> Update(int id, CanticoKmb input)
    {
        var existing = await _db.CanticosKmb.FindAsync(id);
        if (existing == null)
            return NotFound();

        var novoSlug = SlugHelper.Slugify(input.Titulo);
        if (novoSlug != existing.Slug && await _db.CanticosKmb.AnyAsync(c => c.Slug == novoSlug && c.Id != id))
            return Conflict("Já existe um cântico com este título.");

        existing.Titulo = input.Titulo;
        existing.Letra = input.Letra;
        existing.Autor = input.Autor;
        existing.TopicoId = input.TopicoId;
        existing.Slug = novoSlug;

        await _db.SaveChangesAsync();
        return NoContent();
    }

    // ============================
    // APAGAR
    // ============================
    [HttpDelete("{id:int}")]
    [Authorize(Policy = "Gestor")]
    public async Task<IActionResult> Delete(int id)
    {
        var existing = await _db.CanticosKmb.FindAsync(id);
        if (existing == null)
            return NotFound();

        _db.CanticosKmb.Remove(existing);
        await _db.SaveChangesAsync();

        return NoContent();
    }

    // ============================
    // UPLOAD PDF
    // ============================
    [HttpPost("{id:int}/upload-pdf")]
    [Authorize(Policy = "Gestor")]
    public async Task<IActionResult> UploadPdf(int id, IFormFile file)
    {
        var cantico = await _db.CanticosKmb.FindAsync(id);
        if (cantico == null)
            return NotFound();

        if (file == null || file.Length == 0)
            return BadRequest("Nenhum ficheiro enviado.");

        var urlAnterior = cantico.PdfUrl;
        cantico.PdfUrl = await _armazenamento.GuardarAsync(file);
        await _db.SaveChangesAsync();
        await _armazenamento.ApagarSeForNossoAsync(urlAnterior);

        return Ok(new { cantico.Id, cantico.PdfUrl, Codigo = cantico.Slug });
    }

    // ============================
    // LISTAR POR ID DO TÓPICO
    // ============================
    [HttpGet("topico/id/{id:int}")]
    public async Task<ActionResult> GetByTopicoId(int id)
    {
        var topico = await _db.TopicosKmb
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == id);

        if (topico == null)
            return NotFound();

        var canticos = await _db.CanticosKmb
            .Where(c => c.TopicoId == topico.Id)
            .OrderBy(c => c.Titulo)
            .Select(c => new { c.Id, c.Titulo, c.Slug })
            .ToListAsync();

        return Ok(canticos);
    }
}
