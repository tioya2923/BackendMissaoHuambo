using System.Security.Claims;
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

    [HttpPost("login")]
    [Produces("application/json")]
    public async Task<IActionResult> Login([FromBody] GestorLoginRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Email) || string.IsNullOrWhiteSpace(req.Password))
            return BadRequest("Email e password são obrigatórios.");

        var gestor = await _db.Gestores.FirstOrDefaultAsync(g => g.Email == req.Email);
        if (gestor == null || !PasswordHasher.Verify(req.Password, gestor.Password))
            return Unauthorized("Credenciais inválidas.");

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
}
