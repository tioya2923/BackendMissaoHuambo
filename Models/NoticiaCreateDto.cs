using Microsoft.AspNetCore.Http;

namespace MissaoBackend.Models
{
    public class NoticiaCreateDto
    {
        public string Titulo { get; set; } = string.Empty;
        public string Texto { get; set; } = string.Empty;
        public IFormFile? Imagem { get; set; }
    }
}
