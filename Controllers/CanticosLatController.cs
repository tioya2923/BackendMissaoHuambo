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
    [Authorize]
    public async Task<ActionResult<CanticoLat>> Create(CanticoLat input)
    {
        input.Slug = SlugHelper.Slugify(input.Titulo);
        _db.CanticosLat.Add(input);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetBySlug), new { slug = input.Slug }, input);
    }

    [HttpPut("{id:int}")]
    [Authorize]
    public async Task<IActionResult> Update(int id, CanticoLat input)
    {
        var existing = await _db.CanticosLat.FindAsync(id);
        if (existing == null) return NotFound();
        existing.Titulo = input.Titulo;
        existing.Letra = input.Letra;
        existing.TopicoId = input.TopicoId;
        existing.Slug = SlugHelper.Slugify(input.Titulo);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    [Authorize]
    public async Task<IActionResult> Delete(int id)
    {
        var existing = await _db.CanticosLat.FindAsync(id);
        if (existing == null) return NotFound();
        _db.CanticosLat.Remove(existing);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
