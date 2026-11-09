using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using MissaoBackend.Data;
using MissaoBackend.Models;
using MissaoBackend.Services;

namespace MissaoBackend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UtilizadoresController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _config;
    private readonly IWebHostEnvironment _env;
    private readonly ArmazenamentoService _armazenamento;

    // Travam adivinhação de palavras-passe (por email) e criação massiva de contas (por endereço).
    private static readonly LimitadorTentativas FalhasDeLogin = new(8, TimeSpan.FromMinutes(15));
    private static readonly LimitadorTentativas RegistosPorEndereco = new(
        int.TryParse(Environment.GetEnvironmentVariable("LIMITE_REGISTOS_HORA"), out var lim) ? lim : 30, TimeSpan.FromHours(1));

    // Recuperação de palavra-passe: poucos pedidos por email e por endereço, e travão a quem tenta adivinhar o código.
    private static readonly LimitadorTentativas PedidosDeRecuperacaoPorEmail = new(3, TimeSpan.FromHours(1));
    private static readonly LimitadorTentativas PedidosDeRecuperacaoPorEndereco = new(10, TimeSpan.FromHours(1));
    private static readonly LimitadorTentativas FalhasDeRedefinicao = new(10, TimeSpan.FromMinutes(15));
    private const int TentativasPorCodigo = 5;
    private static readonly TimeSpan ValidadeDoCodigo = TimeSpan.FromMinutes(15);

    public UtilizadoresController(AppDbContext db, IConfiguration config, IWebHostEnvironment env, ArmazenamentoService armazenamento)
    {
        _db = db;
        _config = config;
        _env = env;
        _armazenamento = armazenamento;
    }

    // ── DTOs ──────────────────────────────────────────────────────────────────

    public record RegistarRequest(string Nome, string Email, string Password);
    public record LoginRequest(string Email, string Password);
    public record AuthResponse(string Token, string Nome, string Email);
    public record EsqueciSenhaRequest(string Email);
    public record RedefinirSenhaRequest(string Email, string Codigo, string NovaPassword);
    public record DataCampoDto(string Dia, string Mes, string Ano);

    public record AtualizarPerfilRequest(
        string Nome,
        DataCampoDto? Nascimento,
        DataCampoDto? Baptismo,
        DataCampoDto? Comunhao,
        DataCampoDto? Crisma,
        DataCampoDto? Casamento,
        DataCampoDto? Ordem,
        string? Diocese,
        string? Paroquia
    );

    public record PerfilResponse(
        string Nome,
        string Email,
        string? FotoUrl,
        DataCampoDto Nascimento,
        DataCampoDto Baptismo,
        DataCampoDto Comunhao,
        DataCampoDto Crisma,
        DataCampoDto Casamento,
        DataCampoDto Ordem,
        string? Diocese,
        string? Paroquia
    );

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static DataCampoDto ParseData(string? s)
    {
        if (string.IsNullOrEmpty(s)) return new DataCampoDto("", "", "");
        var p = s.Split('/');
        return p.Length == 3 ? new DataCampoDto(p[0], p[1], p[2]) : new DataCampoDto("", "", "");
    }

    private static string? SerializarData(DataCampoDto? d)
    {
        if (d == null || string.IsNullOrEmpty(d.Dia)) return null;
        return $"{d.Dia.PadLeft(2, '0')}/{d.Mes.PadLeft(2, '0')}/{d.Ano}";
    }

    private PerfilResponse MapPerfil(Utilizador u) => new(
        u.Nome, u.Email, u.FotoUrl,
        ParseData(u.Nascimento), ParseData(u.Baptismo), ParseData(u.Comunhao),
        ParseData(u.Crisma), ParseData(u.Casamento), ParseData(u.Ordem),
        u.Diocese, u.Paroquia
    );

    // Usa o mesmo serviço das lojas e do gestor: lê JWT_KEY/ISSUER/AUDIENCE do ambiente. (Antes lia só
    // o ficheiro de configuração, onde a chave é um marcador — por isso o registo/entrada falhava com erro 500.)
    private string GerarToken(Utilizador u)
    {
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, u.Email),
            new Claim("utilizadorId", u.Id.ToString()),
            new Claim("tipo", "utilizador"),
        };
        return JwtTokenService.Criar(_config, claims, TimeSpan.FromDays(30));
    }

    private int? UtilizadorId()
    {
        var claim = User.FindFirst("utilizadorId")?.Value;
        return int.TryParse(claim, out var id) ? id : null;
    }

    // ── Endpoints ─────────────────────────────────────────────────────────────

    [HttpPost("registar")]
    public async Task<IActionResult> Registar([FromBody] RegistarRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Nome) ||
            string.IsNullOrWhiteSpace(req.Email) ||
            string.IsNullOrWhiteSpace(req.Password))
            return BadRequest("Nome, email e password são obrigatórios.");

        var email = req.Email.Trim().ToLower();
        if (!email.Contains('@') || email.Length > 200)
            return BadRequest("Indique um email válido.");
        if (req.Password.Length < 8)
            return BadRequest("A palavra-passe deve ter pelo menos 8 caracteres.");
        if (req.Password.Length > 100)
            return BadRequest("A palavra-passe é demasiado longa (máximo 100 caracteres).");
        var nome = req.Nome.Trim();
        if (nome.Length > 100)
            return BadRequest("O nome é demasiado longo (máximo 100 caracteres).");

        if (!RegistosPorEndereco.Permitir(OrigemDoPedido.Endereco(HttpContext)))
            return StatusCode(429, "Foram criadas demasiadas contas a partir desta ligação. Tente mais tarde.");

        if (await _db.Utilizadores.AnyAsync(u => u.Email == email))
            return Conflict("Este email já está registado.");

        var utilizador = new Utilizador
        {
            Nome     = nome,
            Email    = email,
            Password = PasswordHasher.Hash(req.Password)
        };
        _db.Utilizadores.Add(utilizador);
        await _db.SaveChangesAsync();

        return Ok(new AuthResponse(GerarToken(utilizador), utilizador.Nome, utilizador.Email));
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Email) || string.IsNullOrWhiteSpace(req.Password))
            return BadRequest("Email e password são obrigatórios.");

        var chaveEmail = req.Email.Trim().ToLower();
        if (FalhasDeLogin.Excedeu(chaveEmail))
            return StatusCode(429, "Demasiadas tentativas. Tente novamente dentro de alguns minutos.");

        var utilizador = await _db.Utilizadores
            .FirstOrDefaultAsync(u => u.Email == chaveEmail);

        if (utilizador == null || !PasswordHasher.Verify(req.Password, utilizador.Password))
        {
            FalhasDeLogin.Registar(chaveEmail);
            return Unauthorized("Credenciais inválidas.");
        }
        FalhasDeLogin.Limpar(chaveEmail);

        return Ok(new AuthResponse(GerarToken(utilizador), utilizador.Nome, utilizador.Email));
    }

    /// <summary>
    /// Pede um código de 6 dígitos por email para redefinir a palavra-passe. A resposta é sempre a mesma, exista
    /// ou não a conta, para não revelar quem está registado.
    /// </summary>
    [HttpPost("esqueci-senha")]
    public async Task<IActionResult> EsqueciSenha([FromBody] EsqueciSenhaRequest req)
    {
        if (string.IsNullOrWhiteSpace(req?.Email)) return BadRequest("Indique o email.");
        var email = req.Email.Trim().ToLower();
        if (!email.Contains('@') || email.Length > 200) return BadRequest("Indique um email válido.");

        if (!PedidosDeRecuperacaoPorEndereco.Permitir(OrigemDoPedido.Endereco(HttpContext)) ||
            !PedidosDeRecuperacaoPorEmail.Permitir(email))
            return StatusCode(429, "Foram feitos demasiados pedidos. Tente novamente dentro de uma hora.");

        var resposta = Ok(new { mensagem = "Se existir uma conta com este email, enviámos um código de 6 dígitos, válido durante 15 minutos." });

        var utilizador = await _db.Utilizadores.FirstOrDefaultAsync(u => u.Email == email);
        if (utilizador == null) return resposta;

        var agora = DateTime.UtcNow;
        // Um código novo invalida os anteriores; apaga os que já têm mais de um dia.
        foreach (var antigo in await _db.RecuperacoesSenha.Where(r => r.UtilizadorId == utilizador.Id && !r.Usado).ToListAsync())
            antigo.Usado = true;
        await _db.RecuperacoesSenha.Where(r => r.ExpiraEm < agora.AddDays(-1)).ExecuteDeleteAsync();

        var codigo = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");
        _db.RecuperacoesSenha.Add(new RecuperacaoSenha
        {
            UtilizadorId = utilizador.Id,
            CodigoHash = PasswordHasher.Hash(codigo),
            CriadoEm = agora,
            ExpiraEm = agora + ValidadeDoCodigo,
        });
        await _db.SaveChangesAsync();

        // Só para testes locais, sem SMTP: mostra o código na consola do servidor.
        if (Environment.GetEnvironmentVariable("RECUPERACAO_MOSTRAR_CODIGO") == "1")
            Console.WriteLine($"[teste] código de recuperação para {email}: {codigo}");

        var corpo = $"<p>Olá {System.Net.WebUtility.HtmlEncode(utilizador.Nome)},</p>" +
            "<p>O seu código para redefinir a palavra-passe da Ndatava é:</p>" +
            $"<p style=\"font-size:28px;letter-spacing:6px;font-weight:bold\">{codigo}</p>" +
            "<p>É válido durante 15 minutos. Se não foi você a pedi-lo, ignore este email: a sua conta continua segura.</p>";
        // Sem esperar: o tempo de resposta não deve denunciar se a conta existe. EmailService trata os seus próprios erros.
        _ = EmailService.EnviarAsync(_config, utilizador.Email, "Código para recuperar a palavra-passe — Ndatava", corpo);

        return resposta;
    }

    /// <summary>Confirma o código recebido por email e define a palavra-passe nova. Entra logo na conta.</summary>
    [HttpPost("redefinir-senha")]
    public async Task<IActionResult> RedefinirSenha([FromBody] RedefinirSenhaRequest req)
    {
        if (string.IsNullOrWhiteSpace(req?.Email) || string.IsNullOrWhiteSpace(req.Codigo) || string.IsNullOrEmpty(req.NovaPassword))
            return BadRequest("Preencha o email, o código e a palavra-passe nova.");
        if (req.NovaPassword.Length < 8) return BadRequest("A palavra-passe deve ter pelo menos 8 caracteres.");
        if (req.NovaPassword.Length > 100) return BadRequest("A palavra-passe é demasiado longa (máximo 100 caracteres).");

        var email = req.Email.Trim().ToLower();
        if (FalhasDeRedefinicao.Excedeu(email))
            return StatusCode(429, "Demasiadas tentativas. Tente novamente dentro de alguns minutos.");

        const string invalido = "Código inválido ou expirado. Peça um novo código.";
        var utilizador = await _db.Utilizadores.FirstOrDefaultAsync(u => u.Email == email);
        if (utilizador == null) { FalhasDeRedefinicao.Registar(email); return BadRequest(invalido); }

        var agora = DateTime.UtcNow;
        var recuperacao = await _db.RecuperacoesSenha
            .Where(r => r.UtilizadorId == utilizador.Id && !r.Usado && r.ExpiraEm > agora && r.Tentativas < TentativasPorCodigo)
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

        utilizador.Password = PasswordHasher.Hash(req.NovaPassword);
        foreach (var r in await _db.RecuperacoesSenha.Where(r => r.UtilizadorId == utilizador.Id && !r.Usado).ToListAsync())
            r.Usado = true;
        await _db.SaveChangesAsync();

        FalhasDeRedefinicao.Limpar(email);
        FalhasDeLogin.Limpar(email);
        return Ok(new AuthResponse(GerarToken(utilizador), utilizador.Nome, utilizador.Email));
    }

    [HttpGet("eu")]
    [Authorize]
    public async Task<IActionResult> GetEu()
    {
        var id = UtilizadorId();
        if (id == null) return Unauthorized();
        var u = await _db.Utilizadores.FindAsync(id);
        if (u == null) return NotFound();
        return Ok(MapPerfil(u));
    }

    [HttpPut("eu")]
    [Authorize]
    public async Task<IActionResult> AtualizarEu([FromBody] AtualizarPerfilRequest req)
    {
        var id = UtilizadorId();
        if (id == null) return Unauthorized();
        var u = await _db.Utilizadores.FindAsync(id);
        if (u == null) return NotFound();

        if (string.IsNullOrWhiteSpace(req.Nome))
            return BadRequest("O nome não pode estar vazio.");

        u.Nome              = req.Nome.Trim();
        u.Nascimento        = SerializarData(req.Nascimento);
        u.Baptismo          = SerializarData(req.Baptismo);
        u.Comunhao          = SerializarData(req.Comunhao);
        u.Crisma            = SerializarData(req.Crisma);
        u.Casamento         = SerializarData(req.Casamento);
        u.Ordem             = SerializarData(req.Ordem);
        u.Diocese           = req.Diocese?.Trim();
        u.Paroquia          = req.Paroquia?.Trim();

        await _db.SaveChangesAsync();
        return Ok(MapPerfil(u));
    }

    // ── Administração (Gestor) ───────────────────────────────────────────────
    // Nunca devolve nem aceita a password em texto simples pelo painel de
    // administração — só o próprio utilizador a define/altera via /registar.

    public record UtilizadorAdminDto(
        int Id, string Nome, string Email, string? FotoUrl,
        string? Nascimento, string? Baptismo, string? Comunhao, string? Crisma,
        string? Casamento, string? Ordem,
        string? Diocese, string? Paroquia
    );

    public record AtualizarUtilizadorAdminRequest(
        string Nome, string Email,
        string? Nascimento, string? Baptismo, string? Comunhao, string? Crisma,
        string? Casamento, string? Ordem,
        string? Diocese, string? Paroquia
    );

    private static UtilizadorAdminDto MapAdmin(Utilizador u) => new(
        u.Id, u.Nome, u.Email, u.FotoUrl,
        u.Nascimento, u.Baptismo, u.Comunhao, u.Crisma, u.Casamento, u.Ordem,
        u.Diocese, u.Paroquia
    );

    public record CriarUtilizadorAdminRequest(string Nome, string Email, string PasswordInicial);

    [HttpGet]
    [Authorize(Policy = "Gestor")]
    public async Task<IActionResult> ListarAdmin()
    {
        var lista = await _db.Utilizadores.OrderBy(u => u.Nome).ToListAsync();
        return Ok(lista.Select(MapAdmin));
    }

    [HttpPost]
    [Authorize(Policy = "Gestor")]
    public async Task<IActionResult> CriarAdmin([FromBody] CriarUtilizadorAdminRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Nome) ||
            string.IsNullOrWhiteSpace(req.Email) ||
            string.IsNullOrWhiteSpace(req.PasswordInicial))
            return BadRequest("Nome, email e password inicial são obrigatórios.");

        var email = req.Email.Trim().ToLower();
        if (await _db.Utilizadores.AnyAsync(u => u.Email == email))
            return Conflict("Este email já está registado.");

        var utilizador = new Utilizador
        {
            Nome = req.Nome.Trim(),
            Email = email,
            Password = PasswordHasher.Hash(req.PasswordInicial),
        };
        _db.Utilizadores.Add(utilizador);
        await _db.SaveChangesAsync();

        return Ok(MapAdmin(utilizador));
    }

    [HttpGet("{id:int}")]
    [Authorize(Policy = "Gestor")]
    public async Task<IActionResult> GetAdmin(int id)
    {
        var u = await _db.Utilizadores.FindAsync(id);
        if (u == null) return NotFound();
        return Ok(MapAdmin(u));
    }

    [HttpPut("{id:int}")]
    [Authorize(Policy = "Gestor")]
    public async Task<IActionResult> AtualizarAdmin(int id, [FromBody] AtualizarUtilizadorAdminRequest req)
    {
        var u = await _db.Utilizadores.FindAsync(id);
        if (u == null) return NotFound();

        if (string.IsNullOrWhiteSpace(req.Nome) || string.IsNullOrWhiteSpace(req.Email))
            return BadRequest("Nome e email são obrigatórios.");

        var novoEmail = req.Email.Trim().ToLower();
        if (novoEmail != u.Email && await _db.Utilizadores.AnyAsync(x => x.Email == novoEmail && x.Id != id))
            return Conflict("Já existe outro utilizador com este email.");

        u.Nome = req.Nome.Trim();
        u.Email = novoEmail;
        u.Nascimento = req.Nascimento;
        u.Baptismo = req.Baptismo;
        u.Comunhao = req.Comunhao;
        u.Crisma = req.Crisma;
        u.Casamento = req.Casamento;
        u.Ordem = req.Ordem;
        u.Diocese = req.Diocese?.Trim();
        u.Paroquia = req.Paroquia?.Trim();

        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    [Authorize(Policy = "Gestor")]
    public async Task<IActionResult> DeleteAdmin(int id)
    {
        var u = await _db.Utilizadores.FindAsync(id);
        if (u == null) return NotFound();

        _db.Utilizadores.Remove(u);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("eu")]
    [Authorize]
    public async Task<IActionResult> EliminarEu()
    {
        var id = UtilizadorId();
        if (id == null) return Unauthorized();
        var u = await _db.Utilizadores.FindAsync(id);
        if (u == null) return NotFound();
        // Entrega a administração dos grupos que ficariam sem administrador (ou apaga-os se ficarem vazios).
        await GruposRegras.PrepararEliminacaoConta(_db, u.Id);
        // Partilhas enviadas e recebidas, gravações, contactos e bloqueios desta conta são apagados em cascata.
        _db.Utilizadores.Remove(u);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost("eu/foto")]
    [Authorize]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadFoto(IFormFile foto)
    {
        var id = UtilizadorId();
        if (id == null) return Unauthorized();
        var u = await _db.Utilizadores.FindAsync(id);
        if (u == null) return NotFound();

        if (foto == null || foto.Length == 0)
            return BadRequest("Ficheiro inválido.");

        var ext = Path.GetExtension(foto.FileName).ToLowerInvariant();
        if (ext is not (".jpg" or ".jpeg" or ".png" or ".webp"))
            return BadRequest("Apenas imagens JPG, PNG ou WEBP são aceites.");

        var urlAnterior = u.FotoUrl;
        u.FotoUrl = await _armazenamento.GuardarAsync(foto);
        await _db.SaveChangesAsync();
        await _armazenamento.ApagarSeForNossoAsync(urlAnterior);

        return Ok(new { fotoUrl = u.FotoUrl });
    }
}
