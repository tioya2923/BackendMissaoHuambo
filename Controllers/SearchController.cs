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

        // Limite por secção — sem isto, uma palavra comum dentro do corpo de
        // um cântico/catecismo (ex.: "o senhor") pode devolver dezenas de
        // resultados de uma vez, tornando o dropdown de pesquisa gigante e
        // difícil de ler. Também ordena para que correspondências no título
        // apareçam primeiro, à frente de correspondências só no texto.
        private const int LimitePorSeccao = 8;

        [HttpGet]
        public async Task<IActionResult> Search([FromQuery] string q)
        {
            if (string.IsNullOrWhiteSpace(q))
                return BadRequest("Query string is required.");

            // A tabela Canticos passou a ser genérica (todos os idiomas); esta pesquisa
            // global continua a mostrar só Português aqui — o idioma "canticosUmb" abaixo
            // continua à parte — para não duplicar nem misturar resultados de outros idiomas.
            var canticos = await _context.Canticos
                .Where(c => c.IdiomaId == 1 && (c.Titulo.Contains(q) || c.Letra.Contains(q)))
                .OrderByDescending(c => c.Titulo.Contains(q))
                .ThenBy(c => c.Titulo)
                .Take(LimitePorSeccao)
                .Select(c => new { c.Id, c.Titulo, c.Slug })
                .ToListAsync();

            var canticosUmb = await _context.CanticosUmb
                .Where(c => c.Titulo.Contains(q) || c.Letra.Contains(q))
                .OrderByDescending(c => c.Titulo.Contains(q))
                .ThenBy(c => c.Titulo)
                .Take(LimitePorSeccao)
                .Select(c => new { c.Id, c.Titulo, c.Slug })
                .ToListAsync();

            var topicos = await _context.Topicos
                .Where(t => t.IdiomaId == 1 && (t.Nome.Contains(q) || t.Slug.Contains(q)))
                .Take(LimitePorSeccao)
                .Select(t => new { t.Id, t.Nome, t.Slug })
                .ToListAsync();

            var topicosUmb = await _context.TopicosUmb
                .Where(t => t.Nome.Contains(q) || t.Slug.Contains(q))
                .Take(LimitePorSeccao)
                .Select(t => new { t.Id, t.Nome, t.Slug })
                .ToListAsync();

            var catecismosPt = await _context.CatecismosPt
                .Where(c => c.IdiomaId == 1 && (c.Titulo.Contains(q) || c.Texto.Contains(q)))
                .OrderByDescending(c => c.Titulo.Contains(q))
                .ThenBy(c => c.Titulo)
                .Take(LimitePorSeccao)
                .Select(c => new { c.Id, c.Titulo, c.Slug })
                .ToListAsync();

            var catecismosUb = await _context.CatecismosUb
                .Where(c => c.Titulo.Contains(q) || c.Texto.Contains(q))
                .OrderByDescending(c => c.Titulo.Contains(q))
                .ThenBy(c => c.Titulo)
                .Take(LimitePorSeccao)
                .Select(c => new { c.Id, c.Titulo })
                .ToListAsync();

            var eventos = await _context.Eventos
                .Where(e => e.Titulo.Contains(q) || e.Descricao.Contains(q) || e.Leituras.Contains(q) || e.Observacoes.Contains(q))
                .OrderByDescending(e => e.Titulo.Contains(q))
                .ThenBy(e => e.Data)
                .Take(LimitePorSeccao)
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
