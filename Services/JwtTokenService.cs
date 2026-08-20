using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace MissaoBackend.Services;

// Centraliza a emissão de tokens JWT para os dois tipos de conta da plataforma:
// "gestor" (administrador único, gere o conteúdo religioso e modera as lojas) e
// "loja" (cada loja parceira, gere os seus próprios produtos e encomendas).
public static class JwtTokenService
{
    public class JwtNaoConfiguradoException : Exception
    {
        public JwtNaoConfiguradoException(string mensagem) : base(mensagem) { }
    }

    public static string Criar(IConfiguration config, IEnumerable<Claim> claims, TimeSpan? validade = null)
    {
        var jwtSection = config.GetSection("Jwt");
        var jwtKey = Environment.GetEnvironmentVariable("JWT_KEY") ?? jwtSection["Key"];
        var jwtIssuer = Environment.GetEnvironmentVariable("JWT_ISSUER") ?? jwtSection["Issuer"];
        var jwtAudience = Environment.GetEnvironmentVariable("JWT_AUDIENCE") ?? jwtSection["Audience"];

        if (string.IsNullOrWhiteSpace(jwtKey) || jwtKey == "USE_ENVIRONMENT_VARIABLE")
            throw new JwtNaoConfiguradoException("JWT_KEY não está configurada no servidor.");

        var key = Encoding.UTF8.GetBytes(jwtKey);
        var creds = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: jwtIssuer,
            audience: jwtAudience,
            claims: claims,
            expires: DateTime.UtcNow.Add(validade ?? TimeSpan.FromHours(8)),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
