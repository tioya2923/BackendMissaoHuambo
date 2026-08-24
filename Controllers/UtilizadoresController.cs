using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
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

    private string GerarToken(Utilizador u)
    {
        var jwtSection = _config.GetSection("Jwt");
        var key = Encoding.UTF8.GetBytes(jwtSection["Key"]!);
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, u.Email),
            new Claim("utilizadorId", u.Id.ToString())
        };
        var creds = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: jwtSection["Issuer"],
            audience: jwtSection["Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddDays(30),
            signingCredentials: creds
        );
        return new JwtSecurityTokenHandler().WriteToken(token);
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

        if (await _db.Utilizadores.AnyAsync(u => u.Email == req.Email.ToLower()))
            return Conflict("Este email já está registado.");

        var utilizador = new Utilizador
        {
            Nome     = req.Nome.Trim(),
            Email    = req.Email.Trim().ToLower(),
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

        var utilizador = await _db.Utilizadores
            .FirstOrDefaultAsync(u => u.Email == req.Email.ToLower());

        if (utilizador == null || !PasswordHasher.Verify(req.Password, utilizador.Password))
            return Unauthorized("Credenciais inválidas.");

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
