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
    [Authorize]
    public async Task<ActionResult<TopicoUmb>> Create(TopicoUmb input)
    {
        input.Slug = SlugHelper.Slugify(input.Nome);
        _db.TopicosUmb.Add(input);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetAll), new { id = input.Id }, input);
    }
}
