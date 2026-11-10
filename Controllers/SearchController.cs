using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using MissaoBackend.Data;
using MissaoBackend.Models;

namespace MissaoBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SearchController : ControllerBase
    {
        private readonly IServiceScopeFactory _scopes;
        private readonly IMemoryCache _cache;

        public SearchController(IServiceScopeFactory scopes, IMemoryCache cache)
        {
            _scopes = scopes;
            _cache = cache;
        }

        // Limite por secção — sem isto, uma palavra comum dentro do corpo de
        // um cântico/catecismo (ex.: "o senhor") pode devolver dezenas de
        // resultados de uma vez, tornando o dropdown de pesquisa gigante e
        // difícil de ler. Também ordena para que correspondências no título
        // apareçam primeiro, à frente de correspondências só no texto.
        private const int LimitePorSeccao = 8;

        // A base de dados está longe: cada consulta custa ~0,6 s, e antes eram 17 seguidas (~10 s).
        // Agora as consultas independentes correm em paralelo (cada uma com o seu contexto, porque um
        // DbContext não é seguro entre threads), com um tecto de ligações simultâneas.
        private static readonly SemaphoreSlim Ligacoes = new(10);

        private record ItemCantico(int Id, string Titulo, string Slug);
        private record ItemTopico(int Id, string Nome, string Slug);
        private record ItemCatecismo(int Id, string Titulo, string? Slug);

        private async Task<T> Corre<T>(Func<AppDbContext, Task<T>> consulta)
        {
            await Ligacoes.WaitAsync();
            try
            {
                using var scope = _scopes.CreateScope();
                return await consulta(scope.ServiceProvider.GetRequiredService<AppDbContext>());
            }
            finally { Ligacoes.Release(); }
        }

        // Os idiomas quase nunca mudam: guardam-se 5 minutos em memória.
        private async Task<List<Idioma>> IdiomasActivos() =>
            (await _cache.GetOrCreateAsync("pesquisa-idiomas-activos", async entrada =>
            {
                entrada.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);
                return await Corre(db => db.Idiomas.AsNoTracking().Where(i => i.Ativo).OrderBy(i => i.Ordem).ToListAsync());
            }))!;

        // Primeiro só nos títulos (coluna curta e rápida); só se não chegar ao limite é que se procura
        // no texto, que é o que pesa. O resultado é o mesmo de antes: títulos primeiro, depois o texto.
        private Task<List<ItemCantico>> Canticos(int idiomaId, string q) => Corre(async db =>
        {
            var porTitulo = await db.Canticos.AsNoTracking()
                .Where(c => c.IdiomaId == idiomaId && c.Titulo.Contains(q))
                .OrderBy(c => c.Titulo)
                .Take(LimitePorSeccao)
                .Select(c => new ItemCantico(c.Id, c.Titulo, c.Slug))
                .ToListAsync();
            if (porTitulo.Count >= LimitePorSeccao) return porTitulo;

            var porTexto = await db.Canticos.AsNoTracking()
                .Where(c => c.IdiomaId == idiomaId && !c.Titulo.Contains(q) && c.Letra.Contains(q))
                .OrderBy(c => c.Titulo)
                .Take(LimitePorSeccao - porTitulo.Count)
                .Select(c => new ItemCantico(c.Id, c.Titulo, c.Slug))
                .ToListAsync();
            return porTitulo.Concat(porTexto).ToList();
        });

        private Task<List<ItemTopico>> Topicos(int idiomaId, string q) => Corre(db =>
            db.Topicos.AsNoTracking()
                .Where(t => t.IdiomaId == idiomaId && (t.Nome.Contains(q) || t.Slug.Contains(q)) && db.Canticos.Any(c => c.TopicoId == t.Id))
                .Take(LimitePorSeccao)
                .Select(t => new ItemTopico(t.Id, t.Nome, t.Slug))
                .ToListAsync());

        private Task<List<ItemCatecismo>> Catecismos(int idiomaId, string q) => Corre(async db =>
        {
            var porTitulo = await db.CatecismosPt.AsNoTracking()
                .Where(c => c.IdiomaId == idiomaId && c.Titulo.Contains(q))
                .OrderBy(c => c.Titulo)
                .Take(LimitePorSeccao)
                .Select(c => new ItemCatecismo(c.Id, c.Titulo, c.Slug))
                .ToListAsync();
            if (porTitulo.Count >= LimitePorSeccao) return porTitulo;

            var porTexto = await db.CatecismosPt.AsNoTracking()
                .Where(c => c.IdiomaId == idiomaId && !c.Titulo.Contains(q) && c.Texto.Contains(q))
                .OrderBy(c => c.Titulo)
                .Take(LimitePorSeccao - porTitulo.Count)
                .Select(c => new ItemCatecismo(c.Id, c.Titulo, c.Slug))
                .ToListAsync();
            return porTitulo.Concat(porTexto).ToList();
        });

        private record ItemEvento(int Id, string Titulo, DateTime Data);

        private Task<List<ItemEvento>> Eventos(string q) => Corre(async db =>
        {
            var porTitulo = await db.Eventos.AsNoTracking()
                .Where(e => e.Titulo.Contains(q))
                .OrderBy(e => e.Data)
                .Take(LimitePorSeccao)
                .Select(e => new ItemEvento(e.Id, e.Titulo, e.Data))
                .ToListAsync();
            if (porTitulo.Count >= LimitePorSeccao) return porTitulo;

            var porTexto = await db.Eventos.AsNoTracking()
                .Where(e => !e.Titulo.Contains(q) && (e.Descricao.Contains(q) || e.Leituras.Contains(q) || e.Observacoes.Contains(q)))
                .OrderBy(e => e.Data)
                .Take(LimitePorSeccao - porTitulo.Count)
                .Select(e => new ItemEvento(e.Id, e.Titulo, e.Data))
                .ToListAsync();
            return porTitulo.Concat(porTexto).ToList();
        });

        private async Task<(List<ItemCantico> Canticos, List<ItemTopico> Topicos, List<ItemCatecismo> Catecismos)>
            PesquisarIdioma(int idiomaId, string q)
        {
            var canticos = Canticos(idiomaId, q);
            var topicos = Topicos(idiomaId, q);
            var catecismos = Catecismos(idiomaId, q);
            await Task.WhenAll(canticos, topicos, catecismos);
            return (canticos.Result, topicos.Result, catecismos.Result);
        }

        [HttpGet]
        [OutputCache(PolicyName = "Leitura")]
        public async Task<IActionResult> Search([FromQuery] string q)
        {
            if (string.IsNullOrWhiteSpace(q))
                return BadRequest("Query string is required.");

            // Todos os idiomas vivem nas mesmas tabelas genéricas (Canticos, Topicos, CatecismosPt),
            // separados por IdiomaId — é aí que o admin edita, por isso a pesquisa lê de lá.
            // As tabelas antigas (CanticosUmb, CatecismosUb, …) já não são mantidas.
            var idiomas = await IdiomasActivos();

            var eventos = Eventos(q);
            var porIdioma = idiomas.ToDictionary(i => i.Id, i => PesquisarIdioma(i.Id, q));
            await Task.WhenAll(porIdioma.Values.Cast<Task>().Append(eventos));

            var vazio = (Canticos: new List<ItemCantico>(), Topicos: new List<ItemTopico>(), Catecismos: new List<ItemCatecismo>());
            var pt = idiomas.FirstOrDefault(i => i.Codigo == "pt");
            var umb = idiomas.FirstOrDefault(i => i.Codigo == "umb");
            var resPt = pt != null ? porIdioma[pt.Id].Result : vazio;
            var resUmb = umb != null ? porIdioma[umb.Id].Result : vazio;

            // Os restantes idiomas (Latim, Kimbundu, Oshikwanhama e os que o admin criar)
            // vêm numa lista própria, só com os que tiveram resultados.
            var outrosIdiomas = new List<object>();
            foreach (var idioma in idiomas.Where(i => i.Codigo != "pt" && i.Codigo != "umb"))
            {
                var r = porIdioma[idioma.Id].Result;
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
                eventos = eventos.Result.Select(e => new { e.Id, e.Titulo, e.Data })
            });
        }
    }
}
