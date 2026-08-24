using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MissaoBackend.Data;
using MissaoBackend.Models;
using Microsoft.EntityFrameworkCore;

namespace MissaoBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CatecismoOtcController : ControllerBase
    {
        private readonly AppDbContext _context;
        public CatecismoOtcController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<CatecismoOtc>>> GetAll([FromQuery] int? topicoId = null)
        {
            var query = _context.CatecismosOtc.AsQueryable();
            if (topicoId.HasValue)
                query = query.Where(c => c.CatecismoOtcTopicoId == topicoId);
            return await query.ToListAsync();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<CatecismoOtc>> GetById(int id)
        {
            var item = await _context.CatecismosOtc.FindAsync(id);
            if (item == null) return NotFound();
            return item;
        }

        [HttpPost]
        [Authorize(Policy = "Gestor")]
        public async Task<ActionResult<CatecismoOtc>> Create(CatecismoOtc catecismo)
        {
            _context.CatecismosOtc.Add(catecismo);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetById), new { id = catecismo.Id }, catecismo);
        }

        [HttpPut("{id}")]
        [Authorize(Policy = "Gestor")]
        public async Task<IActionResult> Update(int id, CatecismoOtc catecismo)
        {
            if (id != catecismo.Id) return BadRequest();
            _context.Entry(catecismo).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id}")]
        [Authorize(Policy = "Gestor")]
        public async Task<IActionResult> Delete(int id)
        {
            var item = await _context.CatecismosOtc.FindAsync(id);
            if (item == null) return NotFound();
            _context.CatecismosOtc.Remove(item);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
