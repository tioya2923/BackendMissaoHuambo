using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MissaoBackend.Data;
using MissaoBackend.Models;

namespace MissaoBackend.Controllers
{
    /// <summary>
    /// Endpoints administrativos de manutenção, usados uma única vez para operações
    /// de migração de dados. Todos exigem sessão de Gestor.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = "Gestor")]
    public class DebugController : ControllerBase
    {
        private readonly AppDbContext _db;

        public DebugController(AppDbContext db)
        {
            _db = db;
        }

        /// <summary>
        /// Migra os cânticos/tópicos e o catecismo/orações de Umbundu, Latim, Kimbundu
        /// e Otchikwanyama das tabelas antigas (uma por idioma) para as tabelas genéricas
        /// Topico/Cantico e CatecismoPtTopico/CatecismoPt, marcando cada linha com o
        /// IdiomaId correto. As tabelas antigas NÃO são apagadas nem alteradas — ficam
        /// como cópia de segurança até se confirmar que tudo está correto.
        /// Idempotente: se já houver conteúdo com IdiomaId de um idioma, esse idioma é ignorado.
        /// </summary>
        [HttpPost("migrar-idiomas")]
        public async Task<ActionResult> MigrarIdiomas()
        {
            var idiomas = await _db.Idiomas.AsNoTracking().ToDictionaryAsync(i => i.Codigo, i => i.Id);
            var resultado = new Dictionary<string, object>();

            await using var transacao = await _db.Database.BeginTransactionAsync();

            // ── Cânticos: Umbundu, Latim, Kimbundu ──────────────────────────
            resultado["canticosUmbundu"] = await MigrarCanticos("umb");
            resultado["canticosLatim"] = await MigrarCanticos("lat");
            resultado["canticosKimbundu"] = await MigrarCanticos("kmb");

            // ── Catecismo/Orações: Umbundu, Latim, Otchikwanyama ────────────
            resultado["catecismoUmbundu"] = await MigrarCatecismoUmb();
            resultado["catecismoLatim"] = await MigrarCatecismoLat();
            resultado["catecismoOtchikwanyama"] = await MigrarCatecismoOtc();

            await transacao.CommitAsync();

            return Ok(resultado);

            async Task<object> MigrarCanticos(string codigoIdioma)
            {
                var idiomaId = idiomas[codigoIdioma];

                var jaMigrado = await _db.Topicos.AsNoTracking().AnyAsync(t => t.IdiomaId == idiomaId);
                if (jaMigrado)
                    return new { estado = "já migrado, ignorado" };

                var mapaTopicos = new Dictionary<int, int>();

                if (codigoIdioma == "umb")
                {
                    var topicosAntigos = await _db.TopicosUmb.AsNoTracking().ToListAsync();
                    foreach (var t in topicosAntigos)
                    {
                        var novo = new Topico { Nome = t.Nome, Slug = t.Slug, IdiomaId = idiomaId };
                        _db.Topicos.Add(novo);
                        await _db.SaveChangesAsync();
                        mapaTopicos[t.Id] = novo.Id;
                    }

                    var canticosAntigos = await _db.CanticosUmb.AsNoTracking().ToListAsync();
                    foreach (var c in canticosAntigos)
                    {
                        _db.Canticos.Add(new Cantico
                        {
                            Titulo = c.Titulo,
                            Slug = c.Slug,
                            Letra = c.Letra,
                            Autor = c.Autor,
                            PdfUrl = c.PdfUrl,
                            TopicoId = mapaTopicos[c.TopicoId],
                            IdiomaId = idiomaId
                        });
                    }
                    await _db.SaveChangesAsync();
                    return new { topicos = topicosAntigos.Count, canticos = canticosAntigos.Count };
                }

                if (codigoIdioma == "lat")
                {
                    var topicosAntigos = await _db.TopicosLat.AsNoTracking().ToListAsync();
                    foreach (var t in topicosAntigos)
                    {
                        var novo = new Topico { Nome = t.Nome, Slug = t.Slug, IdiomaId = idiomaId };
                        _db.Topicos.Add(novo);
                        await _db.SaveChangesAsync();
                        mapaTopicos[t.Id] = novo.Id;
                    }

                    var canticosAntigos = await _db.CanticosLat.AsNoTracking().ToListAsync();
                    foreach (var c in canticosAntigos)
                    {
                        _db.Canticos.Add(new Cantico
                        {
                            Titulo = c.Titulo,
                            Slug = c.Slug,
                            Letra = c.Letra,
                            Autor = c.Autor,
                            PdfUrl = c.PdfUrl,
                            TopicoId = mapaTopicos[c.TopicoId],
                            IdiomaId = idiomaId
                        });
                    }
                    await _db.SaveChangesAsync();
                    return new { topicos = topicosAntigos.Count, canticos = canticosAntigos.Count };
                }

                // kmb
                var topicosKmb = await _db.TopicosKmb.AsNoTracking().ToListAsync();
                foreach (var t in topicosKmb)
                {
                    var novo = new Topico { Nome = t.Nome, Slug = t.Slug, IdiomaId = idiomaId };
                    _db.Topicos.Add(novo);
                    await _db.SaveChangesAsync();
                    mapaTopicos[t.Id] = novo.Id;
                }

                var canticosKmb = await _db.CanticosKmb.AsNoTracking().ToListAsync();
                foreach (var c in canticosKmb)
                {
                    _db.Canticos.Add(new Cantico
                    {
                        Titulo = c.Titulo,
                        Slug = c.Slug,
                        Letra = c.Letra,
                        Autor = c.Autor,
                        PdfUrl = c.PdfUrl,
                        TopicoId = mapaTopicos[c.TopicoId],
                        IdiomaId = idiomaId
                    });
                }
                await _db.SaveChangesAsync();
                return new { topicos = topicosKmb.Count, canticos = canticosKmb.Count };
            }

            async Task<object> MigrarCatecismoUmb()
            {
                var idiomaId = idiomas["umb"];
                var jaMigrado = await _db.CatecismoPtTopicos.AsNoTracking().AnyAsync(t => t.IdiomaId == idiomaId);
                if (jaMigrado)
                    return new { estado = "já migrado, ignorado" };

                var mapa = new Dictionary<int, int>();
                var topicosAntigos = await _db.CatecismoUbTopicos.AsNoTracking().ToListAsync();
                foreach (var t in topicosAntigos)
                {
                    var novo = new CatecismoPtTopico { Titulo = t.Titulo, Slug = t.Slug, ParentId = null, IdiomaId = idiomaId };
                    _db.CatecismoPtTopicos.Add(novo);
                    await _db.SaveChangesAsync();
                    mapa[t.Id] = novo.Id;
                }

                var itensAntigos = await _db.CatecismosUb.AsNoTracking().ToListAsync();
                foreach (var c in itensAntigos)
                {
                    _db.CatecismosPt.Add(new CatecismoPt
                    {
                        Titulo = c.Titulo,
                        Texto = c.Texto,
                        Slug = null,
                        CatecismoPtTopicoId = mapa[c.CatecismoUbTopicoId],
                        IdiomaId = idiomaId
                    });
                }
                await _db.SaveChangesAsync();
                return new { topicos = topicosAntigos.Count, itens = itensAntigos.Count };
            }

            async Task<object> MigrarCatecismoLat()
            {
                var idiomaId = idiomas["lat"];
                var jaMigrado = await _db.CatecismoPtTopicos.AsNoTracking().AnyAsync(t => t.IdiomaId == idiomaId);
                if (jaMigrado)
                    return new { estado = "já migrado, ignorado" };

                var mapa = new Dictionary<int, int>();
                var topicosAntigos = await _db.CatecismoLatTopicos.AsNoTracking().ToListAsync();
                foreach (var t in topicosAntigos)
                {
                    var novo = new CatecismoPtTopico { Titulo = t.Titulo, Slug = t.Slug, ParentId = null, IdiomaId = idiomaId };
                    _db.CatecismoPtTopicos.Add(novo);
                    await _db.SaveChangesAsync();
                    mapa[t.Id] = novo.Id;
                }

                var itensAntigos = await _db.CatecismosLat.AsNoTracking().ToListAsync();
                foreach (var c in itensAntigos)
                {
                    _db.CatecismosPt.Add(new CatecismoPt
                    {
                        Titulo = c.Titulo,
                        Texto = c.Texto,
                        Slug = c.Slug,
                        CatecismoPtTopicoId = mapa[c.CatecismoLatTopicoId],
                        IdiomaId = idiomaId
                    });
                }
                await _db.SaveChangesAsync();
                return new { topicos = topicosAntigos.Count, itens = itensAntigos.Count };
            }

            async Task<object> MigrarCatecismoOtc()
            {
                var idiomaId = idiomas["otc"];
                var jaMigrado = await _db.CatecismoPtTopicos.AsNoTracking().AnyAsync(t => t.IdiomaId == idiomaId);
                if (jaMigrado)
                    return new { estado = "já migrado, ignorado" };

                var mapa = new Dictionary<int, int>();
                var topicosAntigos = await _db.CatecismoOtcTopicos.AsNoTracking().ToListAsync();
                foreach (var t in topicosAntigos)
                {
                    var novo = new CatecismoPtTopico { Titulo = t.Titulo, Slug = t.Slug, ParentId = null, IdiomaId = idiomaId };
                    _db.CatecismoPtTopicos.Add(novo);
                    await _db.SaveChangesAsync();
                    mapa[t.Id] = novo.Id;
                }

                var itensAntigos = await _db.CatecismosOtc.AsNoTracking().ToListAsync();
                foreach (var c in itensAntigos)
                {
                    _db.CatecismosPt.Add(new CatecismoPt
                    {
                        Titulo = c.Titulo,
                        Texto = c.Texto,
                        Slug = c.Slug,
                        CatecismoPtTopicoId = mapa[c.CatecismoOtcTopicoId],
                        IdiomaId = idiomaId
                    });
                }
                await _db.SaveChangesAsync();
                return new { topicos = topicosAntigos.Count, itens = itensAntigos.Count };
            }
        }

        /// <summary>Contagens rápidas para conferir a migração (por idioma).</summary>
        [HttpGet("contagens-idiomas")]
        public async Task<ActionResult> ContagensIdiomas()
        {
            var idiomas = await _db.Idiomas.AsNoTracking().OrderBy(i => i.Ordem).ToListAsync();
            var resultado = new List<object>();

            foreach (var idioma in idiomas)
            {
                resultado.Add(new
                {
                    idioma.Codigo,
                    idioma.Nome,
                    topicosCanticos = await _db.Topicos.CountAsync(t => t.IdiomaId == idioma.Id),
                    canticos = await _db.Canticos.CountAsync(c => c.IdiomaId == idioma.Id),
                    topicosCatecismo = await _db.CatecismoPtTopicos.CountAsync(t => t.IdiomaId == idioma.Id),
                    catecismoItens = await _db.CatecismosPt.CountAsync(c => c.IdiomaId == idioma.Id)
                });
            }

            var antigas = new
            {
                canticoUmbAntigo = await _db.CanticosUmb.CountAsync(),
                canticoLatAntigo = await _db.CanticosLat.CountAsync(),
                canticoKmbAntigo = await _db.CanticosKmb.CountAsync(),
                catecismoUbAntigo = await _db.CatecismosUb.CountAsync(),
                catecismoLatAntigo = await _db.CatecismosLat.CountAsync(),
                catecismoOtcAntigo = await _db.CatecismosOtc.CountAsync()
            };

            return Ok(new { novasTabelas = resultado, tabelasAntigas = antigas });
        }
    }
}
