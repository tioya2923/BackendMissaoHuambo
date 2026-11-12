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
        private readonly ArmazenamentoService _armazenamento;

        public LojasController(AppDbContext db, IConfiguration config, ArmazenamentoService armazenamento)
        {
            _db = db;
            _config = config;
            _armazenamento = armazenamento;
        }

        private const int MinPassword = 8;

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
            var emailLoja = req.Email.Trim();
            if (!emailLoja.Contains('@') || emailLoja.Length > 200)
                return BadRequest("Indique um email válido.");
            if (req.Nome.Trim().Length > 150)
                return BadRequest("O nome da loja é demasiado longo (máximo 150 caracteres).");
            if (req.Password.Length < MinPassword)
                return BadRequest($"A password tem de ter pelo menos {MinPassword} caracteres.");
            if (req.Password.Length > 100)
                return BadRequest("A password é demasiado longa (máximo 100 caracteres).");
            if (!CoordenadasValidas(req.Latitude, req.Longitude))
                return BadRequest("A localização indicada é inválida.");
            if (!RegistosPorEndereco.Permitir(OrigemDoPedido.Endereco(HttpContext)))
                return StatusCode(429, "Foram criados demasiados registos a partir desta ligação. Tente mais tarde.");
            if (await _db.Lojas.AnyAsync(l => l.Email == emailLoja))
                return BadRequest("Já existe uma loja registada com este email.");
            if (req.Moeda != null && !Moeda.EhValida(req.Moeda))
                return BadRequest($"Moeda inválida. Use uma de: {string.Join(", ", Moeda.Todas)}.");

            var loja = new Loja
            {
                Nome = req.Nome.Trim(),
                Email = emailLoja,
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

        // Travam adivinhação de palavras-passe (por email) e criação massiva de lojas (por endereço).
        private static readonly ProtecaoLogin FalhasDeLogin = new();
        private static readonly LimitadorTentativas RegistosPorEndereco = new(10, TimeSpan.FromHours(1));

        private static bool CoordenadasValidas(double lat, double lng) =>
            !double.IsNaN(lat) && !double.IsNaN(lng) && lat is >= -90 and <= 90 && lng is >= -180 and <= 180;

        [HttpPost("login")]
        public async Task<IActionResult> Login(LojaLoginRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.Email) || string.IsNullOrWhiteSpace(req.Password))
                return BadRequest("Email e password são obrigatórios.");

            // Sem espaços nas pontas: os teclados dos telemóveis costumam acrescentar um espaço no fim.
            var emailEntrada = req.Email.Trim();
            var chave = emailEntrada.ToLowerInvariant();
            var endereco = OrigemDoPedido.Endereco(HttpContext);
            if (FalhasDeLogin.Bloqueado(chave, endereco))
                return StatusCode(429, "Demasiadas tentativas. Tente novamente dentro de alguns minutos.");

            var loja = await _db.Lojas.FirstOrDefaultAsync(l => l.Email == emailEntrada);
            if (loja == null || !PasswordHasher.Verify(req.Password, loja.Password))
            {
                FalhasDeLogin.Falhou(chave, endereco);
                return Unauthorized("Credenciais inválidas.");
            }
            FalhasDeLogin.Sucesso(chave, endereco);

            var token = EmitirToken(loja);
            return Ok(new LojaLoginResponse(token, loja.Id, loja.Nome, loja.Aprovada));
        }

        // ── Recuperar a palavra-passe da loja ────────────────────────────────────

        public record EsqueciSenhaLojaRequest(string Email);
        public record RedefinirSenhaLojaRequest(string Email, string Codigo, string NovaPassword);

        private static readonly LimitadorTentativas PedidosDeRecuperacaoPorEmail = new(3, TimeSpan.FromHours(1));
        private static readonly LimitadorTentativas PedidosDeRecuperacaoPorEndereco = new(10, TimeSpan.FromHours(1));
        private static readonly LimitadorTentativas FalhasDeRedefinicao = new(10, TimeSpan.FromMinutes(15));
        private const int TentativasPorCodigo = 5;
        private static readonly TimeSpan ValidadeDoCodigo = TimeSpan.FromMinutes(15);

        /// <summary>
        /// Pede um código de 6 dígitos por email para redefinir a palavra-passe da loja. A resposta é sempre a mesma,
        /// exista ou não a loja, para não revelar quem está registado.
        /// </summary>
        [HttpPost("esqueci-senha")]
        public async Task<IActionResult> EsqueciSenha([FromBody] EsqueciSenhaLojaRequest req)
        {
            if (string.IsNullOrWhiteSpace(req?.Email)) return BadRequest("Indique o email.");
            var email = req.Email.Trim().ToLowerInvariant();
            if (!email.Contains('@') || email.Length > 200) return BadRequest("Indique um email válido.");

            if (!PedidosDeRecuperacaoPorEndereco.Permitir(OrigemDoPedido.Endereco(HttpContext)) ||
                !PedidosDeRecuperacaoPorEmail.Permitir(email))
                return StatusCode(429, "Foram feitos demasiados pedidos. Tente novamente dentro de uma hora.");

            var resposta = Ok(new { mensagem = "Se existir uma loja com este email, enviámos um código de 6 dígitos, válido durante 15 minutos." });

            var loja = await _db.Lojas.FirstOrDefaultAsync(l => l.Email == email);
            if (loja == null)
            {
                // Gasta o mesmo tempo (hash) que se a loja existisse, para o tempo de resposta não revelar quem está registado.
                PasswordHasher.Hash(System.Security.Cryptography.RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6"));
                return resposta;
            }

            var agora = DateTime.UtcNow;
            // Um código novo invalida os anteriores; apaga os que já têm mais de um dia.
            foreach (var antigo in await _db.RecuperacoesSenhaLoja.Where(r => r.LojaId == loja.Id && !r.Usado).ToListAsync())
                antigo.Usado = true;
            await _db.RecuperacoesSenhaLoja.Where(r => r.ExpiraEm < agora.AddDays(-1)).ExecuteDeleteAsync();

            var codigo = System.Security.Cryptography.RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");
            _db.RecuperacoesSenhaLoja.Add(new RecuperacaoSenhaLoja
            {
                LojaId = loja.Id,
                CodigoHash = PasswordHasher.Hash(codigo),
                CriadoEm = agora,
                ExpiraEm = agora + ValidadeDoCodigo,
            });
            await _db.SaveChangesAsync();

            // Só para testes locais, sem SMTP: mostra o código na consola do servidor.
            if (Environment.GetEnvironmentVariable("RECUPERACAO_MOSTRAR_CODIGO") == "1")
                Console.WriteLine($"[teste] código de recuperação para {email}: {codigo}");

            var corpo = $"<p>Olá {System.Net.WebUtility.HtmlEncode(loja.Nome)},</p>" +
                "<p>O seu código para redefinir a palavra-passe da loja na Ndatava é:</p>" +
                $"<p style=\"font-size:28px;letter-spacing:6px;font-weight:bold\">{codigo}</p>" +
                "<p>É válido durante 15 minutos. Se não foi você a pedi-lo, ignore este email: a sua loja continua segura.</p>";
            // Sem esperar: o tempo de resposta não deve denunciar se a loja existe. EmailService trata os seus próprios erros.
            _ = EmailService.EnviarAsync(_config, loja.Email, "Código para recuperar a palavra-passe da loja — Ndatava", corpo);

            return resposta;
        }

        /// <summary>Confirma o código recebido por email e define a palavra-passe nova. Entra logo na loja.</summary>
        [HttpPost("redefinir-senha")]
        public async Task<IActionResult> RedefinirSenha([FromBody] RedefinirSenhaLojaRequest req)
        {
            if (string.IsNullOrWhiteSpace(req?.Email) || string.IsNullOrWhiteSpace(req.Codigo) || string.IsNullOrEmpty(req.NovaPassword))
                return BadRequest("Preencha o email, o código e a palavra-passe nova.");
            // Mesmo mínimo do registo das lojas.
            if (req.NovaPassword.Length < MinPassword) return BadRequest($"A palavra-passe deve ter pelo menos {MinPassword} caracteres.");
            if (req.NovaPassword.Length > 100) return BadRequest("A palavra-passe é demasiado longa (máximo 100 caracteres).");

            var email = req.Email.Trim().ToLowerInvariant();
            if (FalhasDeRedefinicao.Excedeu(email))
                return StatusCode(429, "Demasiadas tentativas. Tente novamente dentro de alguns minutos.");

            const string invalido = "Código inválido ou expirado. Peça um novo código.";
            var loja = await _db.Lojas.FirstOrDefaultAsync(l => l.Email == email);
            if (loja == null) { FalhasDeRedefinicao.Registar(email); return BadRequest(invalido); }

            var agora = DateTime.UtcNow;
            var recuperacao = await _db.RecuperacoesSenhaLoja
                .Where(r => r.LojaId == loja.Id && !r.Usado && r.ExpiraEm > agora && r.Tentativas < TentativasPorCodigo)
                .OrderByDescending(r => r.CriadoEm)
                .FirstOrDefaultAsync();
            if (recuperacao == null) { FalhasDeRedefinicao.Registar(email); return BadRequest(invalido); }

            if (!PasswordHasher.Verify(req.Codigo.Trim(), recuperacao.CodigoHash))
            {
                recuperacao.Tentativas++;
                if (recuperacao.Tentativas >= TentativasPorCodigo) recuperacao.Usado = true;
                await _db.SaveChangesAsync();
                FalhasDeRedefinicao.Registar(email);
                return BadRequest(invalido);
            }

            loja.Password = PasswordHasher.Hash(req.NovaPassword);
            foreach (var r in await _db.RecuperacoesSenhaLoja.Where(r => r.LojaId == loja.Id && !r.Usado).ToListAsync())
                r.Usado = true;
            await _db.SaveChangesAsync();

            FalhasDeRedefinicao.Limpar(email);
            FalhasDeLogin.LimparEmail(email);
            return Ok(new LojaLoginResponse(EmitirToken(loja), loja.Id, loja.Nome, loja.Aprovada));
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
            if (req.Nome.Trim().Length > 150)
                return BadRequest("O nome da loja é demasiado longo (máximo 150 caracteres).");
            if (!CoordenadasValidas(req.Latitude, req.Longitude))
                return BadRequest("A localização indicada é inválida.");

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

        // DELETE /api/lojas/eu — a própria loja pede a eliminação da sua conta e dados
        // (exigido pela App Store, Diretriz 5.1.1(v): quem cria conta tem de conseguir
        // eliminá-la a partir da app, sem depender de suporte/email).
        //
        // Sem histórico de encomendas: elimina tudo definitivamente (loja e produtos).
        // Com histórico: não pode apagar as linhas ligadas a encomendas já feitas
        // (ficariam órfãs), por isso anonimiza todos os dados pessoais e desativa a
        // conta — deixa de conseguir entrar e deixa de aparecer para compradores,
        // mas o registo contabilístico das vendas passadas mantém-se, sem identificar
        // a loja. Uma desativação sozinha (sem apagar/anonimizar dados) não cumpre a
        // diretriz, por isso os dados pessoais são sempre removidos aqui.
        [HttpDelete("eu")]
        [Authorize(Policy = "Loja")]
        public async Task<IActionResult> EliminarContaPropria()
        {
            var loja = await _db.Lojas
                .Include(l => l.FormasPagamento)
                .FirstOrDefaultAsync(l => l.Id == LojaIdAtual);
            if (loja == null) return NotFound();

            var produtoIds = await _db.Produtos.Where(p => p.LojaId == loja.Id).Select(p => p.Id).ToListAsync();
            var temEncomendas = await _db.Encomendas.AnyAsync(e => e.LojaId == loja.Id)
                || (produtoIds.Count > 0 && await _db.ItensEncomenda.AnyAsync(i => produtoIds.Contains(i.ProdutoId)));

            if (!temEncomendas)
            {
                var imagens = new List<string?>();
                if (produtoIds.Count > 0)
                {
                    var produtos = await _db.Produtos.Where(p => produtoIds.Contains(p.Id)).ToListAsync();
                    imagens.AddRange(produtos.Select(p => p.ImagemUrl));
                    _db.Produtos.RemoveRange(produtos);
                }
                _db.Lojas.Remove(loja);
                await _db.SaveChangesAsync();
                await ApagarImagensSemUsoAsync(imagens);
                return NoContent();
            }

            _db.FormasPagamentoLoja.RemoveRange(loja.FormasPagamento);
            loja.Nome = "Conta eliminada";
            loja.Email = $"eliminada-{loja.Id}-{Guid.NewGuid():N}@ndatava.invalid";
            loja.Password = PasswordHasher.Hash(Guid.NewGuid().ToString());
            loja.Telefone = null;
            loja.Morada = null;
            loja.Descricao = null;
            loja.InfoPagamento = null;
            loja.Categoria = null;
            loja.Latitude = 0;
            loja.Longitude = 0;
            loja.Aprovada = false;
            loja.Ativa = false;

            var imagensDesativadas = new List<string?>();
            if (produtoIds.Count > 0)
            {
                var produtos = await _db.Produtos.Where(p => produtoIds.Contains(p.Id)).ToListAsync();
                foreach (var p in produtos)
                {
                    p.Disponivel = false;
                    // Os produtos ficam (histórico de encomendas), mas as fotos da loja saem.
                    imagensDesativadas.Add(p.ImagemUrl);
                    p.ImagemUrl = null;
                }
            }

            await _db.SaveChangesAsync();
            await ApagarImagensSemUsoAsync(imagensDesativadas);
            return NoContent();
        }

        // Apaga os ficheiros de imagem que já nenhum produto usa.
        private async Task ApagarImagensSemUsoAsync(IEnumerable<string?> urls)
        {
            foreach (var url in urls.Where(u => !string.IsNullOrWhiteSpace(u)).Distinct())
                if (!await _db.Produtos.AnyAsync(p => p.ImagemUrl == url))
                    await _armazenamento.ApagarSeForNossoAsync(url);
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
