using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;

using MissaoBackend.Data;
using MissaoBackend.Services;

namespace MissaoBackend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _config;

    public AuthController(AppDbContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
    }

    public record GestorLoginRequest(string Email, string Password);

    public record LoginResponse(string Token, string Nome, string Email);

    // Trava a adivinhação da palavra-passe do administrador (por email).
    private static readonly ProtecaoLogin FalhasDeLogin = new();

    [HttpPost("login")]
    [Produces("application/json")]
    public async Task<IActionResult> Login([FromBody] GestorLoginRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Email) || string.IsNullOrWhiteSpace(req.Password))
            return BadRequest("Email e password são obrigatórios.");

        var emailEntrada = req.Email.Trim();
        var chave = emailEntrada.ToLowerInvariant();
        var endereco = OrigemDoPedido.Endereco(HttpContext);
        if (FalhasDeLogin.Bloqueado(chave, endereco))
            return StatusCode(429, "Demasiadas tentativas. Tente novamente dentro de alguns minutos.");

        var gestor = await _db.Gestores.FirstOrDefaultAsync(g => g.Email == emailEntrada);
        if (gestor == null || !PasswordHasher.Verify(req.Password, gestor.Password))
        {
            FalhasDeLogin.Falhou(chave, endereco);
            return Unauthorized("Credenciais inválidas.");
        }
        FalhasDeLogin.Sucesso(chave, endereco);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, gestor.Email),
            new Claim("gestorId", gestor.Id.ToString()),
            new Claim("tipo", "gestor"),
        };

        string tokenString;
        try
        {
            tokenString = JwtTokenService.Criar(_config, claims, TimeSpan.FromHours(4));
        }
        catch (JwtTokenService.JwtNaoConfiguradoException ex)
        {
            return StatusCode(500, ex.Message);
        }

        return Ok(new LoginResponse(tokenString, gestor.Nome, gestor.Email));
    }

    public record AlterarCredenciaisRequest(string PasswordAtual, string NovoEmail, string NovaPassword);

    // PUT /api/auth/gestor/credenciais — o próprio Gestor autenticado muda o seu email e/ou
    // password (ex.: para rodar credenciais comprometidas, sem precisar de acesso à base de dados).
    [HttpPut("gestor/credenciais")]
    [Authorize(Policy = "Gestor")]
    public async Task<IActionResult> AlterarCredenciaisGestor(AlterarCredenciaisRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.PasswordAtual) || string.IsNullOrWhiteSpace(req.NovoEmail) || string.IsNullOrWhiteSpace(req.NovaPassword))
            return BadRequest("Password atual, email e password nova são obrigatórios.");
        if (req.NovaPassword.Length < 10)
            return BadRequest("A password nova tem de ter pelo menos 10 caracteres.");
        if (req.NovaPassword.Length > 100)
            return BadRequest("A password nova é demasiado longa (máximo 100 caracteres).");
        if (!req.NovoEmail.Contains('@'))
            return BadRequest("Indique um email válido.");

        var gestorId = int.Parse(User.FindFirstValue("gestorId") ?? throw new InvalidOperationException("Token sem gestorId."));
        var gestor = await _db.Gestores.FindAsync(gestorId);
        if (gestor == null) return NotFound();

        // Um token roubado não chega para tomar a conta: é preciso saber a password atual.
        var endereco = OrigemDoPedido.Endereco(HttpContext);
        var chaveGestor = "gestor:" + gestor.Id;
        if (FalhasDeLogin.Bloqueado(chaveGestor, endereco))
            return StatusCode(429, "Demasiadas tentativas. Tente novamente dentro de alguns minutos.");
        if (!PasswordHasher.Verify(req.PasswordAtual, gestor.Password))
        {
            FalhasDeLogin.Falhou(chaveGestor, endereco);
            return BadRequest("A password atual está incorreta.");
        }
        FalhasDeLogin.Sucesso(chaveGestor, endereco);

        var novoEmail = req.NovoEmail.Trim();
        if (await _db.Gestores.AnyAsync(g => g.Email == novoEmail && g.Id != gestorId))
            return BadRequest("Já existe um administrador com este email.");

        gestor.Email = novoEmail;
        gestor.Password = PasswordHasher.Hash(req.NovaPassword);
        await _db.SaveChangesAsync();

        return NoContent();
    }
}
