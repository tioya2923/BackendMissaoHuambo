using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MissaoBackend.Data;
using MissaoBackend.Models;
using MissaoBackend.Utils;

namespace MissaoBackend.Controllers
{
    /// <summary>
    /// Tópicos de catecismo/orações, para qualquer idioma. Por omissão devolve/gere
    /// conteúdo em Português (?idioma=pt), para manter compatibilidade com clientes antigos.
    /// </summary>
    [ApiController]
    [Route("api/[controller]/topicos")]
    public class CatecismoPtTopicosController : ControllerBase
    {
        private readonly AppDbContext _context;
        public CatecismoPtTopicosController(AppDbContext context)
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
        public async Task<ActionResult<IEnumerable<CatecismoPtTopico>>> GetAll([FromQuery] string idioma = "pt")
        {
            var idiomaId = await ResolverIdiomaId(idioma);
            if (idiomaId == null) return BadRequest($"Idioma '{idioma}' não existe.");

            return await _context.CatecismoPtTopicos
                .Where(t => t.ParentId == null && t.IdiomaId == idiomaId)
                .ToListAsync();
        }

        // Lista plana com todos os tópicos e subtópicos — usada pelo painel de administração
        [HttpGet("todos")]
        [Authorize(Policy = "Gestor")]
        public async Task<ActionResult<IEnumerable<CatecismoPtTopico>>> GetTodosPlano([FromQuery] string idioma = "pt")
        {
            var idiomaId = await ResolverIdiomaId(idioma);
            if (idiomaId == null) return BadRequest($"Idioma '{idioma}' não existe.");

            return await _context.CatecismoPtTopicos
                .AsNoTracking()
                .Where(t => t.IdiomaId == idiomaId)
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
        public async Task<ActionResult<CatecismoPtTopico>> GetBySlug(string slug, [FromQuery] string idioma = "pt")
        {
            var idiomaId = await ResolverIdiomaId(idioma);
            if (idiomaId == null) return BadRequest($"Idioma '{idioma}' não existe.");

            var item = await _context.CatecismoPtTopicos.FirstOrDefaultAsync(t => t.Slug == slug && t.IdiomaId == idiomaId);
            if (item == null) return NotFound();
            return item;
        }

        [HttpPost]
        [Authorize(Policy = "Gestor")]
        public async Task<ActionResult<CatecismoPtTopico>> Create(CatecismoPtTopico input, [FromQuery] string idioma = "pt")
        {
            if (input.IdiomaId == 0)
            {
                var idiomaId = await ResolverIdiomaId(idioma);
                if (idiomaId == null) return BadRequest($"Idioma '{idioma}' não existe.");
                input.IdiomaId = idiomaId.Value;
            }

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
