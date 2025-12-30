using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MissaoBackend.Data;
using MissaoBackend.Models;

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
            // Removido Include para evitar referência circular/erro de serialização
            return await _context.CatecismoPtTopicos.ToListAsync();
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
        public async Task<ActionResult<CatecismoPtTopico>> Create(CatecismoPtTopico input)
        {
            input.Slug = MissaoBackend.Utils.SlugHelper.Slugify(input.Titulo);
            _context.CatecismoPtTopicos.Add(input);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetAll), new { id = input.Id }, input);
        }
    }
}