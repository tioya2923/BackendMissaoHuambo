using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;

using MissaoBackend.Data;
using MissaoBackend.Models;
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

    [HttpPost("login")]
    [Produces("application/json")]
    public async Task<IActionResult> Login([FromBody] GestorLoginRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Email) || string.IsNullOrWhiteSpace(req.Password))
            return BadRequest("Email e password são obrigatórios.");

        var gestor = await _db.Gestores.FirstOrDefaultAsync(g => g.Email == req.Email);
        if (gestor == null || !PasswordHasher.Verify(req.Password, gestor.Password))
            return Unauthorized("Credenciais inválidas.");

        var jwtSection = _config.GetSection("Jwt");
        var jwtKey = Environment.GetEnvironmentVariable("JWT_KEY") ?? jwtSection["Key"];
        var jwtIssuer = Environment.GetEnvironmentVariable("JWT_ISSUER") ?? jwtSection["Issuer"];
        var jwtAudience = Environment.GetEnvironmentVariable("JWT_AUDIENCE") ?? jwtSection["Audience"];

        if (string.IsNullOrWhiteSpace(jwtKey) || jwtKey == "USE_ENVIRONMENT_VARIABLE")
            return StatusCode(500, "JWT_KEY não está configurada no servidor.");

        var key = Encoding.UTF8.GetBytes(jwtKey);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, gestor.Email),
            new Claim("gestorId", gestor.Id.ToString())
        };

        var creds = new SigningCredentials(
            new SymmetricSecurityKey(key),
            SecurityAlgorithms.HmacSha256
        );

        var token = new JwtSecurityToken(
            issuer: jwtIssuer,
            audience: jwtAudience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(4),
            signingCredentials: creds
        );

        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

        return Ok(new LoginResponse(tokenString, gestor.Nome, gestor.Email));
    }
}
