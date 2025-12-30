using System.ComponentModel.DataAnnotations;

namespace MissaoBackend.Models
{
    public class CatecismoUb
    {
        [Key]
        public int Id { get; set; }
        public required string Titulo { get; set; }
        public required string Texto { get; set; }

        public int CatecismoUbTopicoId { get; set; }
        public CatecismoUbTopico? CatecismoUbTopico { get; set; }
    }
}