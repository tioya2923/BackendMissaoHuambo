using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MissaoBackend.Models
{
    public class CatecismoPtTopico
    {
        [Key]
        public int Id { get; set; }
        public string Titulo { get; set; } = string.Empty;
        public string? Slug { get; set; }
        public int? ParentId { get; set; }

        public CatecismoPtTopico? Parent { get; set; }
        public List<CatecismoPtTopico> SubTopicos { get; set; } = new();
        public List<CatecismoPt> CatecismosPt { get; set; } = new();
    }
}
