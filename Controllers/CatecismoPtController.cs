using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MissaoBackend.Data;
using MissaoBackend.Models;
using MissaoBackend.Utils;
using Microsoft.EntityFrameworkCore;

namespace MissaoBackend.Controllers
{
    /// <summary>
    /// Catecismo e orações, para qualquer idioma. Por omissão devolve/gere conteúdo em
    /// Português (?idioma=pt), para manter compatibilidade com clientes antigos.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class CatecismoPtController : ControllerBase
    {
        private readonly AppDbContext _context;
        public CatecismoPtController(AppDbContext context)
        {
            _context = context;
        }

        private async Task<int?> ResolverIdiomaId(string idioma)
        {
            var codigo = string.IsNullOrWhiteSpace(idioma) ? "pt" : idioma.Trim().ToLowerInvariant();
            var id = await _context.Idiomas.Where(i => i.Codigo == codigo).Select(i => (int?)i.Id).FirstOrDefaultAsync();
            return id;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<CatecismoPt>>> GetAll([FromQuery] int? topicoId = null, [FromQuery] string idioma = "pt")
        {
            var idiomaId = await ResolverIdiomaId(idioma);
            if (idiomaId == null) return BadRequest($"Idioma '{idioma}' não existe.");

            var query = _context.CatecismosPt.Where(c => c.IdiomaId == idiomaId);
            if (topicoId.HasValue)
                query = query.Where(c => c.CatecismoPtTopicoId == topicoId);
            return await query.OrderBy(c => c.Id).ToListAsync();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<CatecismoPt>> GetById(int id)
        {
            var item = await _context.CatecismosPt.FindAsync(id);
            if (item == null) return NotFound();
            return item;
        }

        [HttpPost]
        [Authorize(Policy = "Gestor")]
        public async Task<ActionResult<CatecismoPt>> Create(CatecismoPt catecismo, [FromQuery] string idioma = "pt")
        {
            if (catecismo.IdiomaId == 0)
            {
                var idiomaId = await ResolverIdiomaId(idioma);
                if (idiomaId == null) return BadRequest($"Idioma '{idioma}' não existe.");
                catecismo.IdiomaId = idiomaId.Value;
            }

            catecismo.Slug = SlugHelper.Slugify(catecismo.Titulo);
            _context.CatecismosPt.Add(catecismo);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetById), new { id = catecismo.Id }, catecismo);
        }

        [HttpPut("{id}")]
        [Authorize(Policy = "Gestor")]
        public async Task<IActionResult> Update(int id, CatecismoPt catecismo)
        {
            if (id != catecismo.Id) return BadRequest();

            var existente = await _context.CatecismosPt.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id);
            if (existente == null) return NotFound();
            if (catecismo.IdiomaId == 0) catecismo.IdiomaId = existente.IdiomaId;

            catecismo.Slug = SlugHelper.Slugify(catecismo.Titulo);
            _context.Entry(catecismo).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id}")]
        [Authorize(Policy = "Gestor")]
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
