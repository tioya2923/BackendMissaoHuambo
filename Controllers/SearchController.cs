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

        private record ItemCantico(int Id, string Titulo, string Slug);
        private record ItemTopico(int Id, string Nome, string Slug);
        private record ItemCatecismo(int Id, string Titulo, string? Slug);

        private async Task<(List<ItemCantico> Canticos, List<ItemTopico> Topicos, List<ItemCatecismo> Catecismos)>
            PesquisarIdioma(int idiomaId, string q)
        {
            var canticos = await _context.Canticos
                .AsNoTracking()
                .Where(c => c.IdiomaId == idiomaId && (c.Titulo.Contains(q) || c.Letra.Contains(q)))
                .OrderByDescending(c => c.Titulo.Contains(q))
                .ThenBy(c => c.Titulo)
                .Take(LimitePorSeccao)
                .Select(c => new ItemCantico(c.Id, c.Titulo, c.Slug))
                .ToListAsync();

            var topicos = await _context.Topicos
                .AsNoTracking()
                .Where(t => t.IdiomaId == idiomaId && (t.Nome.Contains(q) || t.Slug.Contains(q)))
                .Take(LimitePorSeccao)
                .Select(t => new ItemTopico(t.Id, t.Nome, t.Slug))
                .ToListAsync();

            var catecismos = await _context.CatecismosPt
                .AsNoTracking()
                .Where(c => c.IdiomaId == idiomaId && (c.Titulo.Contains(q) || c.Texto.Contains(q)))
                .OrderByDescending(c => c.Titulo.Contains(q))
                .ThenBy(c => c.Titulo)
                .Take(LimitePorSeccao)
                .Select(c => new ItemCatecismo(c.Id, c.Titulo, c.Slug))
                .ToListAsync();

            return (canticos, topicos, catecismos);
        }

        [HttpGet]
        public async Task<IActionResult> Search([FromQuery] string q)
        {
            if (string.IsNullOrWhiteSpace(q))
                return BadRequest("Query string is required.");

            // Todos os idiomas vivem nas mesmas tabelas genéricas (Canticos, Topicos, CatecismosPt),
            // separados por IdiomaId — é aí que o admin edita, por isso a pesquisa lê de lá.
            // As tabelas antigas (CanticosUmb, CatecismosUb, …) já não são mantidas.
            var idiomas = await _context.Idiomas
                .AsNoTracking()
                .Where(i => i.Ativo)
                .OrderBy(i => i.Ordem)
                .ToListAsync();

            var vazio = (Canticos: new List<ItemCantico>(), Topicos: new List<ItemTopico>(), Catecismos: new List<ItemCatecismo>());

            var pt = idiomas.FirstOrDefault(i => i.Codigo == "pt");
            var umb = idiomas.FirstOrDefault(i => i.Codigo == "umb");
            var resPt = pt != null ? await PesquisarIdioma(pt.Id, q) : vazio;
            var resUmb = umb != null ? await PesquisarIdioma(umb.Id, q) : vazio;

            // Os restantes idiomas (Latim, Kimbundu, Oshikwanhama e os que o admin criar)
            // vêm numa lista própria, só com os que tiveram resultados.
            var outrosIdiomas = new List<object>();
            foreach (var idioma in idiomas.Where(i => i.Codigo != "pt" && i.Codigo != "umb"))
            {
                var r = await PesquisarIdioma(idioma.Id, q);
                if (r.Canticos.Count + r.Topicos.Count + r.Catecismos.Count == 0) continue;
                outrosIdiomas.Add(new
                {
                    codigo = idioma.Codigo,
                    nome = idioma.Nome,
                    canticos = r.Canticos,
                    topicos = r.Topicos,
                    catecismos = r.Catecismos
                });
            }

            var eventos = await _context.Eventos
                .AsNoTracking()
                .Where(e => e.Titulo.Contains(q) || e.Descricao.Contains(q) || e.Leituras.Contains(q) || e.Observacoes.Contains(q))
                .OrderByDescending(e => e.Titulo.Contains(q))
                .ThenBy(e => e.Data)
                .Take(LimitePorSeccao)
                .Select(e => new { e.Id, e.Titulo, e.Data })
                .ToListAsync();

            // As chaves antigas mantêm-se (versões da app já instaladas continuam a funcionar);
            // "outrosIdiomas" é o que é novo.
            return Ok(new
            {
                canticos = resPt.Canticos,
                canticosUmb = resUmb.Canticos,
                topicos = resPt.Topicos,
                topicosUmb = resUmb.Topicos,
                catecismosPt = resPt.Catecismos,
                catecismosUb = resUmb.Catecismos,
                outrosIdiomas,
                eventos
            });
        }
    }
}
