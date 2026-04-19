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

    // GET /api/calendario?inicio=2025-12-23&fim=2025-12-29
    // GET /api/calendario?data=2025-12-23  (retrocompatível)
    [HttpGet]
    public async Task<IActionResult> GetByDate(
        [FromQuery] string? data,
        [FromQuery] string? inicio,
        [FromQuery] string? fim)
    {
        // Intervalo de datas
        if (!string.IsNullOrWhiteSpace(inicio) && !string.IsNullOrWhiteSpace(fim))
        {
            if (!DateTime.TryParse(inicio, out var dtInicio) || !DateTime.TryParse(fim, out var dtFim))
                return BadRequest("Datas inválidas. Formato ISO esperado (YYYY-MM-DD).");

            var range = await _db.Eventos
                .Where(e => e.Data >= dtInicio.Date && e.Data < dtFim.Date.AddDays(1))
                .OrderBy(e => e.Data)
                .ToListAsync();

            return Ok(range);
        }

        // Dia específico
        if (!string.IsNullOrWhiteSpace(data))
        {
            if (!DateTime.TryParse(data, out var dt))
                return BadRequest("Data inválida. Formato ISO esperado (YYYY-MM-DD).");

            var ev = await _db.Eventos
                .Where(e => e.Data >= dt.Date && e.Data < dt.Date.AddDays(1))
                .OrderBy(e => e.Data)
                .ToListAsync();

            return Ok(ev);
        }

        // Sem filtro — todos os eventos
        var all = await _db.Eventos.OrderBy(e => e.Data).ToListAsync();
        return Ok(all);
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
