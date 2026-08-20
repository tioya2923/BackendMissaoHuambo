using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MissaoBackend.Data;
using MissaoBackend.Models;
using MissaoBackend.Utils;

namespace MissaoBackend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CatecismoLatTopicosController : ControllerBase
{
    private readonly AppDbContext _context;

    public CatecismoLatTopicosController(AppDbContext context) => _context = context;

    [HttpGet]
    public async Task<ActionResult<IEnumerable<CatecismoLatTopico>>> GetAll()
        => await _context.CatecismoLatTopicos.ToListAsync();

    [HttpGet("{id}")]
    public async Task<ActionResult<CatecismoLatTopico>> GetById(int id)
    {
        var item = await _context.CatecismoLatTopicos.FirstOrDefaultAsync(t => t.Id == id);
        if (item == null) return NotFound();
        return item;
    }

    [HttpPost]
    [Authorize(Policy = "Gestor")]
    public async Task<ActionResult<CatecismoLatTopico>> Create(CatecismoLatTopico input)
    {
        input.Slug = SlugHelper.Slugify(input.Titulo);
        _context.CatecismoLatTopicos.Add(input);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetAll), new { id = input.Id }, input);
    }

    [HttpPut("{id:int}")]
    [Authorize(Policy = "Gestor")]
    public async Task<IActionResult> Update(int id, CatecismoLatTopico input)
    {
        var existing = await _context.CatecismoLatTopicos.FindAsync(id);
        if (existing == null) return NotFound();

        existing.Titulo = input.Titulo;
        existing.Slug = SlugHelper.Slugify(input.Titulo);

        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    [Authorize(Policy = "Gestor")]
    public async Task<IActionResult> Delete(int id)
    {
        var existing = await _context.CatecismoLatTopicos.FindAsync(id);
        if (existing == null) return NotFound();

        var temTextos = await _context.CatecismosLat.AnyAsync(c => c.CatecismoLatTopicoId == id);
        if (temTextos)
            return BadRequest("Não é possível eliminar: existem textos associados a este tópico.");

        _context.CatecismoLatTopicos.Remove(existing);
        await _context.SaveChangesAsync();
        return NoContent();
    }
}
