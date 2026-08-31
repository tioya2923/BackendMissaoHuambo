using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MissaoBackend.Data;
using MissaoBackend.Models;
using MissaoBackend.Utils;

namespace MissaoBackend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CanticosLatController : ControllerBase
{
    private readonly AppDbContext _db;

    public CanticosLatController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult> GetAll()
    {
        var canticos = await _db.CanticosLat
            .Include(c => c.Topico)
            .AsNoTracking()
            .OrderBy(c => c.Titulo)
            .Select(c => new {
                c.Id,
                c.Titulo,
                c.Slug,
                c.TopicoId,
                Topico = c.Topico == null ? null : new { c.Topico.Id, c.Topico.Nome, c.Topico.Slug }
            })
            .ToListAsync();

        return Ok(canticos);
    }

    [HttpGet("topico/{slug}")]
    public async Task<ActionResult> GetByTopico(string slug)
    {
        var topico = await _db.TopicosLat.AsNoTracking()
            .FirstOrDefaultAsync(t => t.Slug == slug);
        if (topico == null) return NotFound();

        var canticos = await _db.CanticosLat
            .Where(c => c.TopicoId == topico.Id)
            .OrderBy(c => c.Titulo)
            .Select(c => new { c.Id, c.Titulo, c.Slug })
            .ToListAsync();

        return Ok(canticos);
    }

    [HttpGet("{slug}")]
    public async Task<ActionResult<CanticoLat>> GetBySlug(string slug)
    {
        var cantico = await _db.CanticosLat.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Slug == slug);
        if (cantico == null) return NotFound();
        return Ok(cantico);
    }

    [HttpPost]
    [Authorize(Policy = "Gestor")]
    public async Task<ActionResult<CanticoLat>> Create(CanticoLat input)
    {
        input.Slug = SlugHelper.Slugify(input.Titulo);
        if (await _db.CanticosLat.AnyAsync(c => c.Slug == input.Slug))
            return Conflict("Já existe um cântico com este título.");

        _db.CanticosLat.Add(input);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetBySlug), new { slug = input.Slug }, input);
    }

    [HttpPut("{id:int}")]
    [Authorize(Policy = "Gestor")]
    public async Task<IActionResult> Update(int id, CanticoLat input)
    {
        var existing = await _db.CanticosLat.FindAsync(id);
        if (existing == null) return NotFound();

        var novoSlug = SlugHelper.Slugify(input.Titulo);
        if (novoSlug != existing.Slug && await _db.CanticosLat.AnyAsync(c => c.Slug == novoSlug && c.Id != id))
            return Conflict("Já existe um cântico com este título.");

        existing.Titulo = input.Titulo;
        existing.Letra = input.Letra;
        existing.Autor = input.Autor;
        existing.TopicoId = input.TopicoId;
        existing.Slug = novoSlug;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    [Authorize(Policy = "Gestor")]
    public async Task<IActionResult> Delete(int id)
    {
        var existing = await _db.CanticosLat.FindAsync(id);
        if (existing == null) return NotFound();
        _db.CanticosLat.Remove(existing);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
