using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MissaoBackend.Data;
using MissaoBackend.Models;

namespace MissaoBackend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CalendarioController : ControllerBase
{
    private readonly AppDbContext _db;

    public CalendarioController(AppDbContext db)
    {
        _db = db;
    }

    // GET /api/calendario?data=2025-12-23
    [HttpGet]
    public async Task<IActionResult> GetByDate([FromQuery] string? data)
    {
        if (string.IsNullOrWhiteSpace(data))
        {
            var all = await _db.Eventos.OrderBy(e => e.Data).ToListAsync();
            return Ok(all);
        }

        if (!DateTime.TryParse(data, out var dt))
            return BadRequest("Data inválida. Formato ISO esperado (YYYY-MM-DD).");

        var dayStart = dt.Date;
        var dayEnd = dayStart.AddDays(1);

        var ev = await _db.Eventos
            .Where(e => e.Data >= dayStart && e.Data < dayEnd)
            .OrderBy(e => e.Data)
            .ToListAsync();

        return Ok(ev);
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Create(Evento input)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        _db.Eventos.Add(input);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetByDate), new { data = input.Data.ToString("yyyy-MM-dd") }, input);
    }
}
