using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MissaoBackend.Data;
using MissaoBackend.Models;
using MissaoBackend.Utils;

namespace MissaoBackend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TopicosController : ControllerBase
{
    private readonly AppDbContext _db;

    public TopicosController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Topico>>> GetAll()
    {
        var topicos = await _db.Topicos
            .OrderBy(t => t.Nome)
            .ToListAsync();

        return Ok(topicos);
    }

    [HttpPost]
    [Authorize]
    public async Task<ActionResult<Topico>> Create(Topico input)
    {
        input.Slug = SlugHelper.Slugify(input.Nome);
        _db.Topicos.Add(input);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetAll), new { id = input.Id }, input);
    }

    [HttpPut("{id:int}")]
    [Authorize]
    public async Task<IActionResult> Update(int id, Topico input)
    {
        var existing = await _db.Topicos.FindAsync(id);
        if (existing == null) return NotFound();

        existing.Nome = input.Nome;
        existing.Slug = SlugHelper.Slugify(input.Nome);

        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    [Authorize]
    public async Task<IActionResult> Delete(int id)
    {
        var existing = await _db.Topicos.FindAsync(id);
        if (existing == null) return NotFound();

        var temCanticos = await _db.Canticos.AnyAsync(c => c.TopicoId == id);
        if (temCanticos)
            return BadRequest("Não é possível eliminar: existem cânticos associados a este tópico.");

        _db.Topicos.Remove(existing);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
