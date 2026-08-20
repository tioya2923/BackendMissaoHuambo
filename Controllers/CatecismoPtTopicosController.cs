using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MissaoBackend.Data;
using MissaoBackend.Models;
using MissaoBackend.Utils;

namespace MissaoBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]/topicos")]
    public class CatecismoPtTopicosController : ControllerBase
    {
        private readonly AppDbContext _context;
        public CatecismoPtTopicosController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<CatecismoPtTopico>>> GetAll()
        {
            return await _context.CatecismoPtTopicos
                .Where(t => t.ParentId == null)
                .ToListAsync();
        }

        // Lista plana com todos os tópicos e subtópicos — usada pelo painel de administração
        [HttpGet("todos")]
        [Authorize(Policy = "Gestor")]
        public async Task<ActionResult<IEnumerable<CatecismoPtTopico>>> GetTodosPlano()
        {
            return await _context.CatecismoPtTopicos
                .AsNoTracking()
                .OrderBy(t => t.Titulo)
                .ToListAsync();
        }

        [HttpGet("{id}/subtopicos")]
        public async Task<ActionResult<IEnumerable<CatecismoPtTopico>>> GetSubTopicos(int id)
        {
            return await _context.CatecismoPtTopicos
                .Where(t => t.ParentId == id)
                .ToListAsync();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<CatecismoPtTopico>> GetById(int id)
        {
            var item = await _context.CatecismoPtTopicos.FirstOrDefaultAsync(t => t.Id == id);
            if (item == null) return NotFound();
            return item;
        }

        [HttpGet("slug/{slug}")]
        public async Task<ActionResult<CatecismoPtTopico>> GetBySlug(string slug)
        {
            var item = await _context.CatecismoPtTopicos.FirstOrDefaultAsync(t => t.Slug == slug);
            if (item == null) return NotFound();
            return item;
        }

        [HttpPost]
        [Authorize(Policy = "Gestor")]
        public async Task<ActionResult<CatecismoPtTopico>> Create(CatecismoPtTopico input)
        {
            input.Slug = SlugHelper.Slugify(input.Titulo);
            _context.CatecismoPtTopicos.Add(input);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetAll), new { id = input.Id }, input);
        }

        [HttpPut("{id:int}")]
        [Authorize(Policy = "Gestor")]
        public async Task<IActionResult> Update(int id, CatecismoPtTopico input)
        {
            var existing = await _context.CatecismoPtTopicos.FindAsync(id);
            if (existing == null) return NotFound();

            if (input.ParentId == id)
                return BadRequest("Um tópico não pode ser subtópico de si mesmo.");

            existing.Titulo = input.Titulo;
            existing.Slug = SlugHelper.Slugify(input.Titulo);
            existing.ParentId = input.ParentId;

            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id:int}")]
        [Authorize(Policy = "Gestor")]
        public async Task<IActionResult> Delete(int id)
        {
            var existing = await _context.CatecismoPtTopicos.FindAsync(id);
            if (existing == null) return NotFound();

            var temPerguntas = await _context.CatecismosPt.AnyAsync(c => c.CatecismoPtTopicoId == id);
            if (temPerguntas)
                return BadRequest("Não é possível eliminar: existem perguntas associadas a este tópico.");

            var temSubTopicos = await _context.CatecismoPtTopicos.AnyAsync(t => t.ParentId == id);
            if (temSubTopicos)
                return BadRequest("Não é possível eliminar: existem subtópicos associados a este tópico.");

            _context.CatecismoPtTopicos.Remove(existing);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
