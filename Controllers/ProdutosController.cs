using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MissaoBackend.Data;
using MissaoBackend.Models;

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

        // GET /api/produtos — público, só os disponíveis, para a loja da app
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Produto>>> GetDisponiveis()
        {
            var produtos = await _db.Produtos
                .AsNoTracking()
                .Where(p => p.Disponivel)
                .OrderBy(p => p.Ordem)
                .ThenBy(p => p.Nome)
                .ToListAsync();

            return Ok(produtos);
        }

        // GET /api/produtos/admin — protegido, todos (incluindo indisponíveis)
        [HttpGet("admin")]
        [Authorize]
        public async Task<ActionResult<IEnumerable<Produto>>> GetTodos()
        {
            var produtos = await _db.Produtos
                .AsNoTracking()
                .OrderBy(p => p.Ordem)
                .ThenBy(p => p.Nome)
                .ToListAsync();

            return Ok(produtos);
        }

        [HttpPost]
        [Authorize]
        public async Task<ActionResult<Produto>> Create(Produto input)
        {
            if (string.IsNullOrWhiteSpace(input.Nome))
                return BadRequest("O nome é obrigatório.");
            if (input.Preco < 0)
                return BadRequest("O preço não pode ser negativo.");

            var produto = new Produto
            {
                Nome = input.Nome.Trim(),
                Descricao = input.Descricao?.Trim(),
                Preco = input.Preco,
                ImagemUrl = input.ImagemUrl?.Trim(),
                Categoria = input.Categoria?.Trim(),
                Disponivel = input.Disponivel,
                Ordem = input.Ordem,
            };

            _db.Produtos.Add(produto);
            await _db.SaveChangesAsync();

            return CreatedAtAction(nameof(GetTodos), new { id = produto.Id }, produto);
        }

        [HttpPut("{id:int}")]
        [Authorize]
        public async Task<IActionResult> Update(int id, Produto input)
        {
            var existing = await _db.Produtos.FindAsync(id);
            if (existing == null) return NotFound();

            if (string.IsNullOrWhiteSpace(input.Nome))
                return BadRequest("O nome é obrigatório.");
            if (input.Preco < 0)
                return BadRequest("O preço não pode ser negativo.");

            existing.Nome = input.Nome.Trim();
            existing.Descricao = input.Descricao?.Trim();
            existing.Preco = input.Preco;
            existing.ImagemUrl = input.ImagemUrl?.Trim();
            existing.Categoria = input.Categoria?.Trim();
            existing.Disponivel = input.Disponivel;
            existing.Ordem = input.Ordem;

            await _db.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id:int}")]
        [Authorize]
        public async Task<IActionResult> Delete(int id)
        {
            var existing = await _db.Produtos.FindAsync(id);
            if (existing == null) return NotFound();

            var temEncomendas = await _db.ItensEncomenda.AnyAsync(i => i.ProdutoId == id);
            if (temEncomendas)
                return BadRequest("Não é possível eliminar: já existem encomendas com este produto. Torne-o indisponível em vez de eliminar.");

            _db.Produtos.Remove(existing);
            await _db.SaveChangesAsync();
            return NoContent();
        }
    }
}
