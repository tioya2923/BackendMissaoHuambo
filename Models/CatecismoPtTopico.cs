using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MissaoBackend.Models
{
    public class CatecismoPtTopico
    {
        [Key]
        public int Id { get; set; }
        public string Titulo { get; set; }
        public string? Slug { get; set; }

        public List<CatecismoPt> CatecismosPt { get; set; } = new();
    }
}