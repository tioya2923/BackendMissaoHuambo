using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MissaoBackend.Data;
using MissaoBackend.Services;

namespace MissaoBackend.Controllers;

[ApiController]
[Route("api/ficheiros")]
public class FicheirosController : ControllerBase
{
    private readonly AppDbContext _db;
    public FicheirosController(AppDbContext db) => _db = db;

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id)
    {
        var ficheiro = await _db.Ficheiros.AsNoTracking().FirstOrDefaultAsync(f => f.Id == id);
        if (ficheiro == null) return NotFound();

        Response.Headers.CacheControl = "public, max-age=31536000, immutable";
        // O conteúdo vem de utilizadores: nunca deve ser interpretado como página.
        Response.Headers["X-Content-Type-Options"] = "nosniff";
        Response.Headers["Content-Security-Policy"] = "default-src 'none'; sandbox";

        // Ficheiros gravados antes da validação por bytes podem ter um tipo errado ou perigoso (ex.: text/html).
        // O tipo servido é sempre o detectado nos bytes (imagem ou PDF); se não for nenhum, vai como descarga binária.
        var tipo = ArmazenamentoService.DetectarTipo(ficheiro.Dados, TipoFicheiro.Imagem)
            ?? ArmazenamentoService.DetectarTipo(ficheiro.Dados, TipoFicheiro.Pdf);
        if (tipo == null)
            return File(ficheiro.Dados, "application/octet-stream", $"ficheiro-{id}");

        return File(ficheiro.Dados, tipo);
    }
}
