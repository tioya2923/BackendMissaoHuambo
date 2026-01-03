using Microsoft.AspNetCore.Mvc;
using MissaoBackend.Data;
using MissaoBackend.Models;
using System.Linq;

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
        public IActionResult Search([FromQuery] string q)
        {
            if (string.IsNullOrWhiteSpace(q))
                return BadRequest("Query string is required.");

            var noticias = _context.Noticias.Where(n => n.Titulo.Contains(q) || n.Texto.Contains(q)).ToList();
            var canticos = _context.Canticos.Where(c => c.Titulo.Contains(q) || c.Letra.Contains(q)).ToList();
            var canticosUmb = _context.CanticosUmb.Where(c => c.Titulo.Contains(q) || c.Letra.Contains(q)).ToList();
            var topicos = _context.Topicos.Where(t => t.Nome.Contains(q) || t.Slug.Contains(q)).ToList();
            var topicosUmb = _context.TopicosUmb.Where(t => t.Nome.Contains(q) || t.Slug.Contains(q)).ToList();
            var catecismosPt = _context.CatecismosPt.Where(c => c.Titulo.Contains(q) || c.Texto.Contains(q)).ToList();
            var catecismosUb = _context.CatecismosUb.Where(c => c.Titulo.Contains(q) || c.Texto.Contains(q)).ToList();
            var eventos = _context.Eventos.Where(e => e.Titulo.Contains(q) || e.Descricao.Contains(q) || e.Leituras.Contains(q) || e.Observacoes.Contains(q)).ToList();
            var fotos = _context.Photos.Where(f => f.DescricaoCurta.Contains(q) || f.DescricaoLonga.Contains(q)).ToList();

            return Ok(new {
                noticias,
                canticos,
                canticosUmb,
                topicos,
                topicosUmb,
                catecismosPt,
                catecismosUb,
                eventos,
                fotos
            });
        }
    }
}
