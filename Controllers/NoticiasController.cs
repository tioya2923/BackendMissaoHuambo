using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MissaoBackend.Data;
using MissaoBackend.Models;

namespace MissaoBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class NoticiasController : ControllerBase
    {
        private readonly AppDbContext _context;
        public NoticiasController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Noticia>>> GetAll()
        {
            return await _context.Noticias.OrderByDescending(n => n.Id).ToListAsync();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Noticia>> GetById(int id)
        {
            var noticia = await _context.Noticias.FindAsync(id);
            if (noticia == null) return NotFound();
            return noticia;
        }

        [HttpPost]
        [RequestSizeLimit(10_000_000)] // Limite de 10MB
        public async Task<ActionResult<Noticia>> Create([FromForm] NoticiaCreateDto dto)
        {
            string? imagemUrl = null;
            if (dto.Imagem != null && dto.Imagem.Length > 0)
            {
                var fileName = $"noticia_{Guid.NewGuid()}{System.IO.Path.GetExtension(dto.Imagem.FileName)}";
                var filePath = Path.Combine("wwwroot", "noticias", fileName);
                Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await dto.Imagem.CopyToAsync(stream);
                }
                imagemUrl = $"/noticias/{fileName}";
            }
            var noticia = new Noticia { Titulo = dto.Titulo, Texto = dto.Texto, ImagemUrl = imagemUrl };
            _context.Noticias.Add(noticia);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetById), new { id = noticia.Id }, noticia);
        }
        /// <summary>
        /// Remove todas as notícias e apaga as imagens associadas do disco.
        /// </summary>
        [HttpDelete("all")]
        public async Task<IActionResult> DeleteAllNoticias()
        {
            var noticias = await _context.Noticias.ToListAsync();
            foreach (var noticia in noticias)
            {
                if (!string.IsNullOrEmpty(noticia.ImagemUrl))
                {
                    var filePath = Path.Combine("wwwroot", noticia.ImagemUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
                    if (System.IO.File.Exists(filePath))
                    {
                        try { System.IO.File.Delete(filePath); } catch { /* Ignorar erro */ }
                    }
                }
            }
            _context.Noticias.RemoveRange(noticias);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
