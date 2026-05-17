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
}
