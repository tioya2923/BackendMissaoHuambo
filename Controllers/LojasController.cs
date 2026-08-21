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
    public class LojasController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly IConfiguration _config;

        public LojasController(AppDbContext db, IConfiguration config)
        {
            _db = db;
            _config = config;
        }

        private int LojaIdAtual =>
            int.Parse(User.FindFirstValue("lojaId") ?? throw new InvalidOperationException("Token sem lojaId."));

        // Projeção pública — nunca inclui a password
        private static object ParaPublico(Loja l) => new
        {
            l.Id,
            l.Nome,
            l.Descricao,
            l.Telefone,
            l.Morada,
            l.Categoria,
            l.Latitude,
            l.Longitude,
        };

        public record FormaPagamentoInput(string Metodo, string? Detalhe);
        private static object ParaFormaPagamento(FormaPagamentoLoja f) => new { f.Metodo, f.Detalhe };

        // ── Registo e sessão ─────────────────────────────────────────────────

        public record RegistoLojaRequest(
            string Nome, string Email, string Password,
            string? Telefone, string? Morada, string? Categoria, string? Descricao,
            double Latitude, double Longitude, string? Moeda);

        public record LojaLoginRequest(string Email, string Password);
        public record LojaLoginResponse(string Token, int LojaId, string Nome, bool Aprovada);

        [HttpPost("registar")]
        public async Task<IActionResult> Registar(RegistoLojaRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.Nome) || string.IsNullOrWhiteSpace(req.Email) || string.IsNullOrWhiteSpace(req.Password))
                return BadRequest("Nome, email e password são obrigatórios.");
            if (req.Password.Length < 6)
                return BadRequest("A password tem de ter pelo menos 6 caracteres.");
            if (await _db.Lojas.AnyAsync(l => l.Email == req.Email))
                return BadRequest("Já existe uma loja registada com este email.");
            if (req.Moeda != null && !Moeda.EhValida(req.Moeda))
                return BadRequest($"Moeda inválida. Use uma de: {string.Join(", ", Moeda.Todas)}.");

            var loja = new Loja
            {
                Nome = req.Nome.Trim(),
                Email = req.Email.Trim(),
                Password = PasswordHasher.Hash(req.Password),
                Telefone = req.Telefone?.Trim(),
                Morada = req.Morada?.Trim(),
                Categoria = req.Categoria?.Trim(),
                Descricao = req.Descricao?.Trim(),
                Latitude = req.Latitude,
                Longitude = req.Longitude,
                Moeda = req.Moeda ?? Moeda.AOA,
                Aprovada = false,
                Ativa = true,
            };

            _db.Lojas.Add(loja);
            await _db.SaveChangesAsync();

            var token = EmitirToken(loja);
            return Ok(new LojaLoginResponse(token, loja.Id, loja.Nome, loja.Aprovada));
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LojaLoginRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.Email) || string.IsNullOrWhiteSpace(req.Password))
                return BadRequest("Email e password são obrigatórios.");

            var loja = await _db.Lojas.FirstOrDefaultAsync(l => l.Email == req.Email);
            if (loja == null || !PasswordHasher.Verify(req.Password, loja.Password))
                return Unauthorized("Credenciais inválidas.");

            var token = EmitirToken(loja);
            return Ok(new LojaLoginResponse(token, loja.Id, loja.Nome, loja.Aprovada));
        }

        private string EmitirToken(Loja loja)
        {
            var claims = new[]
            {
                new Claim(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub, loja.Email),
                new Claim("lojaId", loja.Id.ToString()),
                new Claim("tipo", "loja"),
            };
            return JwtTokenService.Criar(_config, claims, TimeSpan.FromHours(8));
        }

        // ── Público: descoberta de lojas ─────────────────────────────────────

        // GET /api/lojas — lojas aprovadas e ativas; se lat/lng forem dados, vêm ordenadas por distância
        [HttpGet]
        public async Task<IActionResult> GetPublicas([FromQuery] double? lat, [FromQuery] double? lng)
        {
            var lojas = await _db.Lojas
                .AsNoTracking()
                .Include(l => l.FormasPagamento)
                .Where(l => l.Aprovada && l.Ativa)
                .ToListAsync();

            var resultado = lojas.Select(l => new
            {
                l.Id,
                l.Nome,
                l.Descricao,
                l.Categoria,
                l.Morada,
                l.Latitude,
                l.Longitude,
                l.Moeda,
                FormasPagamento = l.FormasPagamento.Select(ParaFormaPagamento),
                DistanciaKm = (lat.HasValue && lng.HasValue)
                    ? Math.Round(Utils.GeoHelper.DistanciaKm(lat.Value, lng.Value, l.Latitude, l.Longitude), 1)
                    : (double?)null,
            });

            if (lat.HasValue && lng.HasValue)
                resultado = resultado.OrderBy(l => l.DistanciaKm);
            else
                resultado = resultado.OrderBy(l => l.Nome);

            return Ok(resultado);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetPublica(int id, [FromQuery] double? lat, [FromQuery] double? lng)
        {
            var loja = await _db.Lojas
                .AsNoTracking()
                .Include(l => l.FormasPagamento)
                .FirstOrDefaultAsync(l => l.Id == id && l.Aprovada && l.Ativa);
            if (loja == null) return NotFound();

            return Ok(new
            {
                loja.Id,
                loja.Nome,
                loja.Descricao,
                loja.Categoria,
                loja.Morada,
                loja.Telefone,
                loja.Latitude,
                loja.Longitude,
                loja.Moeda,
                loja.InfoPagamento,
                FormasPagamento = loja.FormasPagamento.Select(ParaFormaPagamento),
                DistanciaKm = (lat.HasValue && lng.HasValue)
                    ? Math.Round(Utils.GeoHelper.DistanciaKm(lat.Value, lng.Value, loja.Latitude, loja.Longitude), 1)
                    : (double?)null,
            });
        }

        // ── Área da própria loja ─────────────────────────────────────────────

        public record AtualizarLojaRequest(
            string Nome, string? Descricao, string? Telefone, string? Morada,
            string? Categoria, string? InfoPagamento, double Latitude, double Longitude,
            List<FormaPagamentoInput>? FormasPagamento, string? Moeda);

        [HttpGet("eu")]
        [Authorize(Policy = "Loja")]
        public async Task<IActionResult> GetPerfilProprio()
        {
            var loja = await _db.Lojas
                .AsNoTracking()
                .Include(l => l.FormasPagamento)
                .FirstOrDefaultAsync(l => l.Id == LojaIdAtual);
            if (loja == null) return NotFound();

            return Ok(new
            {
                loja.Id,
                loja.Nome,
                loja.Email,
                loja.Descricao,
                loja.Telefone,
                loja.Morada,
                loja.Categoria,
                loja.InfoPagamento,
                FormasPagamento = loja.FormasPagamento.Select(ParaFormaPagamento),
                loja.Latitude,
                loja.Longitude,
                loja.Moeda,
                loja.Aprovada,
                loja.Ativa,
                loja.PercentualComissao,
            });
        }

        [HttpPut("eu")]
        [Authorize(Policy = "Loja")]
        public async Task<IActionResult> AtualizarPerfilProprio(AtualizarLojaRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.Nome))
                return BadRequest("O nome é obrigatório.");
            if (req.Moeda != null && !Moeda.EhValida(req.Moeda))
                return BadRequest($"Moeda inválida. Use uma de: {string.Join(", ", Moeda.Todas)}.");

            if (req.FormasPagamento != null)
            {
                var invalidos = req.FormasPagamento.Where(f => !MetodoPagamento.EhValido(f.Metodo)).ToList();
                if (invalidos.Count > 0)
                    return BadRequest($"Método de pagamento inválido: {string.Join(", ", invalidos.Select(f => f.Metodo))}.");
            }

            var loja = await _db.Lojas
                .Include(l => l.FormasPagamento)
                .FirstOrDefaultAsync(l => l.Id == LojaIdAtual);
            if (loja == null) return NotFound();

            loja.Nome = req.Nome.Trim();
            loja.Descricao = req.Descricao?.Trim();
            loja.Telefone = req.Telefone?.Trim();
            loja.Morada = req.Morada?.Trim();
            loja.Categoria = req.Categoria?.Trim();
            loja.InfoPagamento = req.InfoPagamento?.Trim();
            loja.Latitude = req.Latitude;
            loja.Longitude = req.Longitude;
            if (req.Moeda != null) loja.Moeda = req.Moeda;

            if (req.FormasPagamento != null)
            {
                _db.FormasPagamentoLoja.RemoveRange(loja.FormasPagamento);
                loja.FormasPagamento = req.FormasPagamento
                    .Select(f => new FormaPagamentoLoja { Metodo = f.Metodo, Detalhe = f.Detalhe?.Trim() })
                    .ToList();
            }

            await _db.SaveChangesAsync();
            return NoContent();
        }

        [HttpPut("eu/pausar")]
        [Authorize(Policy = "Loja")]
        public async Task<IActionResult> PausarOuReativar([FromBody] bool ativa)
        {
            var loja = await _db.Lojas.FindAsync(LojaIdAtual);
            if (loja == null) return NotFound();
            loja.Ativa = ativa;
            await _db.SaveChangesAsync();
            return NoContent();
        }

        // ── Moderação pelo Gestor ────────────────────────────────────────────

        [HttpGet("admin")]
        [Authorize(Policy = "Gestor")]
        public async Task<IActionResult> GetTodas()
        {
            var lojas = await _db.Lojas
                .AsNoTracking()
                .Include(l => l.FormasPagamento)
                .OrderByDescending(l => l.DataRegisto)
                .ToListAsync();
            return Ok(lojas.Select(l => new
            {
                l.Id,
                l.Nome,
                l.Email,
                l.Telefone,
                l.Morada,
                l.Categoria,
                l.Latitude,
                l.Longitude,
                l.Moeda,
                l.Aprovada,
                l.Ativa,
                l.PercentualComissao,
                FormasPagamento = l.FormasPagamento.Select(ParaFormaPagamento),
                l.DataRegisto,
            }));
        }

        public record ModerarLojaRequest(bool Aprovada, bool Ativa, decimal? PercentualComissao);

        [HttpPut("{id:int}/moderar")]
        [Authorize(Policy = "Gestor")]
        public async Task<IActionResult> Moderar(int id, ModerarLojaRequest req)
        {
            if (req.PercentualComissao is < 0 or > 100)
                return BadRequest("A percentagem de comissão tem de estar entre 0 e 100.");

            var loja = await _db.Lojas.FindAsync(id);
            if (loja == null) return NotFound();

            loja.Aprovada = req.Aprovada;
            loja.Ativa = req.Ativa;
            if (req.PercentualComissao.HasValue)
                loja.PercentualComissao = req.PercentualComissao.Value;
            await _db.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id:int}")]
        [Authorize(Policy = "Gestor")]
        public async Task<IActionResult> Eliminar(int id)
        {
            var loja = await _db.Lojas.FindAsync(id);
            if (loja == null) return NotFound();

            var temProdutos = await _db.Produtos.AnyAsync(p => p.LojaId == id);
            if (temProdutos)
                return BadRequest("Não é possível eliminar: a loja ainda tem produtos. Desative-a em vez de eliminar.");

            _db.Lojas.Remove(loja);
            await _db.SaveChangesAsync();
            return NoContent();
        }
    }
}
