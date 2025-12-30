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
        [HttpPost]
        public async Task<ActionResult<CatecismoUbTopico>> Create(CatecismoUbTopico input)
        {
            input.Slug = MissaoBackend.Utils.SlugHelper.Slugify(input.Titulo);
            _context.CatecismoUbTopicos.Add(input);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetAll), new { id = input.Id }, input);
        }

        // Permitir POST também em /api/CatecismoUbTopicos/topicos
        [HttpPost("topicos")]
        public async Task<ActionResult<CatecismoUbTopico>> CreateTopico(CatecismoUbTopico input)
        {
            input.Slug = MissaoBackend.Utils.SlugHelper.Slugify(input.Titulo);
            _context.CatecismoUbTopicos.Add(input);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetAll), new { id = input.Id }, input);
        }
        private readonly AppDbContext _context;
        public CatecismoUbTopicosController(AppDbContext context)
        {
            _context = context;
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