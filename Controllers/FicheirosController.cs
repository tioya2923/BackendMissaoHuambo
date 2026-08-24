using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MissaoBackend.Data;

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
        return File(ficheiro.Dados, ficheiro.ContentType);
    }
}
