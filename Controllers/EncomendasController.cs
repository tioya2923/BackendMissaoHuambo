using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MissaoBackend.Data;
using MissaoBackend.Models;
using MissaoBackend.Services;

namespace MissaoBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EncomendasController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly IConfiguration _config;

        public EncomendasController(AppDbContext db, IConfiguration config)
        {
            _db = db;
            _config = config;
        }

        private int LojaIdAtual =>
            int.Parse(User.FindFirstValue("lojaId") ?? throw new InvalidOperationException("Token sem lojaId."));

        public record ItemPedido(int ProdutoId, int Quantidade);
        public record NovaEncomenda(
            string NomeCliente,
            string Contacto,
            string? Morada,
            string? Observacoes,
            List<ItemPedido> Itens
        );

        // Resposta ao comprador: nunca inclui a comissão — é um detalhe interno
        // entre a plataforma e a loja, sem relevância para quem compra.
        private static object ParaResposta(Encomenda e) => new
        {
            e.Id,
            e.GrupoId,
            e.LojaId,
            LojaNome = e.Loja?.Nome,
            LojaTelefone = e.Loja?.Telefone,
            InfoPagamento = e.Loja?.InfoPagamento,
            FormasPagamento = e.Loja?.FormasPagamento.Select(f => new { f.Metodo, f.Detalhe }) ?? Enumerable.Empty<object>(),
            e.Total,
            e.Moeda,
            e.Estado,
            Itens = e.Itens.Select(i => new { i.ProdutoNome, i.PrecoUnitario, i.Quantidade }),
        };

        // Resposta para Gestor/Loja: inclui a comissão e o valor líquido que fica
        // para a loja, para permitir o acerto de contas.
        private static object ParaRespostaComComissao(Encomenda e) => new
        {
            e.Id,
            e.GrupoId,
            e.LojaId,
            LojaNome = e.Loja?.Nome,
            e.Data,
            e.NomeCliente,
            e.Contacto,
            e.Morada,
            e.Observacoes,
            e.Estado,
            e.Total,
            e.Moeda,
            e.PercentualComissaoAplicado,
            e.ValorComissao,
            ValorLiquido = e.Total - e.ValorComissao,
            Itens = e.Itens.Select(i => new { i.ProdutoNome, i.PrecoUnitario, i.Quantidade }),
        };

        // POST /api/encomendas — público: o comprador finaliza o carrinho, que pode ter
        // artigos de várias lojas. O servidor agrupa por loja e cria uma Encomenda por
        // cada uma, todas ligadas pelo mesmo GrupoId (uma única "compra" do ponto de
        // vista do comprador, mas cada loja só vê e gere a sua parte).
        [HttpPost]
        public async Task<IActionResult> Create(NovaEncomenda input)
        {
            if (string.IsNullOrWhiteSpace(input.NomeCliente) || string.IsNullOrWhiteSpace(input.Contacto))
                return BadRequest("Nome e contacto são obrigatórios.");
            if (input.Itens == null || input.Itens.Count == 0)
                return BadRequest("A encomenda tem de ter pelo menos um artigo.");
            if (input.Itens.Any(i => i.Quantidade <= 0))
                return BadRequest("A quantidade de cada artigo tem de ser maior que zero.");

            var produtoIds = input.Itens.Select(i => i.ProdutoId).ToList();
            var produtos = await _db.Produtos
                .Include(p => p.Loja)
                .Where(p => produtoIds.Contains(p.Id))
                .ToListAsync();

            foreach (var linha in input.Itens)
            {
                var produto = produtos.FirstOrDefault(p => p.Id == linha.ProdutoId);
                if (produto == null || !produto.Disponivel || produto.Loja == null || !produto.Loja.Aprovada || !produto.Loja.Ativa)
                    return BadRequest($"O artigo com id {linha.ProdutoId} já não está disponível.");
            }

            var grupoId = Guid.NewGuid();
            var encomendasCriadas = new List<Encomenda>();

            foreach (var grupoLoja in input.Itens.GroupBy(i => produtos.First(p => p.Id == i.ProdutoId).LojaId))
            {
                decimal total = 0;
                var itensLoja = new List<ItemEncomenda>();

                foreach (var linha in grupoLoja)
                {
                    var produto = produtos.First(p => p.Id == linha.ProdutoId);
                    var precoEfetivo = produto.PrecoPromocional ?? produto.Preco;
                    var item = new ItemEncomenda
                    {
                        ProdutoId = produto.Id,
                        ProdutoNome = produto.Nome,
                        PrecoUnitario = precoEfetivo,
                        Quantidade = linha.Quantidade,
                    };
                    total += item.PrecoUnitario * item.Quantidade;
                    itensLoja.Add(item);
                }

                // A Ndatava não cobra comissão às lojas — a manutenção do serviço é sustentada
                // por doações voluntárias (ver LembreteApoioService), nunca por uma percentagem
                // das vendas. Os campos ficam a 0 para manter o histórico/esquema compatível.
                const decimal percentualComissao = 0m;
                const decimal valorComissao = 0m;

                // A moeda é fixada a partir da loja no momento da compra, para o histórico
                // não mudar mesmo que a loja altere depois a moeda em que vende.
                var moedaLoja = produtos.First(p => p.LojaId == grupoLoja.Key).Loja!.Moeda;

                var encomenda = new Encomenda
                {
                    NomeCliente = input.NomeCliente.Trim(),
                    Contacto = input.Contacto.Trim(),
                    Morada = input.Morada?.Trim(),
                    Observacoes = input.Observacoes?.Trim(),
                    Estado = EstadoEncomenda.Pendente,
                    Total = total,
                    Moeda = moedaLoja,
                    PercentualComissaoAplicado = percentualComissao,
                    ValorComissao = valorComissao,
                    LojaId = grupoLoja.Key,
                    GrupoId = grupoId,
                    Itens = itensLoja,
                };

                _db.Encomendas.Add(encomenda);
                encomendasCriadas.Add(encomenda);
            }

            await _db.SaveChangesAsync();

            // Recarrega com a loja incluída, para devolver o nome e as formas de pagamento
            foreach (var enc in encomendasCriadas)
            {
                await _db.Entry(enc).Reference(e => e.Loja).LoadAsync();
                if (enc.Loja != null)
                    await _db.Entry(enc.Loja).Collection(l => l.FormasPagamento).LoadAsync();
            }

            // Avisa cada loja por email de que tem uma encomenda nova — não há
            // notificação push nem SMS, por isso isto é o único aviso automático
            // que a loja recebe. Uma falha no envio nunca impede a encomenda de
            // ser criada (EmailService trata e regista o erro internamente).
            foreach (var enc in encomendasCriadas)
            {
                if (enc.Loja == null || string.IsNullOrWhiteSpace(enc.Loja.Email)) continue;
                await EmailService.EnviarAsync(_config, enc.Loja.Email, $"Nova encomenda #{enc.Id} — {enc.Loja.Nome}", MontarEmailNovaEncomenda(enc));
            }

            return Ok(encomendasCriadas.Select(ParaResposta));
        }

        private static string MontarEmailNovaEncomenda(Encomenda enc)
        {
            var itensHtml = string.Join("", enc.Itens.Select(i =>
                $"<tr><td style='padding:4px 8px;'>{i.Quantidade}× {i.ProdutoNome}</td>" +
                $"<td style='padding:4px 8px;text-align:right;'>{Moeda.Formatar(i.PrecoUnitario * i.Quantidade, enc.Moeda)}</td></tr>"));

            return $@"
                <div style='font-family:sans-serif;color:#222;'>
                    <h2 style='margin-bottom:4px;'>Nova encomenda recebida</h2>
                    <p style='color:#666;'>Referência #{enc.Id} · {enc.Data:dd/MM/yyyy HH:mm}</p>
                    <p><strong>Cliente:</strong> {enc.NomeCliente}<br/>
                       <strong>Contacto:</strong> {enc.Contacto}
                       {(string.IsNullOrWhiteSpace(enc.Morada) ? "" : $"<br/><strong>Morada:</strong> {enc.Morada}")}
                       {(string.IsNullOrWhiteSpace(enc.Observacoes) ? "" : $"<br/><strong>Observações:</strong> {enc.Observacoes}")}
                    </p>
                    <table style='border-collapse:collapse;width:100%;max-width:400px;margin-top:12px;'>
                        {itensHtml}
                        <tr><td style='padding:8px;font-weight:bold;border-top:1px solid #ddd;'>Total</td>
                            <td style='padding:8px;font-weight:bold;text-align:right;border-top:1px solid #ddd;'>{Moeda.Formatar(enc.Total, enc.Moeda)}</td></tr>
                    </table>
                    <p style='color:#666;margin-top:16px;font-size:13px;'>
                        Entre no seu painel em Ndatava (área da loja) para confirmar ou atualizar o estado desta encomenda.
                    </p>
                </div>";
        }

        // GET /api/encomendas — protegido (Gestor): todas as encomendas, de todas as lojas
        [HttpGet]
        [Authorize(Policy = "Gestor")]
        public async Task<IActionResult> GetAll()
        {
            var encomendas = await _db.Encomendas
                .Include(e => e.Itens)
                .Include(e => e.Loja)
                .AsNoTracking()
                .OrderByDescending(e => e.Data)
                .ToListAsync();

            return Ok(encomendas.Select(ParaRespostaComComissao));
        }

        // GET /api/encomendas/comissoes — protegido (Gestor): total de comissão a
        // receber de cada loja, para servir de base ao acerto de contas periódico
        // (feito fora da app, já que os pagamentos são diretos entre comprador e loja).
        [HttpGet("comissoes")]
        [Authorize(Policy = "Gestor")]
        public async Task<IActionResult> GetResumoComissoes()
        {
            var resumo = await _db.Encomendas
                .Include(e => e.Loja)
                .AsNoTracking()
                .Where(e => e.Estado != EstadoEncomenda.Cancelada)
                .GroupBy(e => new { e.LojaId, LojaNome = e.Loja!.Nome, e.Moeda })
                .Select(g => new
                {
                    g.Key.LojaId,
                    g.Key.LojaNome,
                    g.Key.Moeda,
                    NumeroEncomendas = g.Count(),
                    TotalVendido = g.Sum(e => e.Total),
                    TotalComissao = g.Sum(e => e.ValorComissao),
                })
                .OrderByDescending(r => r.TotalComissao)
                .ToListAsync();

            return Ok(new
            {
                Lojas = resumo,
                TotalGeralComissao = resumo.Sum(r => r.TotalComissao),
            });
        }

        // GET /api/encomendas/minha-loja — protegido (Loja): só as encomendas da própria loja
        [HttpGet("minha-loja")]
        [Authorize(Policy = "Loja")]
        public async Task<IActionResult> GetMinhaLoja()
        {
            var encomendas = await _db.Encomendas
                .Include(e => e.Itens)
                .Where(e => e.LojaId == LojaIdAtual)
                .AsNoTracking()
                .OrderByDescending(e => e.Data)
                .ToListAsync();

            return Ok(encomendas.Select(e => new
            {
                e.Id,
                e.Data,
                e.NomeCliente,
                e.Contacto,
                e.Morada,
                e.Observacoes,
                e.Estado,
                e.Total,
                e.Moeda,
                e.PercentualComissaoAplicado,
                e.ValorComissao,
                ValorLiquido = e.Total - e.ValorComissao,
                Itens = e.Itens.Select(i => new { i.ProdutoNome, i.PrecoUnitario, i.Quantidade }),
            }));
        }

        public record AtualizarEstado(string Estado);

        // PUT /api/encomendas/{id}/estado — o Gestor pode mudar qualquer encomenda;
        // uma Loja só pode mudar as suas próprias.
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

            var ehGestor = User.HasClaim("tipo", "gestor");
            var ehLojaDona = User.HasClaim("tipo", "loja") && encomenda.LojaId == LojaIdAtual;
            if (!ehGestor && !ehLojaDona) return Forbid();

            encomenda.Estado = input.Estado;
            await _db.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id:int}")]
        [Authorize(Policy = "Gestor")]
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
