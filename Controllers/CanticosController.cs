using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MissaoBackend.Data;
using MissaoBackend.Models;
using MissaoBackend.Utils;

namespace MissaoBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CanticosController : ControllerBase
    {
        [HttpPost("topico")]
        public async Task<ActionResult<Topico>> CreateTopico(Topico input)
        {
            if (string.IsNullOrWhiteSpace(input.Slug) && !string.IsNullOrWhiteSpace(input.Nome))
            {
                input.Slug = SlugHelper.Slugify(input.Nome);
            }
            _db.Topicos.Add(input);
            await _db.SaveChangesAsync();
            return Created($"/api/Canticos/topico/{input.Slug}", input);
        }
        private readonly AppDbContext _db;
        private readonly IWebHostEnvironment _env;

        public CanticosController(AppDbContext db, IWebHostEnvironment env)
        {
            _db = db;
            _env = env;
        }

        [HttpGet("topico/{slug}")]
        public async Task<ActionResult> GetByTopico(string slug)
        {
            var topico = await _db.Topicos
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Slug == slug);

            if (topico == null) return NotFound();

            var canticos = await _db.Canticos
                .Where(c => c.TopicoId == topico.Id)
                .OrderBy(c => c.Titulo)
                .Select(c => new { c.Id, c.Titulo, c.Slug })
                .ToListAsync();

            return Ok(canticos);
        }

        [HttpGet("{slug}")]
        public async Task<ActionResult<Cantico>> GetBySlug(string slug)
        {
            var cantico = await _db.Canticos
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Slug == slug);

            if (cantico == null) return NotFound();
            return Ok(cantico);
        }

        [HttpGet("search")]
        public async Task<ActionResult> Search([FromQuery] string q)
        {
            if (string.IsNullOrWhiteSpace(q))
                return Ok(Array.Empty<object>());

            q = q.Trim();

            var results = await _db.Canticos
                .Where(c =>
                    EF.Functions.Like(c.Titulo, $"%{q}%") ||
                    EF.Functions.Like(c.Letra, $"%{q}%"))
                .OrderBy(c => c.Titulo)
                .Select(c => new { c.Id, c.Titulo, c.Slug })
                .ToListAsync();

            return Ok(results);
        }

        [HttpPost]
        [Authorize]
        public async Task<ActionResult<Cantico>> Create(Cantico input)
        {
            input.Slug = SlugHelper.Slugify(input.Titulo);
            _db.Canticos.Add(input);
            await _db.SaveChangesAsync();

            return CreatedAtAction(nameof(GetBySlug), new { slug = input.Slug }, input);
        }

        [HttpPut("{id:int}")]
        [Authorize]
        public async Task<IActionResult> Update(int id, Cantico input)
        {
            var existing = await _db.Canticos.FindAsync(id);
            if (existing == null) return NotFound();

            existing.Titulo = input.Titulo;
            existing.Letra = input.Letra;
            existing.TopicoId = input.TopicoId;
            existing.Slug = SlugHelper.Slugify(input.Titulo);

            await _db.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id:int}")]
        [Authorize]
        public async Task<IActionResult> Delete(int id)
        {
            var existing = await _db.Canticos.FindAsync(id);
            if (existing == null) return NotFound();

            _db.Canticos.Remove(existing);
            await _db.SaveChangesAsync();
            return NoContent();
        }

        [HttpPost("{id:int}/upload-pdf")]
        [Authorize]
        public async Task<IActionResult> UploadPdf(int id, IFormFile file)
        {
            var cantico = await _db.Canticos.FindAsync(id);
            if (cantico == null) return NotFound();

            if (file == null || file.Length == 0)
                return BadRequest("Nenhum ficheiro enviado.");

            var root = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            var uploadsDir = Path.Combine(root, "partituras");
            Directory.CreateDirectory(uploadsDir);

            var fileName = $"{cantico.Slug}-{Guid.NewGuid():N}.pdf";
            var filePath = Path.Combine(uploadsDir, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            cantico.PdfUrl = $"/partituras/{fileName}";
            await _db.SaveChangesAsync();

            return Ok(new { cantico.Id, cantico.PdfUrl });
        }

    }
}
