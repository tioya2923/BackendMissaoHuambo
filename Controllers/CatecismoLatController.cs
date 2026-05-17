using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MissaoBackend.Data;
using MissaoBackend.Models;

namespace MissaoBackend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CatecismoLatController : ControllerBase
{
    private readonly AppDbContext _context;

    public CatecismoLatController(AppDbContext context) => _context = context;

    [HttpGet]
    public async Task<ActionResult<IEnumerable<CatecismoLat>>> GetAll([FromQuery] int? topicoId = null)
    {
        var query = _context.CatecismosLat.AsQueryable();
        if (topicoId.HasValue)
            query = query.Where(c => c.CatecismoLatTopicoId == topicoId);
        return await query.OrderBy(c => c.Id).ToListAsync();
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<CatecismoLat>> GetById(int id)
    {
        var item = await _context.CatecismosLat.FindAsync(id);
        if (item == null) return NotFound();
        return item;
    }

    [HttpPost]
    [Authorize]
    public async Task<ActionResult<CatecismoLat>> Create(CatecismoLat input)
    {
        _context.CatecismosLat.Add(input);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = input.Id }, input);
    }

    [HttpDelete("{id}")]
    [Authorize]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await _context.CatecismosLat.FindAsync(id);
        if (item == null) return NotFound();
        _context.CatecismosLat.Remove(item);
        await _context.SaveChangesAsync();
        return NoContent();
    }
}
