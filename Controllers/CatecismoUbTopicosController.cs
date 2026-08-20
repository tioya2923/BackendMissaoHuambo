using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MissaoBackend.Data;
using MissaoBackend.Models;

namespace MissaoBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CatecismoUbTopicosController : ControllerBase
    {
        private readonly AppDbContext _context;
        public CatecismoUbTopicosController(AppDbContext context)
        {
            _context = context;
        }

        [HttpPost]
        [Authorize(Policy = "Gestor")]
        public async Task<ActionResult<CatecismoUbTopico>> Create(CatecismoUbTopico input)
        {
            input.Slug = MissaoBackend.Utils.SlugHelper.Slugify(input.Titulo);
            _context.CatecismoUbTopicos.Add(input);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetAll), new { id = input.Id }, input);
        }

        // Permitir POST também em /api/CatecismoUbTopicos/topicos
        [HttpPost("topicos")]
        [Authorize(Policy = "Gestor")]
        public async Task<ActionResult<CatecismoUbTopico>> CreateTopico(CatecismoUbTopico input)
        {
            input.Slug = MissaoBackend.Utils.SlugHelper.Slugify(input.Titulo);
            _context.CatecismoUbTopicos.Add(input);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetAll), new { id = input.Id }, input);
        }

        [HttpPut("{id:int}")]
        [Authorize(Policy = "Gestor")]
        public async Task<IActionResult> Update(int id, CatecismoUbTopico input)
        {
            var existing = await _context.CatecismoUbTopicos.FindAsync(id);
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
            var existing = await _context.CatecismoUbTopicos.FindAsync(id);
            if (existing == null) return NotFound();

            var temPerguntas = await _context.CatecismosUb.AnyAsync(c => c.CatecismoUbTopicoId == id);
            if (temPerguntas)
                return BadRequest("Não é possível eliminar: existem perguntas associadas a este tópico.");

            _context.CatecismoUbTopicos.Remove(existing);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<CatecismoUbTopico>>> GetAll()
        {
            // Removido Include para evitar referência circular/erro de serialização
            return await _context.CatecismoUbTopicos.ToListAsync();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<CatecismoUbTopico>> GetById(int id)
        {
            var item = await _context.CatecismoUbTopicos.FirstOrDefaultAsync(t => t.Id == id);
            if (item == null) return NotFound();
            return item;
        }

        [HttpGet("slug/{slug}")]
        public async Task<ActionResult<CatecismoUbTopico>> GetBySlug(string slug)
        {
            var item = await _context.CatecismoUbTopicos.FirstOrDefaultAsync(t => t.Slug == slug);
            if (item == null) return NotFound();
            return item;
        }
    }
}
