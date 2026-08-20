using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MissaoBackend.Data;
using MissaoBackend.Models;
using MissaoBackend.Utils;

namespace MissaoBackend.Controllers;

[ApiController]
[Route("api/umbundu/topicos")]
public class UmbunduTopicosController : ControllerBase
{
    private readonly AppDbContext _db;

    public UmbunduTopicosController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<TopicoUmb>>> GetAll()
    {
        var topicos = await _db.TopicosUmb
            .OrderBy(t => t.Nome)
            .ToListAsync();

        return Ok(topicos);
    }

    [HttpPost]
    [Authorize(Policy = "Gestor")]
    public async Task<ActionResult<TopicoUmb>> Create(TopicoUmb input)
    {
        input.Slug = SlugHelper.Slugify(input.Nome);
        _db.TopicosUmb.Add(input);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetAll), new { id = input.Id }, input);
    }

    [HttpPut("{id:int}")]
    [Authorize(Policy = "Gestor")]
    public async Task<IActionResult> Update(int id, TopicoUmb input)
    {
        var existing = await _db.TopicosUmb.FindAsync(id);
        if (existing == null) return NotFound();

        existing.Nome = input.Nome;
        existing.Slug = SlugHelper.Slugify(input.Nome);

        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    [Authorize(Policy = "Gestor")]
    public async Task<IActionResult> Delete(int id)
    {
        var existing = await _db.TopicosUmb.FindAsync(id);
        if (existing == null) return NotFound();

        var temCanticos = await _db.CanticosUmb.AnyAsync(c => c.TopicoId == id);
        if (temCanticos)
            return BadRequest("Não é possível eliminar: existem cânticos associados a este tópico.");

        _db.TopicosUmb.Remove(existing);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
