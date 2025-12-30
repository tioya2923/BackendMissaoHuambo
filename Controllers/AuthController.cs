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

    public record LoginRequest(string Email, string Password);

    public record LoginResponse(string Token, string Nome, string Email);

    [HttpPost("login")]
    [Produces("application/json")]
    public async Task<IActionResult> Login([FromBody] LoginRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Email) || string.IsNullOrWhiteSpace(req.Password))
            return BadRequest("Email e password são obrigatórios.");

        var gestor = await _db.Gestores.FirstOrDefaultAsync(g => g.Email == req.Email);
        if (gestor == null || !PasswordHasher.Verify(req.Password, gestor.PasswordHash))
            return Unauthorized("Credenciais inválidas.");

        var jwtSection = _config.GetSection("Jwt");
        var key = Encoding.UTF8.GetBytes(jwtSection["Key"]!);

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
            issuer: jwtSection["Issuer"],
            audience: jwtSection["Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(4),
            signingCredentials: creds
        );

        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

        return Ok(new LoginResponse(tokenString, gestor.Nome, gestor.Email));
    }
}
