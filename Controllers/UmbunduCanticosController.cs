using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MissaoBackend.Data;
using MissaoBackend.Models;
using MissaoBackend.Utils;

namespace MissaoBackend.Controllers;

[ApiController]
[Route("api/umbundu/canticos")]
public class UmbunduCanticosController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IWebHostEnvironment _env;

    public UmbunduCanticosController(AppDbContext db, IWebHostEnvironment env)
    {
        _db = db;
        _env = env;
    }

    // ============================
    // LISTAR TÓPICOS
    // ============================
    [HttpGet("topicos")]
    public async Task<ActionResult> ListarTopicos()
    {
        var topicos = await _db.TopicosUmb
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
        var canticos = await _db.CanticosUmb
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
        var topico = await _db.TopicosUmb
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Slug == slug);

        if (topico == null)
            return NotFound();

        var canticos = await _db.CanticosUmb
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
    public async Task<ActionResult<CanticoUmb>> GetBySlug(string slug)
    {
        var cantico = await _db.CanticosUmb
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

        var results = await _db.CanticosUmb
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
    [Authorize]
    public async Task<ActionResult<CanticoUmb>> Create(CanticoUmb input)
    {
        input.Slug = SlugHelper.Slugify(input.Titulo);
        _db.CanticosUmb.Add(input);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetBySlug), new { slug = input.Slug }, input);
    }

    // ============================
    // ATUALIZAR
    // ============================
    [HttpPut("{id:int}")]
    [Authorize]
    public async Task<IActionResult> Update(int id, CanticoUmb input)
    {
        var existing = await _db.CanticosUmb.FindAsync(id);
        if (existing == null)
            return NotFound();

        existing.Titulo = input.Titulo;
        existing.Letra = input.Letra;
        existing.TopicoId = input.TopicoId;
        existing.Slug = SlugHelper.Slugify(input.Titulo);

        await _db.SaveChangesAsync();
        return NoContent();
    }

    // ============================
    // APAGAR
    // ============================
    [HttpDelete("{id:int}")]
    [Authorize]
    public async Task<IActionResult> Delete(int id)
    {
        var existing = await _db.CanticosUmb.FindAsync(id);
        if (existing == null)
            return NotFound();

        _db.CanticosUmb.Remove(existing);
        await _db.SaveChangesAsync();

        return NoContent();
    }

    // ============================
    // UPLOAD PDF
    // ============================
    [HttpPost("{id:int}/upload-pdf")]
    [Authorize]
    public async Task<IActionResult> UploadPdf(int id, IFormFile file)
    {
        var cantico = await _db.CanticosUmb.FindAsync(id);
        if (cantico == null)
            return NotFound();

        if (file == null || file.Length == 0)
            return BadRequest("Nenhum ficheiro enviado.");

        var root = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
        var uploadsDir = Path.Combine(root, "partituras");
        Directory.CreateDirectory(uploadsDir);

        var fileName = $"{cantico.Slug}-{Guid.NewGuid():N}.pdf";
        var filePath = Path.Combine(uploadsDir, fileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        cantico.PdfUrl = $"/partituras/{fileName}";
        await _db.SaveChangesAsync();

        return Ok(new { cantico.Id, cantico.PdfUrl, Codigo = cantico.Slug });
    }

    // ============================
    // LISTAR POR ID DO TÓPICO
    // ============================
    [HttpGet("topico/id/{id:int}")]
    public async Task<ActionResult> GetByTopicoId(int id)
    {
        var topico = await _db.TopicosUmb
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == id);

        if (topico == null)
            return NotFound();

        var canticos = await _db.CanticosUmb
            .Where(c => c.TopicoId == topico.Id)
            .OrderBy(c => c.Titulo)
            .Select(c => new { c.Id, c.Titulo, c.Slug })
            .ToListAsync();

        return Ok(canticos);
    }
}
