using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MissaoBackend.Data;
using MissaoBackend.Models;
using MissaoBackend.Utils;

namespace MissaoBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProdutosController : ControllerBase
    {
        private readonly AppDbContext _db;

        public ProdutosController(AppDbContext db)
        {
            _db = db;
        }

        private int LojaIdAtual =>
            int.Parse(User.FindFirstValue("lojaId") ?? throw new InvalidOperationException("Token sem lojaId."));

        // GET /api/produtos?q=&lat=&lng= — público: artigos disponíveis de todas as lojas
        // aprovadas e ativas. Com lat/lng, cada resultado traz a distância à loja e vem
        // ordenado do mais próximo; sem lat/lng, ordena por nome.
        [HttpGet]
        public async Task<IActionResult> GetDisponiveis([FromQuery] string? q, [FromQuery] double? lat, [FromQuery] double? lng)
        {
            var query = _db.Produtos
                .AsNoTracking()
                .Include(p => p.Loja)
                .Where(p => p.Disponivel && p.Loja != null && p.Loja.Aprovada && p.Loja.Ativa);

            if (!string.IsNullOrWhiteSpace(q))
            {
                var termo = q.Trim();
                query = query.Where(p => EF.Functions.Like(p.Nome, $"%{termo}%"));
            }

            var produtos = await query.ToListAsync();

            var resultado = produtos.Select(p => new
            {
                p.Id,
                p.Nome,
                p.Descricao,
                p.Preco,
                p.PrecoPromocional,
                p.EmDestaque,
                p.ImagemUrl,
                p.Categoria,
                Loja = new
                {
                    p.Loja!.Id,
                    p.Loja.Nome,
                    p.Loja.Morada,
                    p.Loja.Latitude,
                    p.Loja.Longitude,
                    p.Loja.Moeda,
                },
                DistanciaKm = (lat.HasValue && lng.HasValue)
                    ? Math.Round(GeoHelper.DistanciaKm(lat.Value, lng.Value, p.Loja.Latitude, p.Loja.Longitude), 1)
                    : (double?)null,
            });

            resultado = (lat.HasValue && lng.HasValue)
                ? resultado.OrderBy(p => p.DistanciaKm)
                : resultado.OrderBy(p => p.Nome);

            return Ok(resultado);
        }

        // GET /api/produtos/loja/{lojaId} — público: catálogo completo de uma loja
        [HttpGet("loja/{lojaId:int}")]
        public async Task<IActionResult> GetPorLoja(int lojaId)
        {
            var loja = await _db.Lojas.AsNoTracking().FirstOrDefaultAsync(l => l.Id == lojaId && l.Aprovada && l.Ativa);
            if (loja == null) return NotFound();

            var produtos = await _db.Produtos
                .AsNoTracking()
                .Where(p => p.LojaId == lojaId && p.Disponivel)
                .OrderBy(p => p.Ordem).ThenBy(p => p.Nome)
                .Select(p => new { p.Id, p.Nome, p.Descricao, p.Preco, p.PrecoPromocional, p.EmDestaque, p.ImagemUrl, p.Categoria })
                .ToListAsync();

            return Ok(produtos);
        }

        // GET /api/produtos/minha — protegido (Loja): todos os produtos da própria loja
        [HttpGet("minha")]
        [Authorize(Policy = "Loja")]
        public async Task<IActionResult> GetMeusProdutos()
        {
            var produtos = await _db.Produtos
                .AsNoTracking()
                .Where(p => p.LojaId == LojaIdAtual)
                .OrderBy(p => p.Ordem).ThenBy(p => p.Nome)
                .ToListAsync();

            return Ok(produtos);
        }

        [HttpPost]
        [Authorize(Policy = "Loja")]
        public async Task<ActionResult<Produto>> Create(Produto input)
        {
            if (string.IsNullOrWhiteSpace(input.Nome))
                return BadRequest("O nome é obrigatório.");
            if (input.Preco < 0)
                return BadRequest("O preço não pode ser negativo.");
            if (input.PrecoPromocional is < 0)
                return BadRequest("O preço promocional não pode ser negativo.");
            if (input.PrecoPromocional.HasValue && input.PrecoPromocional >= input.Preco)
                return BadRequest("O preço promocional tem de ser menor que o preço normal.");

            var produto = new Produto
            {
                Nome = input.Nome.Trim(),
                Descricao = input.Descricao?.Trim(),
                Preco = input.Preco,
                PrecoPromocional = input.PrecoPromocional,
                EmDestaque = input.EmDestaque,
                ImagemUrl = input.ImagemUrl?.Trim(),
                Categoria = input.Categoria?.Trim(),
                Disponivel = input.Disponivel,
                Ordem = input.Ordem,
                LojaId = LojaIdAtual,
            };

            _db.Produtos.Add(produto);
            await _db.SaveChangesAsync();

            return CreatedAtAction(nameof(GetMeusProdutos), produto);
        }

        [HttpPut("{id:int}")]
        [Authorize(Policy = "Loja")]
        public async Task<IActionResult> Update(int id, Produto input)
        {
            var existing = await _db.Produtos.FindAsync(id);
            if (existing == null) return NotFound();
            if (existing.LojaId != LojaIdAtual) return Forbid();

            if (string.IsNullOrWhiteSpace(input.Nome))
                return BadRequest("O nome é obrigatório.");
            if (input.Preco < 0)
                return BadRequest("O preço não pode ser negativo.");
            if (input.PrecoPromocional is < 0)
                return BadRequest("O preço promocional não pode ser negativo.");
            if (input.PrecoPromocional.HasValue && input.PrecoPromocional >= input.Preco)
                return BadRequest("O preço promocional tem de ser menor que o preço normal.");

            existing.Nome = input.Nome.Trim();
            existing.Descricao = input.Descricao?.Trim();
            existing.Preco = input.Preco;
            existing.PrecoPromocional = input.PrecoPromocional;
            existing.EmDestaque = input.EmDestaque;
            existing.ImagemUrl = input.ImagemUrl?.Trim();
            existing.Categoria = input.Categoria?.Trim();
            existing.Disponivel = input.Disponivel;
            existing.Ordem = input.Ordem;

            await _db.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id:int}")]
        [Authorize(Policy = "Loja")]
        public async Task<IActionResult> Delete(int id)
        {
            var existing = await _db.Produtos.FindAsync(id);
            if (existing == null) return NotFound();
            if (existing.LojaId != LojaIdAtual) return Forbid();

            var temEncomendas = await _db.ItensEncomenda.AnyAsync(i => i.ProdutoId == id);
            if (temEncomendas)
                return BadRequest("Não é possível eliminar: já existem encomendas com este produto. Torne-o indisponível em vez de eliminar.");

            _db.Produtos.Remove(existing);
            await _db.SaveChangesAsync();
            return NoContent();
        }

        // GET /api/produtos/admin — protegido (Gestor): visão geral de todos os produtos, de todas as lojas
        [HttpGet("admin")]
        [Authorize(Policy = "Gestor")]
        public async Task<IActionResult> GetTodosParaGestor()
        {
            var produtos = await _db.Produtos
                .AsNoTracking()
                .Include(p => p.Loja)
                .OrderByDescending(p => p.Id)
                .Select(p => new
                {
                    p.Id,
                    p.Nome,
                    p.Preco,
                    p.Disponivel,
                    Loja = p.Loja == null ? null : new { p.Loja.Id, p.Loja.Nome },
                })
                .ToListAsync();

            return Ok(produtos);
        }
    }
}
