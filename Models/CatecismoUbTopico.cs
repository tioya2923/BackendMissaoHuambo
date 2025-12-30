using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MissaoBackend.Models
{
    public class CatecismoUbTopico
    {
        [Key]
        public int Id { get; set; }
        public required string Titulo { get; set; }
        public string? Slug { get; set; }

        public List<CatecismoUb> CatecismosUb { get; set; } = new();
    }
}