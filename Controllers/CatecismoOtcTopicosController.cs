using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MissaoBackend.Data;
using MissaoBackend.Models;

namespace MissaoBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CatecismoOtcTopicosController : ControllerBase
    {
        private readonly AppDbContext _context;
        public CatecismoOtcTopicosController(AppDbContext context)
        {
            _context = context;
        }

        [HttpPost]
        [Authorize(Policy = "Gestor")]
        public async Task<ActionResult<CatecismoOtcTopico>> Create(CatecismoOtcTopico input)
        {
            input.Slug = MissaoBackend.Utils.SlugHelper.Slugify(input.Titulo);
            _context.CatecismoOtcTopicos.Add(input);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetAll), new { id = input.Id }, input);
        }

        // Permitir POST também em /api/CatecismoOtcTopicos/topicos
        [HttpPost("topicos")]
        [Authorize(Policy = "Gestor")]
        public async Task<ActionResult<CatecismoOtcTopico>> CreateTopico(CatecismoOtcTopico input)
        {
            input.Slug = MissaoBackend.Utils.SlugHelper.Slugify(input.Titulo);
            _context.CatecismoOtcTopicos.Add(input);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetAll), new { id = input.Id }, input);
        }

        [HttpPut("{id:int}")]
        [Authorize(Policy = "Gestor")]
        public async Task<IActionResult> Update(int id, CatecismoOtcTopico input)
        {
            var existing = await _context.CatecismoOtcTopicos.FindAsync(id);
            if (existing == null) return NotFound();

            existing.Titulo = input.Titulo;
            existing.Slug = MissaoBackend.Utils.SlugHelper.Slugify(input.Titulo);

            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id:int}")]
        [Authorize(Policy = "Gestor")]
        public async Task<IActionResult> Delete(int id)
        {
            var existing = await _context.CatecismoOtcTopicos.FindAsync(id);
            if (existing == null) return NotFound();

            var temConteudo = await _context.CatecismosOtc.AnyAsync(c => c.CatecismoOtcTopicoId == id);
            if (temConteudo)
                return BadRequest("Não é possível eliminar: existem conteúdos associados a este tópico.");

            _context.CatecismoOtcTopicos.Remove(existing);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<CatecismoOtcTopico>>> GetAll()
        {
            return await _context.CatecismoOtcTopicos.ToListAsync();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<CatecismoOtcTopico>> GetById(int id)
        {
            var item = await _context.CatecismoOtcTopicos.FirstOrDefaultAsync(t => t.Id == id);
            if (item == null) return NotFound();
            return item;
        }

        [HttpGet("slug/{slug}")]
        public async Task<ActionResult<CatecismoOtcTopico>> GetBySlug(string slug)
        {
            var item = await _context.CatecismoOtcTopicos.FirstOrDefaultAsync(t => t.Slug == slug);
            if (item == null) return NotFound();
            return item;
        }
    }
}
