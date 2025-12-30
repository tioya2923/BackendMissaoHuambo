using System.ComponentModel.DataAnnotations;

namespace MissaoBackend.Models
{
    public class Noticia
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public string Titulo { get; set; } = string.Empty;
        [Required]
        public string Texto { get; set; } = string.Empty;
        public string? ImagemUrl { get; set; }
    }
}
