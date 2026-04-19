using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MissaoBackend.Data;

namespace MissaoBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SearchController : ControllerBase
    {
        private readonly AppDbContext _context;

        public SearchController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Search([FromQuery] string q)
        {
            if (string.IsNullOrWhiteSpace(q))
                return BadRequest("Query string is required.");

            var canticos = await _context.Canticos
                .Where(c => c.Titulo.Contains(q) || c.Letra.Contains(q))
                .Select(c => new { c.Id, c.Titulo, c.Slug })
                .ToListAsync();

            var canticosUmb = await _context.CanticosUmb
                .Where(c => c.Titulo.Contains(q) || c.Letra.Contains(q))
                .Select(c => new { c.Id, c.Titulo, c.Slug })
                .ToListAsync();

            var topicos = await _context.Topicos
                .Where(t => t.Nome.Contains(q) || t.Slug.Contains(q))
                .Select(t => new { t.Id, t.Nome, t.Slug })
                .ToListAsync();

            var topicosUmb = await _context.TopicosUmb
                .Where(t => t.Nome.Contains(q) || t.Slug.Contains(q))
                .Select(t => new { t.Id, t.Nome, t.Slug })
                .ToListAsync();

            var catecismosPt = await _context.CatecismosPt
                .Where(c => c.Titulo.Contains(q) || c.Texto.Contains(q))
                .Select(c => new { c.Id, c.Titulo, c.Slug })
                .ToListAsync();

            var catecismosUb = await _context.CatecismosUb
                .Where(c => c.Titulo.Contains(q) || c.Texto.Contains(q))
                .Select(c => new { c.Id, c.Titulo })
                .ToListAsync();

            var eventos = await _context.Eventos
                .Where(e => e.Titulo.Contains(q) || e.Descricao.Contains(q) || e.Leituras.Contains(q) || e.Observacoes.Contains(q))
                .Select(e => new { e.Id, e.Titulo, e.Data })
                .ToListAsync();

            return Ok(new {
                canticos,
                canticosUmb,
                topicos,
                topicosUmb,
                catecismosPt,
                catecismosUb,
                eventos
            });
        }
    }
}
