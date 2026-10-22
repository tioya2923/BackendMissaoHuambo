using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.EntityFrameworkCore;
using MissaoBackend.Data;
using MissaoBackend.Models;

namespace MissaoBackend.Controllers
{
    /// <summary>
    /// Gere os idiomas disponíveis na app. Criar aqui um idioma novo (ex: Suaíli, código
    /// "swa") faz com que ele apareça automaticamente no painel de administração com
    /// Cânticos e Catecismo/Orações próprios — sem precisar de código novo no backend.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class IdiomasController : ControllerBase
    {
        private readonly AppDbContext _db;

        public IdiomasController(AppDbContext db)
        {
            _db = db;
        }

        /// <summary>
        /// Idiomas que a app e o site mostram: só os activos que têm conteúdo. Com ?secao=canticos ou
        /// ?secao=catecismo, só os que têm conteúdo nessa secção (sem o parâmetro, em qualquer uma).
        /// </summary>
        [HttpGet]
        [OutputCache(PolicyName = "Leitura")]
        public async Task<ActionResult<IEnumerable<Idioma>>> GetAll([FromQuery] string? secao = null)
        {
            var comCanticos = await _db.Canticos.AsNoTracking().Select(c => c.IdiomaId).Distinct().ToListAsync();
            var comCatecismo = await _db.CatecismosPt.AsNoTracking().Select(c => c.IdiomaId).Distinct().ToListAsync();

            var s = (secao ?? string.Empty).Trim().ToLowerInvariant();
            var comConteudo = s switch
            {
                "canticos" => comCanticos,
                "catecismo" => comCatecismo,
                _ => comCanticos.Union(comCatecismo).ToList(),
            };

            return await _db.Idiomas.AsNoTracking()
                .Where(i => i.Ativo && comConteudo.Contains(i.Id))
                .OrderBy(i => i.Ordem).ThenBy(i => i.Nome)
                .ToListAsync();
        }

        /// <summary>Todos os idiomas (também os inactivos e os sem conteúdo): usado pelo painel de administração.</summary>
        [HttpGet("todos")]
        [Authorize(Policy = "Gestor")]
        public async Task<ActionResult<IEnumerable<Idioma>>> GetTodos()
        {
            return await _db.Idiomas.AsNoTracking().OrderBy(i => i.Ordem).ThenBy(i => i.Nome).ToListAsync();
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<Idioma>> GetById(int id)
        {
            var item = await _db.Idiomas.FindAsync(id);
            if (item == null) return NotFound();
            return item;
        }

        [HttpPost]
        [Authorize(Policy = "Gestor")]
        public async Task<ActionResult<Idioma>> Create(Idioma input)
        {
            if (string.IsNullOrWhiteSpace(input.Codigo) || string.IsNullOrWhiteSpace(input.Nome))
                return BadRequest("Código e nome são obrigatórios.");

            input.Codigo = input.Codigo.Trim().ToLowerInvariant();

            if (await _db.Idiomas.AnyAsync(i => i.Codigo == input.Codigo))
                return Conflict($"Já existe um idioma com o código '{input.Codigo}'.");

            _db.Idiomas.Add(input);
            await _db.SaveChangesAsync();
            return CreatedAtAction(nameof(GetById), new { id = input.Id }, input);
        }

        [HttpPut("{id:int}")]
        [Authorize(Policy = "Gestor")]
        public async Task<IActionResult> Update(int id, Idioma input)
        {
            var existing = await _db.Idiomas.FindAsync(id);
            if (existing == null) return NotFound();

            var novoCodigo = (input.Codigo ?? "").Trim().ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(novoCodigo) || string.IsNullOrWhiteSpace(input.Nome))
                return BadRequest("Código e nome são obrigatórios.");

            if (novoCodigo != existing.Codigo && await _db.Idiomas.AnyAsync(i => i.Codigo == novoCodigo && i.Id != id))
                return Conflict($"Já existe um idioma com o código '{novoCodigo}'.");

            existing.Codigo = novoCodigo;
            existing.Nome = input.Nome;
            existing.Ordem = input.Ordem;
            existing.Ativo = input.Ativo;

            await _db.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id:int}")]
        [Authorize(Policy = "Gestor")]
        public async Task<IActionResult> Delete(int id)
        {
            var existing = await _db.Idiomas.FindAsync(id);
            if (existing == null) return NotFound();

            var temConteudo =
                await _db.Topicos.AnyAsync(t => t.IdiomaId == id) ||
                await _db.Canticos.AnyAsync(c => c.IdiomaId == id) ||
                await _db.CatecismoPtTopicos.AnyAsync(t => t.IdiomaId == id) ||
                await _db.CatecismosPt.AnyAsync(c => c.IdiomaId == id);

            if (temConteudo)
                return BadRequest("Não é possível eliminar: este idioma ainda tem conteúdo associado (cânticos, tópicos ou catecismo/orações). Desative-o em vez de eliminar, ou remova primeiro todo o conteúdo.");

            _db.Idiomas.Remove(existing);
            await _db.SaveChangesAsync();
            return NoContent();
        }
    }
}
