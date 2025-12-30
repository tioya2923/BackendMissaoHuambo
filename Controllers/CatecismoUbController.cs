using Microsoft.AspNetCore.Mvc;
using MissaoBackend.Data;
using MissaoBackend.Models;
using Microsoft.EntityFrameworkCore;

namespace MissaoBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CatecismoUbController : ControllerBase
    {
        private readonly AppDbContext _context;
        public CatecismoUbController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<CatecismoUb>>> GetAll([FromQuery] int? topicoId = null)
        {
            var query = _context.CatecismosUb.AsQueryable();
            if (topicoId.HasValue)
                query = query.Where(c => c.CatecismoUbTopicoId == topicoId);
            return await query.ToListAsync();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<CatecismoUb>> GetById(int id)
        {
            var item = await _context.CatecismosUb.FindAsync(id);
            if (item == null) return NotFound();
            return item;
        }

        [HttpPost]
        public async Task<ActionResult<CatecismoUb>> Create(CatecismoUb catecismo)
        {
            _context.CatecismosUb.Add(catecismo);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetById), new { id = catecismo.Id }, catecismo);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, CatecismoUb catecismo)
        {
            if (id != catecismo.Id) return BadRequest();
            _context.Entry(catecismo).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var item = await _context.CatecismosUb.FindAsync(id);
            if (item == null) return NotFound();
            _context.CatecismosUb.Remove(item);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}