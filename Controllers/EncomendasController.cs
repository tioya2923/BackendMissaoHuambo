using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MissaoBackend.Data;
using MissaoBackend.Models;

namespace MissaoBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EncomendasController : ControllerBase
    {
        private readonly AppDbContext _db;

        public EncomendasController(AppDbContext db)
        {
            _db = db;
        }

        public record ItemPedido(int ProdutoId, int Quantidade);
        public record NovaEncomenda(
            string NomeCliente,
            string Contacto,
            string? Morada,
            string? Observacoes,
            List<ItemPedido> Itens
        );

        // POST /api/encomendas — público: o cliente faz o pedido a partir da loja da app
        [HttpPost]
        public async Task<ActionResult<Encomenda>> Create(NovaEncomenda input)
        {
            if (string.IsNullOrWhiteSpace(input.NomeCliente) || string.IsNullOrWhiteSpace(input.Contacto))
                return BadRequest("Nome e contacto são obrigatórios.");

            if (input.Itens == null || input.Itens.Count == 0)
                return BadRequest("A encomenda tem de ter pelo menos um artigo.");

            var itensValidos = new List<ItemEncomenda>();
            decimal total = 0;

            foreach (var linha in input.Itens)
            {
                if (linha.Quantidade <= 0)
                    return BadRequest("A quantidade de cada artigo tem de ser maior que zero.");

                var produto = await _db.Produtos.FindAsync(linha.ProdutoId);
                if (produto == null || !produto.Disponivel)
                    return BadRequest($"O artigo com id {linha.ProdutoId} já não está disponível.");

                var item = new ItemEncomenda
                {
                    ProdutoId = produto.Id,
                    ProdutoNome = produto.Nome,
                    PrecoUnitario = produto.Preco,
                    Quantidade = linha.Quantidade,
                };
                total += item.PrecoUnitario * item.Quantidade;
                itensValidos.Add(item);
            }

            var encomenda = new Encomenda
            {
                NomeCliente = input.NomeCliente.Trim(),
                Contacto = input.Contacto.Trim(),
                Morada = input.Morada?.Trim(),
                Observacoes = input.Observacoes?.Trim(),
                Estado = EstadoEncomenda.Pendente,
                Total = total,
                Itens = itensValidos,
            };

            _db.Encomendas.Add(encomenda);
            await _db.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = encomenda.Id }, encomenda);
        }

        // GET /api/encomendas — protegido: lista para o painel de administração
        [HttpGet]
        [Authorize]
        public async Task<ActionResult<IEnumerable<Encomenda>>> GetAll()
        {
            var encomendas = await _db.Encomendas
                .Include(e => e.Itens)
                .AsNoTracking()
                .OrderByDescending(e => e.Data)
                .ToListAsync();

            return Ok(encomendas);
        }

        [HttpGet("{id:int}")]
        [Authorize]
        public async Task<ActionResult<Encomenda>> GetById(int id)
        {
            var encomenda = await _db.Encomendas
                .Include(e => e.Itens)
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.Id == id);

            if (encomenda == null) return NotFound();
            return Ok(encomenda);
        }

        public record AtualizarEstado(string Estado);

        [HttpPut("{id:int}/estado")]
        [Authorize]
        public async Task<IActionResult> AtualizarEstadoEncomenda(int id, AtualizarEstado input)
        {
            var validos = new[] {
                EstadoEncomenda.Pendente, EstadoEncomenda.Confirmada,
                EstadoEncomenda.Enviada, EstadoEncomenda.Cancelada,
            };
            if (!validos.Contains(input.Estado))
                return BadRequest($"Estado inválido. Use um de: {string.Join(", ", validos)}.");

            var encomenda = await _db.Encomendas.FindAsync(id);
            if (encomenda == null) return NotFound();

            encomenda.Estado = input.Estado;
            await _db.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id:int}")]
        [Authorize]
        public async Task<IActionResult> Delete(int id)
        {
            var encomenda = await _db.Encomendas.FindAsync(id);
            if (encomenda == null) return NotFound();

            _db.Encomendas.Remove(encomenda);
            await _db.SaveChangesAsync();
            return NoContent();
        }
    }
}
