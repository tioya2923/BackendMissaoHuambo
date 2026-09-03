using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MissaoBackend.Data;
using MissaoBackend.Models;
using MissaoBackend.Utils;

namespace MissaoBackend.Controllers;

/// <summary>
/// Tópicos de cânticos, para qualquer idioma. Por omissão devolve/gere conteúdo em
/// Português (?idioma=pt), para manter compatibilidade com clientes antigos.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class TopicosController : ControllerBase
{
    private readonly AppDbContext _db;

    public TopicosController(AppDbContext db)
    {
        _db = db;
    }

    private async Task<int?> ResolverIdiomaId(string idioma)
    {
        var codigo = string.IsNullOrWhiteSpace(idioma) ? "pt" : idioma.Trim().ToLowerInvariant();
        var id = await _db.Idiomas.Where(i => i.Codigo == codigo).Select(i => (int?)i.Id).FirstOrDefaultAsync();
        return id;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Topico>>> GetAll([FromQuery] string idioma = "pt")
    {
        var idiomaId = await ResolverIdiomaId(idioma);
        if (idiomaId == null) return BadRequest($"Idioma '{idioma}' não existe.");

        var topicos = await _db.Topicos
            .Where(t => t.IdiomaId == idiomaId)
            .OrderBy(t => t.Nome)
            .ToListAsync();

        return Ok(topicos);
    }

    [HttpPost]
    [Authorize(Policy = "Gestor")]
    public async Task<ActionResult<Topico>> Create(Topico input, [FromQuery] string idioma = "pt")
    {
        if (input.IdiomaId == 0)
        {
            var idiomaId = await ResolverIdiomaId(idioma);
            if (idiomaId == null) return BadRequest($"Idioma '{idioma}' não existe.");
            input.IdiomaId = idiomaId.Value;
        }

        input.Slug = SlugHelper.Slugify(input.Nome);
        _db.Topicos.Add(input);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetAll), new { id = input.Id }, input);
    }

    [HttpPut("{id:int}")]
    [Authorize(Policy = "Gestor")]
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
    [Authorize(Policy = "Gestor")]
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
