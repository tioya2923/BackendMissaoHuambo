using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MissaoBackend.Data;
using MissaoBackend.Models;

namespace MissaoBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FormasApoioController : ControllerBase
    {
        private readonly AppDbContext _db;

        public FormasApoioController(AppDbContext db)
        {
            _db = db;
        }

        // GET /api/formasapoio — público, só as ativas, para a app mostrar aos utilizadores
        [HttpGet]
        public async Task<ActionResult<IEnumerable<FormaApoio>>> GetAtivas()
        {
            var formas = await _db.FormasApoio
                .AsNoTracking()
                .Where(f => f.Ativo)
                .OrderBy(f => f.Ordem)
                .ThenBy(f => f.Id)
                .ToListAsync();

            return Ok(formas);
        }

        // GET /api/formasapoio/admin — protegido, todas (incluindo inativas), para o painel de administração
        [HttpGet("admin")]
        [Authorize(Policy = "Gestor")]
        public async Task<ActionResult<IEnumerable<FormaApoio>>> GetTodas()
        {
            var formas = await _db.FormasApoio
                .AsNoTracking()
                .OrderBy(f => f.Ordem)
                .ThenBy(f => f.Id)
                .ToListAsync();

            return Ok(formas);
        }

        [HttpPost]
        [Authorize(Policy = "Gestor")]
        public async Task<ActionResult<FormaApoio>> Create(FormaApoio input)
        {
            if (string.IsNullOrWhiteSpace(input.Label) || string.IsNullOrWhiteSpace(input.Valor))
                return BadRequest("Label e Valor são obrigatórios.");

            var forma = new FormaApoio
            {
                Label = input.Label.Trim(),
                Valor = input.Valor.Trim(),
                Descricao = input.Descricao?.Trim(),
                Ativo = input.Ativo,
                Ordem = input.Ordem,
            };

            _db.FormasApoio.Add(forma);
            await _db.SaveChangesAsync();

            return CreatedAtAction(nameof(GetTodas), new { id = forma.Id }, forma);
        }

        [HttpPut("{id:int}")]
        [Authorize(Policy = "Gestor")]
        public async Task<IActionResult> Update(int id, FormaApoio input)
        {
            var existing = await _db.FormasApoio.FindAsync(id);
            if (existing == null) return NotFound();

            if (string.IsNullOrWhiteSpace(input.Label) || string.IsNullOrWhiteSpace(input.Valor))
                return BadRequest("Label e Valor são obrigatórios.");

            existing.Label = input.Label.Trim();
            existing.Valor = input.Valor.Trim();
            existing.Descricao = input.Descricao?.Trim();
            existing.Ativo = input.Ativo;
            existing.Ordem = input.Ordem;

            await _db.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id:int}")]
        [Authorize(Policy = "Gestor")]
        public async Task<IActionResult> Delete(int id)
        {
            var existing = await _db.FormasApoio.FindAsync(id);
            if (existing == null) return NotFound();

            _db.FormasApoio.Remove(existing);
            await _db.SaveChangesAsync();
            return NoContent();
        }
    }
}
