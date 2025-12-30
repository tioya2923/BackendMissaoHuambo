using System.ComponentModel.DataAnnotations;

namespace MissaoBackend.Models
{
    public class CatecismoPt
    {
        [Key]
        public int Id { get; set; }
        public required string Titulo { get; set; }
        public required string Texto { get; set; }
        public string? Slug { get; set; }

        public int? CatecismoPtTopicoId { get; set; }
        public CatecismoPtTopico? CatecismoPtTopico { get; set; }
    }
}