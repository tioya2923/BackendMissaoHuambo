using Microsoft.AspNetCore.Mvc;
using MissaoBackend.Data;
using MissaoBackend.Models;
using MissaoBackend.Utils;
using Microsoft.EntityFrameworkCore;

namespace MissaoBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CatecismoPtController : ControllerBase
    {
        private readonly AppDbContext _context;
        public CatecismoPtController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<CatecismoPt>>> GetAll([FromQuery] int? topicoId = null)
        {
            var query = _context.CatecismosPt.AsQueryable();
            if (topicoId.HasValue)
                query = query.Where(c => c.CatecismoPtTopicoId == topicoId);
            return await query.ToListAsync();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<CatecismoPt>> GetById(int id)
        {
            var item = await _context.CatecismosPt.FindAsync(id);
            if (item == null) return NotFound();
            return item;
        }

        [HttpPost]
        public async Task<ActionResult<CatecismoPt>> Create(CatecismoPt catecismo)
        {
            catecismo.Slug = SlugHelper.Slugify(catecismo.Titulo);
            _context.CatecismosPt.Add(catecismo);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetById), new { id = catecismo.Id }, catecismo);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, CatecismoPt catecismo)
        {
            if (id != catecismo.Id) return BadRequest();
            catecismo.Slug = SlugHelper.Slugify(catecismo.Titulo);
            _context.Entry(catecismo).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var item = await _context.CatecismosPt.FindAsync(id);
            if (item == null) return NotFound();
            _context.CatecismosPt.Remove(item);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}