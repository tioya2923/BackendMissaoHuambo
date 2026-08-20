using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MissaoBackend.Data;
using MissaoBackend.Models;
using MissaoBackend.Utils;

namespace MissaoBackend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TopicosLatController : ControllerBase
{
    private readonly AppDbContext _db;

    public TopicosLatController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<IEnumerable<TopicoLat>>> GetAll()
        => await _db.TopicosLat.OrderBy(t => t.Nome).ToListAsync();

    [HttpPost]
    [Authorize]
    public async Task<ActionResult<TopicoLat>> Create(TopicoLat input)
    {
        input.Slug = SlugHelper.Slugify(input.Nome);
        _db.TopicosLat.Add(input);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetAll), new { id = input.Id }, input);
    }

    [HttpPut("{id:int}")]
    [Authorize]
    public async Task<IActionResult> Update(int id, TopicoLat input)
    {
        var existing = await _db.TopicosLat.FindAsync(id);
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
        var existing = await _db.TopicosLat.FindAsync(id);
        if (existing == null) return NotFound();

        var temCanticos = await _db.CanticosLat.AnyAsync(c => c.TopicoId == id);
        if (temCanticos)
            return BadRequest("Não é possível eliminar: existem cânticos associados a este tópico.");

        _db.TopicosLat.Remove(existing);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
